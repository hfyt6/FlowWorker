using FlowWorker.Core.DTOs;
using FlowWorker.Core.Interfaces;
using FlowWorker.Core.Repositories;
using FlowWorker.Shared.DTOs;
using FlowWorker.Shared.Entities;
using FlowWorker.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Text;

namespace FlowWorker.Core.Services;

/// <summary>
/// 消息服务实现
/// </summary>
public class MessageService : IMessageService
{
    private readonly IMessageRepository _messageRepository;
    private readonly ISessionRepository _sessionRepository;
    private readonly IOpenAIService _openAIService;
    private readonly IMemberRepository _memberRepository;
    private readonly IGroupChatService _groupChatService;
    private readonly ILogger<MessageService> _logger;

    public MessageService(
        IMessageRepository messageRepository,
        ISessionRepository sessionRepository,
        IOpenAIService openAIService,
        IMemberRepository memberRepository,
        IGroupChatService groupChatService,
        ILogger<MessageService> logger)
    {
        _messageRepository = messageRepository;
        _sessionRepository = sessionRepository;
        _openAIService = openAIService;
        _memberRepository = memberRepository;
        _groupChatService = groupChatService;
        _logger = logger;
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
            session.MaxTokens,
            null,
            session);

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
            session.MaxTokens,
            null,
            session);

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

    public async Task<SendMessageResponse> SendMessageStreamAsync(Guid sessionId, string content, Func<StreamContentChunk, Task> onChunk)
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
        
        // 调用 OpenAI API 流式接口
        var fullContent = new StringBuilder();
        
        await _openAIService.SendMessageStreamAsync(
            session.ApiConfig.ApiKey,
            session.ApiConfig.BaseUrl,
            session.Model,
            allMessages,
            async chunk =>
            {
                if (chunk.Type == "content" && !string.IsNullOrEmpty(chunk.Content))
                {
                    fullContent.Append(chunk.Content);
                }
                await onChunk(chunk);
            },
            session.SystemPrompt,
            session.Temperature,
            session.MaxTokens,
            null,
            session);

        var responseContent = fullContent.ToString();

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

        // 发送完成标记
        await onChunk(new StreamContentChunk
        {
            Type = "complete",
            Content = responseContent
        });
        
        return new SendMessageResponse
        {
            Content = responseContent,
            Model = session.Model,
            IsComplete = true
        };
    }

    public async Task<SendMessageResponse> RegenerateResponseStreamAsync(Guid sessionId, Func<StreamContentChunk, Task> onChunk)
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

        // 调用 OpenAI API 流式接口重新生成
        var fullContent = new StringBuilder();
        
        await _openAIService.SendMessageStreamAsync(
            session.ApiConfig.ApiKey,
            session.ApiConfig.BaseUrl,
            session.Model,
            remainingMessages,
            async chunk =>
            {
                if (chunk.Type == "content" && !string.IsNullOrEmpty(chunk.Content))
                {
                    fullContent.Append(chunk.Content);
                }
                await onChunk(chunk);
            },
            session.SystemPrompt,
            session.Temperature,
            session.MaxTokens,
            null,
            session);

        var responseContent = fullContent.ToString();

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

        // 发送完成标记
        await onChunk(new StreamContentChunk
        {
            Type = "complete",
            Content = responseContent
        });

        return new SendMessageResponse
        {
            Content = responseContent,
            Model = session.Model,
            IsComplete = true
        };
    }

    /// <summary>
    /// 发送群聊消息（支持多AI响应）
    /// </summary>
    public async Task SendMessageGroupStreamAsync(
        Guid sessionId,
        string content,
        Guid? senderMemberId,
        Func<StreamContentChunk, Guid, Task> onChunk)
    {
        var session = await _sessionRepository.GetSessionWithMembersAsync(sessionId);
        if (session == null)
        {
            throw new InvalidOperationException($"会话 {sessionId} 不存在");
        }

        if (session.Type != SessionType.Group)
        {
            throw new InvalidOperationException("此方法仅支持群聊会话");
        }

        // 获取第一个AI成员的API配置（群聊使用第一个AI成员的配置）
        var firstAiMember = session.SessionMembers?
            .Where(sm => sm.Member?.Type == MemberType.AI && sm.IsActive)
            .Select(sm => sm.Member)
            .FirstOrDefault();

        if (firstAiMember?.ApiConfig == null)
        {
            throw new InvalidOperationException($"群聊会话 {sessionId} 中没有配置API信息的AI成员");
        }

        var apiConfig = firstAiMember.ApiConfig;

        // 创建对话上下文
        var context = await _groupChatService.CreateContextAsync(sessionId);

        // 保存用户消息
        var userMessage = new Message
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            MemberId = senderMemberId,
            Role = MessageRole.User,
            Content = content,
            CreatedAt = DateTime.UtcNow
        };
        await _messageRepository.AddAsync(userMessage);

        // 通知用户消息已保存
        await onChunk(new StreamContentChunk
        {
            Type = "user_message",
            Content = content,
            MessageId = userMessage.Id.ToString()
        }, senderMemberId ?? Guid.Empty);

        // 分析消息路由
        var routeResult = await _groupChatService.AnalyzeMessageRouteAsync(
            sessionId, senderMemberId, content, context);

        if (!routeResult.NeedsAiResponse)
        {
            _logger.LogInformation("群聊消息不需要AI响应");
            return;
        }

        // 依次让每个AI成员响应
        foreach (var aiMemberId in routeResult.RespondingAiMembers)
        {
            if (_groupChatService.ShouldTerminateConversation(context))
            {
                _logger.LogInformation("群聊对话已终止: 深度 {Depth}", context.CurrentDepth);
                break;
            }

            context.CurrentDepth++;
            context.RemainingTokens--;

            // 获取AI成员信息
            var aiMember = await _memberRepository.GetByIdAsync(aiMemberId);
            if (aiMember == null || aiMember.Status != MemberStatus.Online)
            {
                continue;
            }

            // 获取AI成员的系统提示词
            var systemPrompt = await _groupChatService.GetAiMemberSystemPromptAsync(aiMemberId, context);

            // 获取消息历史
            var messages = await _messageRepository.GetBySessionIdAsync(sessionId);
            if(messages.Count > 0 && messages.Last().Role == MessageRole.Assistant)
            {
                var tempMessages = messages.ToList();
                tempMessages.RemoveAt(tempMessages.Count-1);
                messages = tempMessages;
            }
            
            // 调用 OpenAI API 流式接口
            var fullContent = new StringBuilder();
            var assistantMessageId = Guid.NewGuid();

            // 通知开始响应
            await onChunk(new StreamContentChunk
            {
                Type = "member_start",
                MemberId = aiMemberId.ToString(),
                MemberName = aiMember.Name
            }, aiMemberId);

            try
            {
                // 使用AI成员自己的API配置，如果没有则使用第一个AI成员的API配置
                var memberApiConfig = aiMember.ApiConfig ?? apiConfig;
                var model = !string.IsNullOrWhiteSpace(aiMember.Model) ? aiMember.Model : memberApiConfig.Model;
                
                _logger.LogInformation("AI成员 {MemberId} 使用模型: {Model}, API BaseUrl: {BaseUrl}", 
                    aiMemberId, model, memberApiConfig.BaseUrl);

                await _openAIService.SendMessageStreamAsync(
                    memberApiConfig.ApiKey,
                    memberApiConfig.BaseUrl,
                    model,
                    messages,
                    async chunk =>
                    {
                        if (chunk.Type == "content" && !string.IsNullOrEmpty(chunk.Content))
                        {
                            fullContent.Append(chunk.Content);
                        }
                        await onChunk(chunk, aiMemberId);
                    },
                    systemPrompt,
                    aiMember.Temperature,
                    (int?)aiMember.MaxToken ?? session.MaxTokens,
                    null,
                    session);

                var responseContent = fullContent.ToString();

                // 保存AI消息
                var assistantMessage = new Message
                {
                    Id = assistantMessageId,
                    SessionId = sessionId,
                    MemberId = aiMemberId,
                    Role = MessageRole.Assistant,
                    Content = responseContent,
                    Model = model,
                    CreatedAt = DateTime.UtcNow
                };
                await _messageRepository.AddAsync(assistantMessage);

                // 标记该成员已响应
                context.RespondedMembers.Add(aiMemberId);

                // 更新消息历史
                context.MessageHistory.Add(assistantMessage);

                // 通知响应完成
                await onChunk(new StreamContentChunk
                {
                    Type = "member_complete",
                    Content = responseContent,
                    MessageId = assistantMessageId.ToString(),
                    MemberId = aiMemberId.ToString(),
                    MemberName = aiMember.Name
                }, aiMemberId);

                // 检查是否需要继续让其他AI响应
                var nextRoute = await _groupChatService.AnalyzeMessageRouteAsync(
                    sessionId, aiMemberId, responseContent, context);

                if (nextRoute.ShouldTerminate)
                {
                    _logger.LogInformation("群聊对话终止: {Reason}", nextRoute.TerminationReason);
                    break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI成员 {MemberId} 响应失败", aiMemberId);
                await onChunk(new StreamContentChunk
                {
                    Type = "error",
                    Content = $"AI成员 {aiMember.Name} 响应失败: {ex.Message}",
                    MemberId = aiMemberId.ToString()
                }, aiMemberId);
            }
        }

        // 通知群聊结束
        await onChunk(new StreamContentChunk
        {
            Type = "group_complete"
        }, Guid.Empty);
    }

    /// <summary>
    /// 发送群聊消息（非流式，支持多AI响应）- 内部调用流式方法
    /// </summary>
    public async Task<List<SendMessageResponse>> SendMessageGroupAsync(
        Guid sessionId,
        string content,
        Guid? senderMemberId)
    {
        var responses = new List<SendMessageResponse>();
        var responseContents = new Dictionary<Guid, StringBuilder>();
        var responseModels = new Dictionary<Guid, string>();

        await SendMessageGroupStreamAsync(
            sessionId,
            content,
            senderMemberId,
            async (chunk, memberId) =>
            {
                if (chunk.Type == "content" && !string.IsNullOrEmpty(chunk.Content))
                {
                    if (!responseContents.ContainsKey(memberId))
                    {
                        responseContents[memberId] = new StringBuilder();
                    }
                    responseContents[memberId].Append(chunk.Content);
                }
                else if (chunk.Type == "member_start")
                {
                    // 记录成员开始响应
                    if (!string.IsNullOrEmpty(chunk.Model))
                    {
                        responseModels[memberId] = chunk.Model;
                    }
                }
                else if (chunk.Type == "member_complete")
                {
                    // 成员响应完成，构建响应对象
                    var responseContent = responseContents.ContainsKey(memberId) 
                        ? responseContents[memberId].ToString() 
                        : chunk.Content ?? string.Empty;
                    
                    responses.Add(new SendMessageResponse
                    {
                        Content = responseContent,
                        Model = responseModels.ContainsKey(memberId) ? responseModels[memberId] : chunk.Model ?? string.Empty,
                        IsComplete = true
                    });
                }
                else if (chunk.Type == "error" && memberId != Guid.Empty)
                {
                    // 处理错误响应
                    responses.Add(new SendMessageResponse
                    {
                        Content = chunk.Content ?? "未知错误",
                        Model = responseModels.ContainsKey(memberId) ? responseModels[memberId] : string.Empty,
                        IsComplete = false
                    });
                }

                await Task.CompletedTask;
            });

        return responses;
    }
}
