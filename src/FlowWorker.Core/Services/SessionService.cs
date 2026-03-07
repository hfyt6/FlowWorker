using FlowWorker.Core.DTOs;
using FlowWorker.Core.Interfaces;
using FlowWorker.Core.Repositories;
using FlowWorker.Shared.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace FlowWorker.Core.Services;

/// <summary>
/// 会话服务实现
/// </summary>
public class SessionService : ISessionService
{
    private readonly ISessionRepository _sessionRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly IApiConfigRepository _apiConfigRepository;

    public SessionService(
        ISessionRepository sessionRepository,
        IMessageRepository messageRepository,
        IApiConfigRepository apiConfigRepository)
    {
        _sessionRepository = sessionRepository;
        _messageRepository = messageRepository;
        _apiConfigRepository = apiConfigRepository;
    }

    public async Task<IReadOnlyList<SessionListItemDto>> GetSessionsAsync(Guid? apiConfigId = null)
    {
        IEnumerable<Session> sessions;
        
        if (apiConfigId.HasValue)
        {
            sessions = await _sessionRepository.GetByApiConfigIdAsync(apiConfigId.Value);
        }
        else
        {
            sessions = await _sessionRepository.GetAllAsync();
        }

        // 获取每个会话的消息数量
        var result = new List<SessionListItemDto>();
        foreach (var session in sessions)
        {
            var messageCount = await _messageRepository.CountAsync(m => m.SessionId == session.Id);
            result.Add(new SessionListItemDto
            {
                Id = session.Id,
                Title = session.Title,
                Model = session.Model,
                CreatedAt = session.CreatedAt,
                UpdatedAt = session.UpdatedAt,
                MessageCount = messageCount
            });
        }

        return result;
    }

    public async Task<SessionDetailDto?> GetSessionDetailAsync(Guid id)
    {
        var session = await _sessionRepository.GetByIdAsync(id);
        if (session == null)
        {
            return null;
        }

        var messages = await _messageRepository.GetBySessionIdAsync(id);
        var apiConfig = await _apiConfigRepository.GetByIdAsync(session.ApiConfigId);

        return new SessionDetailDto
        {
            Id = session.Id,
            Title = session.Title,
            ApiConfigId = session.ApiConfigId,
            ApiConfigName = apiConfig?.Name ?? string.Empty,
            Model = session.Model,
            SystemPrompt = session.SystemPrompt ?? string.Empty,
            Temperature = session.Temperature,
            MaxTokens = session.MaxTokens,
            CreatedAt = session.CreatedAt,
            UpdatedAt = session.UpdatedAt,
            Messages = messages.ToList()
        };
    }

    public async Task<Guid> CreateSessionAsync(CreateSessionRequest request)
    {
        var session = new Session
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            ApiConfigId = request.ApiConfigId,
            Model = request.Model,
            SystemPrompt = request.SystemPrompt,
            Temperature = request.Temperature,
            MaxTokens = request.MaxTokens,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _sessionRepository.AddAsync(session);
        return session.Id;
    }

    public async Task UpdateSessionAsync(Guid id, UpdateSessionRequest request)
    {
        var session = await _sessionRepository.GetByIdAsync(id);
        if (session == null)
        {
            throw new InvalidOperationException($"会话 {id} 不存在");
        }

        session.Title = request.Title;
        session.ApiConfigId = request.ApiConfigId;
        session.Model = request.Model;
        session.SystemPrompt = request.SystemPrompt;
        session.Temperature = request.Temperature;
        session.MaxTokens = request.MaxTokens;
        session.UpdatedAt = DateTime.UtcNow;

        await _sessionRepository.UpdateAsync(session);
    }

    public async Task DeleteSessionAsync(Guid id)
    {
        var session = await _sessionRepository.GetByIdAsync(id);
        if (session == null)
        {
            throw new InvalidOperationException($"会话 {id} 不存在");
        }

        // 删除关联的消息
        await _messageRepository.DeleteBySessionIdAsync(id);
        
        await _sessionRepository.DeleteAsync(session);
    }

    public async Task<IReadOnlyList<SessionListItemDto>> SearchSessionsAsync(string title)
    {
        var sessions = await _sessionRepository.SearchAsync(title);
        
        var result = new List<SessionListItemDto>();
        foreach (var session in sessions)
        {
            var messageCount = await _messageRepository.CountAsync(m => m.SessionId == session.Id);
            result.Add(new SessionListItemDto
            {
                Id = session.Id,
                Title = session.Title,
                Model = session.Model,
                CreatedAt = session.CreatedAt,
                UpdatedAt = session.UpdatedAt,
                MessageCount = messageCount
            });
        }

        return result;
    }
}