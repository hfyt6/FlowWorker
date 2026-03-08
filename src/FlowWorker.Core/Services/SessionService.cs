using FlowWorker.Core.DTOs;
using FlowWorker.Core.Interfaces;
using FlowWorker.Core.Repositories;
using FlowWorker.Shared.Entities;
using FlowWorker.Shared.Enums;
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
    private readonly IMemberRepository _memberRepository;

    public SessionService(
        ISessionRepository sessionRepository,
        IMessageRepository messageRepository,
        IApiConfigRepository apiConfigRepository,
        IMemberRepository memberRepository)
    {
        _sessionRepository = sessionRepository;
        _messageRepository = messageRepository;
        _apiConfigRepository = apiConfigRepository;
        _memberRepository = memberRepository;
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

        // 获取每个会话的消息数量和成员数量
        var result = new List<SessionListItemDto>();
        foreach (var session in sessions)
        {
            var messageCount = await _messageRepository.CountAsync(m => m.SessionId == session.Id);
            var sessionWithMembers = await _sessionRepository.GetSessionWithMembersAsync(session.Id);
            var memberCount = sessionWithMembers?.SessionMembers?.Count ?? 0;
            
            result.Add(new SessionListItemDto
            {
                Id = session.Id,
                Title = session.Title,
                Type = session.Type,
                Model = session.Model,
                CreatedAt = session.CreatedAt,
                UpdatedAt = session.UpdatedAt,
                MessageCount = messageCount,
                MemberCount = memberCount
            });
        }

        return result;
    }

    public async Task<SessionDetailDto?> GetSessionDetailAsync(Guid id)
    {
        var session = await _sessionRepository.GetSessionWithMembersAsync(id);
        if (session == null)
        {
            return null;
        }

        var messages = await _messageRepository.GetBySessionIdAsync(id);
        var apiConfig = await _apiConfigRepository.GetByIdAsync(session.ApiConfigId);

        // 构建参与者列表
        var members = new List<SessionMemberDto>();
        if (session.SessionMembers != null)
        {
            foreach (var sm in session.SessionMembers.Where(m => m.IsActive))
            {
                var member = sm.Member;
                if (member != null)
                {
                    members.Add(new SessionMemberDto
                    {
                        Id = member.Id,
                        Name = member.Name,
                        Type = member.Type,
                        Avatar = member.Avatar,
                        Status = member.Status,
                        RoleName = member.Role?.Name,
                        RoleDisplayName = member.Role?.DisplayName,
                        JoinedAt = sm.JoinedAt,
                        IsActive = sm.IsActive
                    });
                }
            }
        }

        return new SessionDetailDto
        {
            Id = session.Id,
            Title = session.Title,
            Type = session.Type,
            ApiConfigId = session.ApiConfigId,
            ApiConfigName = apiConfig?.Name ?? string.Empty,
            Model = session.Model,
            SystemPrompt = session.SystemPrompt ?? string.Empty,
            Temperature = session.Temperature,
            MaxTokens = session.MaxTokens,
            CreatedAt = session.CreatedAt,
            UpdatedAt = session.UpdatedAt,
            Messages = messages.ToList(),
            Members = members
        };
    }

    public async Task<Guid> CreateSessionAsync(CreateSessionRequest request)
    {
        // 获取成员信息
        var member = await _memberRepository.GetByIdAsync(request.MemberId);
        if (member == null)
        {
            throw new InvalidOperationException($"成员 {request.MemberId} 不存在");
        }

        if (member.Type != MemberType.AI)
        {
            throw new InvalidOperationException("单聊只能选择AI成员");
        }

        if (!member.ApiConfigId.HasValue)
        {
            throw new InvalidOperationException($"成员 {member.Name} 没有关联的API配置");
        }

        var session = new Session
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Type = SessionType.Single,
            ApiConfigId = member.ApiConfigId.Value,
            Model = member.Model ?? string.Empty,
            SystemPrompt = member.Role?.SystemPrompt ?? "You are a helpful assistant.",
            Temperature = member.Temperature,
            MaxTokens = member.MaxTokens,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _sessionRepository.AddAsync(session);

        // 添加AI成员到会话
        await _sessionRepository.AddMemberAsync(session.Id, request.MemberId);

        return session.Id;
    }

    public async Task<Guid> CreateGroupSessionAsync(CreateGroupSessionRequest request)
    {
        // 验证创建者存在
        var creator = await _memberRepository.GetByIdAsync(request.CreatedBy);
        if (creator == null)
        {
            throw new InvalidOperationException($"创建者 {request.CreatedBy} 不存在");
        }

        // 获取默认 API 配置
        var defaultConfig = await _apiConfigRepository.GetDefaultConfigAsync();
        if (defaultConfig == null)
        {
            throw new InvalidOperationException("未找到默认 API 配置，请先创建 API 配置");
        }

        var session = new Session
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Type = SessionType.Group,
            CreatedBy = request.CreatedBy,
            ApiConfigId = defaultConfig.Id,
            Model = defaultConfig.Model,
            SystemPrompt = request.SystemPrompt ?? "你是一个群聊助手，与其他AI助手协作帮助用户解决问题。",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _sessionRepository.AddAsync(session);

        // 添加创建者到会话
        await _sessionRepository.AddMemberAsync(session.Id, request.CreatedBy);

        // 添加AI参与者
        foreach (var aiMemberId in request.AiMemberIds)
        {
            var aiMember = await _memberRepository.GetByIdAsync(aiMemberId);
            if (aiMember != null && aiMember.Type == MemberType.AI)
            {
                await _sessionRepository.AddMemberAsync(session.Id, aiMemberId);
            }
        }

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
            var sessionWithMembers = await _sessionRepository.GetSessionWithMembersAsync(session.Id);
            var memberCount = sessionWithMembers?.SessionMembers?.Count ?? 0;
            
            result.Add(new SessionListItemDto
            {
                Id = session.Id,
                Title = session.Title,
                Type = session.Type,
                Model = session.Model,
                CreatedAt = session.CreatedAt,
                UpdatedAt = session.UpdatedAt,
                MessageCount = messageCount,
                MemberCount = memberCount
            });
        }

        return result;
    }

    public async Task<IReadOnlyList<SessionMemberDto>> GetSessionMembersAsync(Guid sessionId)
    {
        var session = await _sessionRepository.GetSessionWithMembersAsync(sessionId);
        if (session == null)
        {
            throw new InvalidOperationException($"会话 {sessionId} 不存在");
        }

        var members = new List<SessionMemberDto>();
        if (session.SessionMembers != null)
        {
            foreach (var sm in session.SessionMembers.Where(m => m.IsActive))
            {
                var member = sm.Member;
                if (member != null)
                {
                    members.Add(new SessionMemberDto
                    {
                        Id = member.Id,
                        Name = member.Name,
                        Type = member.Type,
                        Avatar = member.Avatar,
                        Status = member.Status,
                        RoleName = member.Role?.Name,
                        RoleDisplayName = member.Role?.DisplayName,
                        JoinedAt = sm.JoinedAt,
                        IsActive = sm.IsActive
                    });
                }
            }
        }

        return members;
    }

    public async Task AddMemberToSessionAsync(Guid sessionId, Guid memberId)
    {
        var member = await _memberRepository.GetByIdAsync(memberId);
        if (member == null)
        {
            throw new InvalidOperationException($"成员 {memberId} 不存在");
        }

        var session = await _sessionRepository.GetByIdAsync(sessionId);
        if (session == null)
        {
            throw new InvalidOperationException($"会话 {sessionId} 不存在");
        }

        if (session.Type != SessionType.Group)
        {
            throw new InvalidOperationException("只有群聊会话可以添加参与者");
        }

        await _sessionRepository.AddMemberAsync(sessionId, memberId);
    }

    public async Task RemoveMemberFromSessionAsync(Guid sessionId, Guid memberId)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId);
        if (session == null)
        {
            throw new InvalidOperationException($"会话 {sessionId} 不存在");
        }

        if (session.Type != SessionType.Group)
        {
            throw new InvalidOperationException("只有群聊会话可以移除参与者");
        }

        await _sessionRepository.RemoveMemberAsync(sessionId, memberId);
    }
}
