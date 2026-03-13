namespace FlowWorker.Shared.Entities;

/// <summary>
/// 角色提示词配置实体
/// </summary>
public class RolePromptConfig
{
    /// <summary>
    /// 主键
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 角色ID
    /// </summary>
    public Guid RoleId { get; set; }

    /// <summary>
    /// 当前模式（code/architect/ask）
    /// </summary>
    public string CurrentMode { get; set; } = "code";

    /// <summary>
    /// 自定义指令
    /// </summary>
    public string? CustomInstructions { get; set; }

    /// <summary>
    /// 最大迭代次数
    /// </summary>
    public int MaxIterations { get; set; } = 10;

    /// <summary>
    /// Token预算
    /// </summary>
    public int TokenBudget { get; set; } = 4000;

    /// <summary>
    /// 工具白名单（JSON格式）
    /// </summary>
    public string? ToolWhitelist { get; set; }

    /// <summary>
    /// 工具黑名单（JSON格式）
    /// </summary>
    public string? ToolBlacklist { get; set; }

    /// <summary>
    /// 是否启用思维链
    /// </summary>
    public bool EnableChainOfThought { get; set; } = true;

    /// <summary>
    /// 是否启用任务进度追踪
    /// </summary>
    public bool EnableTaskProgress { get; set; } = true;

    /// <summary>
    /// 是否启用自我反思
    /// </summary>
    public bool EnableSelfReflection { get; set; } = false;

    /// <summary>
    /// 高级提示词技术配置（JSON格式）
    /// </summary>
    public string? AdvancedTechniques { get; set; }

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
    public virtual Role? Role { get; set; }
}
