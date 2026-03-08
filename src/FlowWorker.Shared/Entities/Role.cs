using System.Text.Json.Serialization;

namespace FlowWorker.Shared.Entities;

/// <summary>
/// 角色实体
/// </summary>
public class Role
{
    /// <summary>
    /// 主键
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 角色名称（coder/ui-designer/architect/reviewer/general等）
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 显示名称
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 角色描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 系统提示词模板
    /// </summary>
    public string SystemPrompt { get; set; } = string.Empty;

    /// <summary>
    /// 允许使用的工具列表（JSON格式）
    /// </summary>
    public string? AllowedTools { get; set; }

    /// <summary>
    /// 是否为内置角色
    /// </summary>
    public bool IsBuiltIn { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 关联的AI成员列表
    /// </summary>
    [JsonIgnore]
    public virtual ICollection<Member> Members { get; set; } = new List<Member>();
}
