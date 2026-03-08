using System.Text.Json.Serialization;

namespace FlowWorker.Shared.Entities;

/// <summary>
/// 会话成员关联实体
/// </summary>
public class SessionMember
{
    /// <summary>
    /// 主键
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 会话ID
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// 成员ID
    /// </summary>
    public Guid MemberId { get; set; }

    /// <summary>
    /// 加入时间
    /// </summary>
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 是否活跃
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 关联的会话
    /// </summary>
    [JsonIgnore]
    public virtual Session? Session { get; set; }

    /// <summary>
    /// 关联的成员
    /// </summary>
    [JsonIgnore]
    public virtual Member? Member { get; set; }
}
