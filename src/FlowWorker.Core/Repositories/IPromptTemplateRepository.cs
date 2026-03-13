using FlowWorker.Shared.Entities;

namespace FlowWorker.Core.Repositories;

/// <summary>
/// 提示词模板仓库接口
/// </summary>
public interface IPromptTemplateRepository
{
    /// <summary>
    /// 获取所有模板
    /// </summary>
    Task<IReadOnlyList<PromptTemplate>> GetAllAsync();

    /// <summary>
    /// 根据角色获取模板
    /// </summary>
    Task<IReadOnlyList<PromptTemplate>> GetByRoleAsync(string role);

    /// <summary>
    /// 根据角色和类型获取模板
    /// </summary>
    Task<IReadOnlyList<PromptTemplate>> GetByRoleAndTypeAsync(string role, string templateType);

    /// <summary>
    /// 根据ID获取模板
    /// </summary>
    Task<PromptTemplate?> GetByIdAsync(Guid id);

    /// <summary>
    /// 根据角色和名称获取模板
    /// </summary>
    Task<PromptTemplate?> GetByRoleAndNameAsync(string role, string name);

    /// <summary>
    /// 获取角色的系统提示词模板
    /// </summary>
    Task<PromptTemplate?> GetSystemTemplateByRoleAsync(string role);

    /// <summary>
    /// 添加模板
    /// </summary>
    Task AddAsync(PromptTemplate template);

    /// <summary>
    /// 更新模板
    /// </summary>
    Task UpdateAsync(PromptTemplate template);

    /// <summary>
    /// 删除模板
    /// </summary>
    Task DeleteAsync(PromptTemplate template);

    /// <summary>
    /// 检查模板是否存在
    /// </summary>
    Task<bool> ExistsAsync(Func<PromptTemplate, bool> predicate);

    /// <summary>
    /// 获取内置模板
    /// </summary>
    Task<IReadOnlyList<PromptTemplate>> GetBuiltInTemplatesAsync();

    /// <summary>
    /// 获取自定义模板
    /// </summary>
    Task<IReadOnlyList<PromptTemplate>> GetCustomTemplatesAsync();
}
