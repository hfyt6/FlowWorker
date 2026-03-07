using System.Text.Json.Serialization;

namespace FlowWorker.Shared.Entities;

/// <summary>
/// API 配置实体
/// </summary>
public class ApiConfig
{
    /// <summary>
    /// 主键
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 配置名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// API 基础 URL
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// API 密钥（加密存储）
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// 默认模型
    /// </summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// 是否为默认配置
    /// </summary>
    public bool IsDefault { get; set; }

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
    /// 关联的会话列表
    /// </summary>
    [JsonIgnore]
    public ICollection<Session> Sessions { get; set; } = new List<Session>();
}