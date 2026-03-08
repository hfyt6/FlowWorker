using FlowWorker.Core.DTOs;

namespace FlowWorker.Core.Interfaces;

/// <summary>
/// 角色服务接口
/// </summary>
public interface IRoleService
{
    /// <summary>
    /// 获取所有角色列表
    /// </summary>
    Task<IReadOnlyList<RoleListItemDto>> GetAllRolesAsync();

    /// <summary>
    /// 获取内置角色列表
    /// </summary>
    Task<IReadOnlyList<RoleListItemDto>> GetBuiltInRolesAsync();

    /// <summary>
    /// 获取自定义角色列表
    /// </summary>
    Task<IReadOnlyList<RoleListItemDto>> GetCustomRolesAsync();

    /// <summary>
    /// 获取角色详情
    /// </summary>
    Task<RoleDetailDto?> GetRoleByIdAsync(Guid id);

    /// <summary>
    /// 根据名称获取角色
    /// </summary>
    Task<RoleDetailDto?> GetRoleByNameAsync(string name);

    /// <summary>
    /// 创建自定义角色
    /// </summary>
    Task<Guid> CreateRoleAsync(CreateRoleRequest request);

    /// <summary>
    /// 更新角色配置
    /// </summary>
    Task UpdateRoleAsync(Guid id, UpdateRoleRequest request);

    /// <summary>
    /// 删除自定义角色
    /// </summary>
    Task DeleteRoleAsync(Guid id);

    /// <summary>
    /// 获取角色的系统提示词模板
    /// </summary>
    Task<string?> GetSystemPromptTemplateAsync(Guid id);

    /// <summary>
    /// 初始化内置角色
    /// </summary>
    Task InitializeBuiltInRolesAsync();

    /// <summary>
    /// 检查角色是否存在
    /// </summary>
    Task<bool> ExistsAsync(Guid id);
}
