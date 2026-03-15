using System.Text.Json.Serialization;
using FlowWorker.Shared.Enums;

namespace FlowWorker.Shared.Entities;

/// <summary>
/// 成员实体（用户和AI的统一抽象）
/// </summary>
public class Member
{
    /// <summary>
    /// 主键
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 显示名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 类型（User/AI）
    /// </summary>
    public MemberType Type { get; set; }

    /// <summary>
    /// 关联的角色ID（仅AI类型）
    /// </summary>
    public Guid? RoleId { get; set; }

    /// <summary>
    /// 头像URL
    /// </summary>
    public string? Avatar { get; set; }

    /// <summary>
    /// 状态（Offline/Online/Busy）
    /// </summary>
    public MemberStatus Status { get; set; } = MemberStatus.Online;

    /// <summary>
    /// 关联的API配置ID（仅AI类型）
    /// </summary>
    public Guid? ApiConfigId { get; set; }

    /// <summary>
    /// 使用的模型（仅AI类型）
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// 生成温度（仅AI类型）
    /// </summary>
    public decimal Temperature { get; set; } = 0.7m;

    /// <summary>
    /// 最大上下文长度（仅AI类型），默认128k
    /// </summary>
    public long MaxToken { get; set; } = 131072; // 128k = 128 * 1024

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 关联的角色
    /// </summary>
    [JsonIgnore]
    public virtual Role? Role { get; set; }

    /// <summary>
    /// 关联的API配置
    /// </summary>
    [JsonIgnore]
    public virtual ApiConfig? ApiConfig { get; set; }

    /// <summary>
    /// 关联的会话成员记录
    /// </summary>
    [JsonIgnore]
    public virtual ICollection<SessionMember> SessionMembers { get; set; } = new List<SessionMember>();

    /// <summary>
    /// 发送的消息列表
    /// </summary>
    [JsonIgnore]
    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
}
