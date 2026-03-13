using System;
using System.Collections.Generic;

namespace FlowWorker.Core.DTOs;

/// <summary>
/// 提示词模板列表项 DTO
/// </summary>
public class PromptTemplateListItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string TemplateType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsBuiltIn { get; set; }
    public int Version { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// 提示词模板详情 DTO
/// </summary>
public class PromptTemplateDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string TemplateType { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<string>? Variables { get; set; }
    public bool IsBuiltIn { get; set; }
    public string? Description { get; set; }
    public int Version { get; set; }
    public Guid? ParentId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// 创建提示词模板请求
/// </summary>
public class CreatePromptTemplateRequest
{
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string TemplateType { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<string>? Variables { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// 更新提示词模板请求
/// </summary>
public class UpdatePromptTemplateRequest
{
    public string Name { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<string>? Variables { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// 提示词模板预览请求
/// </summary>
public class PreviewPromptTemplateRequest
{
    public string Template { get; set; } = string.Empty;
    public Dictionary<string, string> Variables { get; set; } = new();
}

/// <summary>
/// 提示词模板预览响应
/// </summary>
public class PreviewPromptTemplateResponse
{
    public string RenderedContent { get; set; } = string.Empty;
    public List<string> DetectedVariables { get; set; } = new();
}

/// <summary>
/// 角色提示词配置 DTO
/// </summary>
public class RolePromptConfigDto
{
    public Guid Id { get; set; }
    public Guid RoleId { get; set; }
    public string CurrentMode { get; set; } = string.Empty;
    public string? CustomInstructions { get; set; }
    public int MaxIterations { get; set; }
    public int TokenBudget { get; set; }
    public List<string>? ToolWhitelist { get; set; }
    public List<string>? ToolBlacklist { get; set; }
    public bool EnableChainOfThought { get; set; }
    public bool EnableTaskProgress { get; set; }
    public bool EnableSelfReflection { get; set; }
    public Dictionary<string, object>? AdvancedTechniques { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// 更新角色提示词配置请求
/// </summary>
public class UpdateRolePromptConfigRequest
{
    public string? CurrentMode { get; set; }
    public string? CustomInstructions { get; set; }
    public int? MaxIterations { get; set; }
    public int? TokenBudget { get; set; }
    public List<string>? ToolWhitelist { get; set; }
    public List<string>? ToolBlacklist { get; set; }
    public bool? EnableChainOfThought { get; set; }
    public bool? EnableTaskProgress { get; set; }
    public bool? EnableSelfReflection { get; set; }
    public Dictionary<string, object>? AdvancedTechniques { get; set; }
}

/// <summary>
/// 渲染后的提示词 DTO
/// </summary>
public class RenderedPromptDto
{
    public string SystemPrompt { get; set; } = string.Empty;
    public string? CustomInstructions { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}
