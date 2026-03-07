using System.Text.Json.Serialization;

namespace FlowWorker.Shared.Entities;

/// <summary>
/// 消息角色枚举
/// </summary>
public enum MessageRole
{
    /// <summary>
    /// 系统消息
    /// </summary>
    System,

    /// <summary>
    /// 用户消息
    /// </summary>
    User,

    /// <summary>
    /// 助手消息
    /// </summary>
    Assistant,

    /// <summary>
    /// 工具消息
    /// </summary>
    Tool
}

/// <summary>
/// 消息实体
/// </summary>
public class Message
{
    /// <summary>
    /// 主键
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 关联的会话 ID
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// 角色（user/assistant/system/tool）
    /// </summary>
    public MessageRole Role { get; set; }

    /// <summary>
    /// 消息内容
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 消耗的 Token 数
    /// </summary>
    public int? Tokens { get; set; }

    /// <summary>
    /// 使用的模型
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 扩展元数据（JSON 格式）
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// 关联的会话
    /// </summary>
    [JsonIgnore]
    public Session? Session { get; set; }
}