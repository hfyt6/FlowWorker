using System.Linq.Expressions;
using FlowWorker.Shared.Entities;
using FlowWorker.Shared.Enums;

namespace FlowWorker.Core.Repositories;

/// <summary>
/// 成员仓储接口
/// </summary>
public interface IMemberRepository
{
    Task<Member?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<Member>> GetAllAsync();
    Task<IReadOnlyList<Member>> GetByConditionAsync(Expression<Func<Member, bool>> predicate);
    Task<Member?> GetFirstAsync(Expression<Func<Member, bool>> predicate);
    Task AddAsync(Member entity);
    Task AddRangeAsync(IEnumerable<Member> entities);
    Task UpdateAsync(Member entity);
    Task DeleteAsync(Member entity);
    Task DeleteRangeAsync(IEnumerable<Member> entities);
    Task<bool> ExistsAsync(Expression<Func<Member, bool>> predicate);
    Task<int> CountAsync(Expression<Func<Member, bool>>? predicate = null);

    /// <summary>
    /// 根据类型获取成员列表
    /// </summary>
    Task<IReadOnlyList<Member>> GetByTypeAsync(MemberType type);

    /// <summary>
    /// 根据角色ID获取成员列表
    /// </summary>
    Task<IReadOnlyList<Member>> GetByRoleIdAsync(Guid roleId);

    /// <summary>
    /// 根据会话ID获取成员列表
    /// </summary>
    Task<IReadOnlyList<Member>> GetBySessionIdAsync(Guid sessionId);

    /// <summary>
    /// 获取AI成员列表（包含角色信息）
    /// </summary>
    Task<IReadOnlyList<Member>> GetAIMembersWithRoleAsync();
}
