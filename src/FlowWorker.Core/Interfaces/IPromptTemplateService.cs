using FlowWorker.Core.DTOs;

namespace FlowWorker.Core.Interfaces;

/// <summary>
/// 提示词模板服务接口
/// </summary>
public interface IPromptTemplateService
{
    /// <summary>
    /// 获取所有提示词模板
    /// </summary>
    Task<IReadOnlyList<PromptTemplateListItemDto>> GetAllTemplatesAsync();

    /// <summary>
    /// 根据角色获取模板
    /// </summary>
    Task<IReadOnlyList<PromptTemplateListItemDto>> GetTemplatesByRoleAsync(string role);

    /// <summary>
    /// 获取模板详情
    /// </summary>
    Task<PromptTemplateDetailDto?> GetTemplateByIdAsync(Guid id);

    /// <summary>
    /// 创建模板
    /// </summary>
    Task<Guid> CreateTemplateAsync(CreatePromptTemplateRequest request);

    /// <summary>
    /// 更新模板
    /// </summary>
    Task UpdateTemplateAsync(Guid id, UpdatePromptTemplateRequest request);

    /// <summary>
    /// 删除模板
    /// </summary>
    Task DeleteTemplateAsync(Guid id);

    /// <summary>
    /// 获取角色的系统提示词
    /// </summary>
    Task<string?> GetSystemPromptByRoleAsync(string role);

    /// <summary>
    /// 渲染提示词模板
    /// </summary>
    Task<string> RenderTemplateAsync(string template, Dictionary<string, string> variables);

    /// <summary>
    /// 预览渲染后的提示词
    /// </summary>
    Task<PreviewPromptTemplateResponse> PreviewTemplateAsync(PreviewPromptTemplateRequest request);

    /// <summary>
    /// 获取模板变量列表
    /// </summary>
    List<string> ExtractVariables(string template);

    /// <summary>
    /// 初始化内置提示词模板
    /// </summary>
    Task InitializeBuiltInTemplatesAsync();

    /// <summary>
    /// 重置角色提示词为内置模板
    /// </summary>
    Task ResetToBuiltInAsync(string role);

    /// <summary>
    /// 获取角色提示词配置
    /// </summary>
    Task<RolePromptConfigDto?> GetRolePromptConfigAsync(Guid roleId);

    /// <summary>
    /// 更新角色提示词配置
    /// </summary>
    Task UpdateRolePromptConfigAsync(Guid roleId, UpdateRolePromptConfigRequest request);

    /// <summary>
    /// 渲染完整的角色提示词（包含配置）
    /// </summary>
    Task<RenderedPromptDto> RenderRolePromptAsync(Guid roleId, Dictionary<string, string>? variables = null);
}
