using System.Linq.Expressions;
using FlowWorker.Shared.Entities;

namespace FlowWorker.Core.Repositories;

/// <summary>
/// 角色仓储接口
/// </summary>
public interface IRoleRepository
{
    Task<Role?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<Role>> GetAllAsync();
    Task<IReadOnlyList<Role>> GetByConditionAsync(Expression<Func<Role, bool>> predicate);
    Task<Role?> GetFirstAsync(Expression<Func<Role, bool>> predicate);
    Task AddAsync(Role entity);
    Task AddRangeAsync(IEnumerable<Role> entities);
    Task UpdateAsync(Role entity);
    Task DeleteAsync(Role entity);
    Task DeleteRangeAsync(IEnumerable<Role> entities);
    Task<bool> ExistsAsync(Expression<Func<Role, bool>> predicate);
    Task<int> CountAsync(Expression<Func<Role, bool>>? predicate = null);

    /// <summary>
    /// 根据名称获取角色
    /// </summary>
    Task<Role?> GetByNameAsync(string name);

    /// <summary>
    /// 获取所有内置角色
    /// </summary>
    Task<IReadOnlyList<Role>> GetBuiltInRolesAsync();

    /// <summary>
    /// 获取所有自定义角色
    /// </summary>
    Task<IReadOnlyList<Role>> GetCustomRolesAsync();

    /// <summary>
    /// 检查角色是否被使用
    /// </summary>
    Task<bool> IsRoleInUseAsync(Guid roleId);
}
