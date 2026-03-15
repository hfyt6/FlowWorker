using System.Text.Json.Serialization;
using FlowWorker.Shared.Enums;

namespace FlowWorker.Shared.Entities;

/// <summary>
/// 会话实体
/// </summary>
public class Session
{
    /// <summary>
    /// 主键
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 会话标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 会话类型（Single/Group）
    /// </summary>
    public SessionType Type { get; set; } = SessionType.Single;

    /// <summary>
    /// 创建者（User Member ID）
    /// </summary>
    public Guid CreatedBy { get; set; }

    /// <summary>
    /// 关联的 API 配置 ID（单聊时必填，群聊时为 null，因为每个 AI 成员有自己的 API 配置）
    /// </summary>
    public Guid? ApiConfigId { get; set; }

    /// <summary>
    /// 使用的模型（单聊时必填，群聊时为 null）
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

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 扩展元数据（JSON 格式）
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// 会话工作目录路径，限定AI工具的文件操作范围
    /// </summary>
    public string WorkingDirectory { get; set; } = string.Empty;

    /// <summary>
    /// 关联的 API 配置
    /// </summary>
    [JsonIgnore]
    public virtual ApiConfig? ApiConfig { get; set; }

    /// <summary>
    /// 关联的消息列表
    /// </summary>
    [JsonIgnore]
    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();

    /// <summary>
    /// 关联的会话成员列表
    /// </summary>
    [JsonIgnore]
    public virtual ICollection<SessionMember> SessionMembers { get; set; } = new List<SessionMember>();
}
