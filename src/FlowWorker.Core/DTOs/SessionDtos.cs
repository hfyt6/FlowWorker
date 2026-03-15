using FlowWorker.Shared.Entities;
using FlowWorker.Shared.Enums;

namespace FlowWorker.Core.DTOs;

/// <summary>
/// 创建会话请求（单聊）
/// </summary>
public class CreateSessionRequest
{
    /// <summary>
    /// 会话标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 成员ID（AI成员）
    /// </summary>
    public Guid MemberId { get; set; }

    /// <summary>
    /// 会话工作目录路径（可选，默认为系统临时目录）
    /// </summary>
    public string? WorkingDirectory { get; set; }
}

/// <summary>
/// 创建群聊会话请求
/// </summary>
public class CreateGroupSessionRequest
{
    /// <summary>
    /// 会话标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 创建者ID（用户成员ID）
    /// </summary>
    public Guid CreatedBy { get; set; }

    /// <summary>
    /// AI参与者ID列表
    /// </summary>
    public List<Guid> AiMemberIds { get; set; } = new();

    /// <summary>
    /// 系统提示词（可选）
    /// </summary>
    public string? SystemPrompt { get; set; }

    /// <summary>
    /// 会话工作目录路径（可选，默认为系统临时目录）
    /// </summary>
    public string? WorkingDirectory { get; set; }
}

/// <summary>
/// 添加会话参与者请求
/// </summary>
public class AddSessionMemberRequest
{
    /// <summary>
    /// 成员ID
    /// </summary>
    public Guid MemberId { get; set; }
}

/// <summary>
/// 会话参与者信息
/// </summary>
public class SessionMemberDto
{
    /// <summary>
    /// 成员ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 成员名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 成员类型
    /// </summary>
    public MemberType Type { get; set; }

    /// <summary>
    /// 头像
    /// </summary>
    public string? Avatar { get; set; }

    /// <summary>
    /// 成员状态
    /// </summary>
    public MemberStatus Status { get; set; }

    /// <summary>
    /// 角色名称（仅AI成员）
    /// </summary>
    public string? RoleName { get; set; }

    /// <summary>
    /// 角色显示名称（仅AI成员）
    /// </summary>
    public string? RoleDisplayName { get; set; }

    /// <summary>
    /// 加入时间
    /// </summary>
    public DateTime JoinedAt { get; set; }

    /// <summary>
    /// 是否活跃
    /// </summary>
    public bool IsActive { get; set; }
}

/// <summary>
/// 更新会话请求
/// </summary>
public class UpdateSessionRequest
{
    /// <summary>
    /// 会话标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// API 配置 ID（单聊时必填，群聊时为 null）
    /// </summary>
    public Guid? ApiConfigId { get; set; }

    /// <summary>
    /// 使用的模型
    /// </summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// 系统提示词
    /// </summary>
    public string SystemPrompt { get; set; } = string.Empty;

    /// <summary>
    /// 生成温度
    /// </summary>
    public decimal Temperature { get; set; } = 0.7m;

    /// <summary>
    /// 最大 Token 数
    /// </summary>
    public int? MaxTokens { get; set; }
}

/// <summary>
/// 会话列表项
/// </summary>
public class SessionListItemDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public SessionType Type { get; set; }
    public string Model { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int MessageCount { get; set; }
    
    /// <summary>
    /// 参与者数量（群聊）
    /// </summary>
    public int MemberCount { get; set; }
}

/// <summary>
/// 会话详情
/// </summary>
public class SessionDetailDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public SessionType Type { get; set; }
    public Guid? ApiConfigId { get; set; }
    public string ApiConfigName { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string SystemPrompt { get; set; } = string.Empty;
    public decimal Temperature { get; set; }
    public int? MaxTokens { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// 会话工作目录路径
    /// </summary>
    public string WorkingDirectory { get; set; } = string.Empty;

    public ICollection<Message> Messages { get; set; } = new List<Message>();

    /// <summary>
    /// 参与者列表
    /// </summary>
    public List<SessionMemberDto> Members { get; set; } = new();
}
