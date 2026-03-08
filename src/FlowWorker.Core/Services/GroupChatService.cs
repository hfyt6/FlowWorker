using FlowWorker.Core.Configuration;
using FlowWorker.Core.DTOs;
using FlowWorker.Core.Interfaces;
using FlowWorker.Core.Repositories;
using FlowWorker.Shared.Entities;
using FlowWorker.Shared.Enums;
using Microsoft.Extensions.Logging;

namespace FlowWorker.Core.Services;

/// <summary>
/// 群聊消息路由结果
/// </summary>
public class MessageRouteResult
{
    /// <summary>
    /// 是否需要AI响应
    /// </summary>
    public bool NeedsAiResponse { get; set; }

    /// <summary>
    /// 需要响应的AI成员列表
    /// </summary>
    public List<Guid> RespondingAiMembers { get; set; } = new();

    /// <summary>
    /// 是否应该终止对话
    /// </summary>
    public bool ShouldTerminate { get; set; }

    /// <summary>
    /// 终止原因
    /// </summary>
    public string? TerminationReason { get; set; }
}

/// <summary>
/// 对话上下文
/// </summary>
public class ConversationContext
{
    /// <summary>
    /// 会话ID
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// 当前调用深度
    /// </summary>
    public int CurrentDepth { get; set; }

    /// <summary>
    /// 剩余调用令牌
    /// </summary>
    public int RemainingTokens { get; set; }

    /// <summary>
    /// 本轮已响应的AI成员
    /// </summary>
    public HashSet<Guid> RespondedMembers { get; set; } = new();

    /// <summary>
    /// 消息历史
    /// </summary>
    public List<Message> MessageHistory { get; set; } = new();

    /// <summary>
    /// 参与者列表
    /// </summary>
    public List<SessionMemberDto> Members { get; set; } = new();

    /// <summary>
    /// 会话开始时间
    /// </summary>
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 群聊服务接口
/// </summary>
public interface IGroupChatService
{
    /// <summary>
    /// 分析消息路由，决定哪些AI应该响应
    /// </summary>
    Task<MessageRouteResult> AnalyzeMessageRouteAsync(
        Guid sessionId,
        Guid? senderId,
        string messageContent,
        ConversationContext context);

    /// <summary>
    /// 创建新的对话上下文
    /// </summary>
    Task<ConversationContext> CreateContextAsync(Guid sessionId);

    /// <summary>
    /// 检查是否应该终止对话
    /// </summary>
    bool ShouldTerminateConversation(ConversationContext context);

    /// <summary>
    /// 获取AI成员的系统提示词
    /// </summary>
    Task<string> GetAiMemberSystemPromptAsync(Guid memberId, ConversationContext context);
}

/// <summary>
/// 群聊服务实现
/// </summary>
public class GroupChatService : IGroupChatService
{
    private readonly ISessionRepository _sessionRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly IMemberRepository _memberRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly GroupChatOptions _options;
    private readonly ILogger<GroupChatService> _logger;

    public GroupChatService(
        ISessionRepository sessionRepository,
        IMessageRepository messageRepository,
        IMemberRepository memberRepository,
        IRoleRepository roleRepository,
        GroupChatOptions options,
        ILogger<GroupChatService> logger)
    {
        _sessionRepository = sessionRepository;
        _messageRepository = messageRepository;
        _memberRepository = memberRepository;
        _roleRepository = roleRepository;
        _options = options;
        _logger = logger;
    }

    public async Task<MessageRouteResult> AnalyzeMessageRouteAsync(
        Guid sessionId,
        Guid? senderId,
        string messageContent,
        ConversationContext context)
    {
        var result = new MessageRouteResult();

        // 检查调用深度
        if (context.CurrentDepth >= _options.MaxCallDepth)
        {
            result.ShouldTerminate = true;
            result.TerminationReason = $"达到最大调用深度 ({_options.MaxCallDepth})";
            return result;
        }

        // 检查令牌
        if (context.RemainingTokens <= 0)
        {
            result.ShouldTerminate = true;
            result.TerminationReason = "调用令牌已用尽";
            return result;
        }

        // 检查超时
        if (DateTime.UtcNow - context.StartTime > TimeSpan.FromMinutes(_options.RoundTimeoutMinutes))
        {
            result.ShouldTerminate = true;
            result.TerminationReason = $"对话超时 ({_options.RoundTimeoutMinutes} 分钟)";
            return result;
        }

        // 获取活跃的AI成员
        var aiMembers = context.Members
            .Where(m => m.Type == MemberType.AI && m.IsActive)
            .ToList();

        if (aiMembers.Count == 0)
        {
            result.NeedsAiResponse = false;
            return result;
        }

        // 分析消息内容，决定哪些AI应该响应
        var respondingMembers = await DetermineRespondingMembersAsync(
            senderId,
            messageContent,
            aiMembers,
            context);

        // 过滤掉已经响应过的成员
        respondingMembers = respondingMembers
            .Where(m => !context.RespondedMembers.Contains(m))
            .ToList();

        if (respondingMembers.Count == 0)
        {
            result.NeedsAiResponse = false;
            return result;
        }

        result.NeedsAiResponse = true;
        result.RespondingAiMembers = respondingMembers;

        return result;
    }

    public async Task<ConversationContext> CreateContextAsync(Guid sessionId)
    {
        var session = await _sessionRepository.GetSessionWithMembersAsync(sessionId);
        if (session == null)
        {
            throw new InvalidOperationException($"会话 {sessionId} 不存在");
        }

        var messages = await _messageRepository.GetBySessionIdAsync(sessionId);
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

        return new ConversationContext
        {
            SessionId = sessionId,
            CurrentDepth = 0,
            RemainingTokens = _options.InitialCallTokens,
            MessageHistory = messages.ToList(),
            Members = members,
            StartTime = DateTime.UtcNow
        };
    }

    public bool ShouldTerminateConversation(ConversationContext context)
    {
        return context.CurrentDepth >= _options.MaxCallDepth ||
               context.RemainingTokens <= 0 ||
               DateTime.UtcNow - context.StartTime > TimeSpan.FromMinutes(_options.RoundTimeoutMinutes);
    }

    public async Task<string> GetAiMemberSystemPromptAsync(Guid memberId, ConversationContext context)
    {
        var member = await _memberRepository.GetByIdAsync(memberId);
        if (member == null)
        {
            return "你是一个AI助手。";
        }

        var role = member.Role;
        if (role == null)
        {
            return $"你是{member.Name}，一个AI助手。";
        }

        // 构建群聊上下文
        var otherMembers = context.Members
            .Where(m => m.Id != memberId)
            .Select(m => m.Name)
            .ToList();

        var otherMembersStr = otherMembers.Count > 0
            ? $"其他参与者：{string.Join("、", otherMembers)}"
            : "";

        var systemPrompt = $@"{role.SystemPrompt}

## 群聊上下文
你的名字是{member.Name}（角色：{role.DisplayName}）。
{otherMembersStr}

## 群聊规则
1. 你是在一个群聊环境中，与其他AI助手和用户一起讨论。
2. 当其他AI发言时，请认真倾听并可以补充或讨论。
3. 如果问题不是针对你的专业领域，可以简短回应或让其他更专业的AI回答。
4. 保持回复简洁，避免过长的回复。
5. 如果认为讨论已经结束，可以表示""我认为讨论已经比较充分了""。

## 当前对话历史
{FormatRecentMessages(context.MessageHistory, 5)}";

        return systemPrompt;
    }

    /// <summary>
    /// 决定哪些AI成员应该响应
    /// </summary>
    private async Task<List<Guid>> DetermineRespondingMembersAsync(
        Guid? senderId,
        string messageContent,
        List<SessionMemberDto> aiMembers,
        ConversationContext context)
    {
        var result = new List<Guid>();

        // 检查是否有@提及
        var mentionedMembers = ExtractMentionedMembers(messageContent, aiMembers);
        if (mentionedMembers.Count > 0)
        {
            result.AddRange(mentionedMembers);
            return result.Distinct().ToList();
        }

        // 获取发送者信息
        var sender = senderId.HasValue 
            ? context.Members.FirstOrDefault(m => m.Id == senderId.Value)
            : null;

        // 如果发送者为null或用户类型，让所有AI响应（群聊中用户消息应该得到所有AI的响应）
        if (sender == null || sender.Type == MemberType.User)
        {
            // 用户消息或无法确定发送者时，所有AI都可以响应
            result.AddRange(aiMembers.Select(m => m.Id));
        }
        else if (sender.Type == MemberType.AI)
        {
            // AI发送的消息，其他AI可以选择性响应
            // 检查消息是否表示结束
            if (ContainsTerminationSignal(messageContent))
            {
                // 不需要其他AI响应
                return result;
            }

            // 让其他AI有机会补充
            var otherAiMembers = aiMembers
                .Where(m => m.Id != senderId)
                .Select(m => m.Id)
                .ToList();

            // 限制响应数量，避免无限循环
            if (context.CurrentDepth < _options.MaxCallDepth - 1 && otherAiMembers.Count > 0)
            {
                result.AddRange(otherAiMembers.Take(1)); // 只让一个AI响应
            }
        }

        return result.Distinct().ToList();
    }

    /// <summary>
    /// 提取@提及的成员
    /// </summary>
    private List<Guid> ExtractMentionedMembers(string messageContent, List<SessionMemberDto> aiMembers)
    {
        var result = new List<Guid>();

        // 匹配 @成员名 的模式
        var mentionPattern = @"@(\S+)";
        var matches = System.Text.RegularExpressions.Regex.Matches(messageContent, mentionPattern);

        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            var mentionedName = match.Groups[1].Value;
            var member = aiMembers.FirstOrDefault(m =>
                m.Name.Equals(mentionedName, StringComparison.OrdinalIgnoreCase) ||
                (m.RoleDisplayName?.Equals(mentionedName, StringComparison.OrdinalIgnoreCase) ?? false));

            if (member != null)
            {
                result.Add(member.Id);
            }
        }

        return result;
    }

    /// <summary>
    /// 检查消息是否包含终止信号
    /// </summary>
    private bool ContainsTerminationSignal(string messageContent)
    {
        var terminationSignals = new[]
        {
            "讨论已经结束",
            "我认为讨论已经比较充分了",
            "没有其他要补充的了",
            "以上就是我的回答"
        };

        return terminationSignals.Any(signal =>
            messageContent.Contains(signal, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 格式化最近的消息
    /// </summary>
    private string FormatRecentMessages(List<Message> messages, int count)
    {
        var recentMessages = messages.TakeLast(count).ToList();
        if (recentMessages.Count == 0)
        {
            return "（暂无历史消息）";
        }

        var sb = new System.Text.StringBuilder();
        foreach (var msg in recentMessages)
        {
            var senderName = msg.Member?.Name ?? "用户";
            sb.AppendLine($"[{senderName}]: {msg.Content}");
        }

        return sb.ToString();
    }
}