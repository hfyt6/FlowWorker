using FlowWorker.Core.DTOs;
using FlowWorker.Core.Interfaces;
using FlowWorker.Core.Repositories;
using FlowWorker.Shared.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace FlowWorker.Core.Services;

/// <summary>
/// 消息服务实现
/// </summary>
public class MessageService : IMessageService
{
    private readonly IMessageRepository _messageRepository;
    private readonly ISessionRepository _sessionRepository;
    private readonly IOpenAIService _openAIService;

    public MessageService(
        IMessageRepository messageRepository,
        ISessionRepository sessionRepository,
        IOpenAIService openAIService)
    {
        _messageRepository = messageRepository;
        _sessionRepository = sessionRepository;
        _openAIService = openAIService;
    }

    public async Task<IReadOnlyList<MessageListItemDto>> GetMessagesAsync(Guid sessionId)
    {
        var messages = await _messageRepository.GetBySessionIdAsync(sessionId);
        
        return messages.Select(m => new MessageListItemDto
        {
            Id = m.Id,
            Role = m.Role,
            Content = m.Content,
            Tokens = m.Tokens,
            Model = m.Model,
            CreatedAt = m.CreatedAt
        }).ToList();
    }

    public async Task<IReadOnlyList<MessageListItemDto>> GetLastMessagesAsync(Guid sessionId, int count)
    {
        var messages = await _messageRepository.GetLastMessagesAsync(sessionId, count);
        
        return messages.Select(m => new MessageListItemDto
        {
            Id = m.Id,
            Role = m.Role,
            Content = m.Content,
            Tokens = m.Tokens,
            Model = m.Model,
            CreatedAt = m.CreatedAt
        }).ToList();
    }

    public async Task<Guid> CreateMessageAsync(Guid sessionId, CreateMessageRequest request)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId);
        if (session == null)
        {
            throw new InvalidOperationException($"会话 {sessionId} 不存在");
        }

        var message = new Message
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            Role = request.Role,
            Content = request.Content,
            Tokens = request.Tokens,
            Model = request.Model,
            CreatedAt = DateTime.UtcNow
        };

        await _messageRepository.AddAsync(message);
        return message.Id;
    }

    public async Task DeleteMessageAsync(Guid id)
    {
        var message = await _messageRepository.GetByIdAsync(id);
        if (message == null)
        {
            throw new InvalidOperationException($"消息 {id} 不存在");
        }

        await _messageRepository.DeleteAsync(message);
    }

    public async Task<SendMessageResponse> SendMessageAsync(Guid sessionId, string content)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId);
        if (session == null)
        {
            throw new InvalidOperationException($"会话 {sessionId} 不存在");
        }

        // 验证 API 配置
        if (session.ApiConfig == null)
        {
            throw new InvalidOperationException($"会话 {sessionId} 未配置 API 信息，请先在设置页面配置 API 信息");
        }

        if (string.IsNullOrWhiteSpace(session.ApiConfig.BaseUrl))
        {
            throw new InvalidOperationException($"会话 {sessionId} 的 API 基础 URL 为空，请先在设置页面配置 API 信息");
        }

        if (string.IsNullOrWhiteSpace(session.ApiConfig.ApiKey))
        {
            throw new InvalidOperationException($"会话 {sessionId} 的 API 密钥为空，请先在设置页面配置 API 信息");
        }

        // 获取会话的历史消息
        var messages = await _messageRepository.GetBySessionIdAsync(sessionId);
        
        // 添加用户消息
        var userMessage = new Message
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            Role = MessageRole.User,
            Content = content,
            CreatedAt = DateTime.UtcNow
        };
        
        // 保存用户消息到数据库
        await _messageRepository.AddAsync(userMessage);
        
        var allMessages = messages.Concat(new[] { userMessage });
        
        // 调用 OpenAI API
        var responseContent = await _openAIService.SendMessageAsync(
            session.ApiConfig.ApiKey,
            session.ApiConfig.BaseUrl,
            session.Model,
            allMessages,
            session.SystemPrompt,
            session.Temperature,
            session.MaxTokens);

        // 保存助手消息
        var assistantMessage = new Message
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            Role = MessageRole.Assistant,
            Content = responseContent,
            Model = session.Model,
            CreatedAt = DateTime.UtcNow
        };
        
        await _messageRepository.AddAsync(assistantMessage);

        return new SendMessageResponse
        {
            Content = responseContent,
            Model = session.Model,
            IsComplete = true
        };
    }

    public async Task<SendMessageResponse> RegenerateResponseAsync(Guid sessionId)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId);
        if (session == null)
        {
            throw new InvalidOperationException($"会话 {sessionId} 不存在");
        }

        // 验证 API 配置
        if (session.ApiConfig == null)
        {
            throw new InvalidOperationException($"会话 {sessionId} 未配置 API 信息，请先在设置页面配置 API 信息");
        }

        if (string.IsNullOrWhiteSpace(session.ApiConfig.BaseUrl))
        {
            throw new InvalidOperationException($"会话 {sessionId} 的 API 基础 URL 为空，请先在设置页面配置 API 信息");
        }

        if (string.IsNullOrWhiteSpace(session.ApiConfig.ApiKey))
        {
            throw new InvalidOperationException($"会话 {sessionId} 的 API 密钥为空，请先在设置页面配置 API 信息");
        }

        // 获取会话的最后两条消息（最后一条是用户消息，倒数第二条是助手消息）
        var messages = await _messageRepository.GetLastMessagesAsync(sessionId, 2);
        
        if (messages.Count < 2)
        {
            throw new InvalidOperationException("没有足够的消息进行重新生成");
        }

        // 移除最后两条消息（用户消息和之前的助手消息）
        await _messageRepository.DeleteRangeAsync(messages);

        // 重新获取消息历史
        var remainingMessages = await _messageRepository.GetBySessionIdAsync(sessionId);
        
        // 获取最后一条用户消息
        var lastUserMessage = remainingMessages.LastOrDefault(m => m.Role == MessageRole.User);
        if (lastUserMessage == null)
        {
            throw new InvalidOperationException("没有找到用户消息");
        }

        // 调用 OpenAI API 重新生成
        var responseContent = await _openAIService.SendMessageAsync(
            session.ApiConfig.ApiKey,
            session.ApiConfig.BaseUrl,
            session.Model,
            remainingMessages,
            session.SystemPrompt,
            session.Temperature,
            session.MaxTokens);

        // 保存新的助手消息
        var assistantMessage = new Message
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            Role = MessageRole.Assistant,
            Content = responseContent,
            Model = session.Model,
            CreatedAt = DateTime.UtcNow
        };
        
        await _messageRepository.AddAsync(assistantMessage);

        return new SendMessageResponse
        {
            Content = responseContent,
            Model = session.Model,
            IsComplete = true
        };
    }
}