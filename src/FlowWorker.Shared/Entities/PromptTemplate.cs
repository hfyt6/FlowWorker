namespace FlowWorker.Shared.Entities;

/// <summary>
/// 提示词模板实体
/// </summary>
public class PromptTemplate
{
    /// <summary>
    /// 主键
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 模板名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 所属角色（coder/ui-designer/architect/reviewer/general等）
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// 模板类型（system/template/example）
    /// </summary>
    public string TemplateType { get; set; } = string.Empty;

    /// <summary>
    /// 模板内容
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 模板变量（JSON格式存储）
    /// </summary>
    public string? Variables { get; set; }

    /// <summary>
    /// 是否为内置模板
    /// </summary>
    public bool IsBuiltIn { get; set; }

    /// <summary>
    /// 模板描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 版本号
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// 父模板ID（用于版本管理）
    /// </summary>
    public Guid? ParentId { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
