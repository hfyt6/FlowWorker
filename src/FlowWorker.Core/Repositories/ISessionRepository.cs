using FlowWorker.Shared.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace FlowWorker.Core.Repositories;

/// <summary>
/// 会话仓储接口
/// </summary>
public interface ISessionRepository
{
    /// <summary>
    /// 根据主键获取实体
    /// </summary>
    /// <param name="id">主键</param>
    /// <returns>实体或 null</returns>
    Task<Session?> GetByIdAsync(Guid id);

    /// <summary>
    /// 获取所有实体
    /// </summary>
    /// <returns>实体列表</returns>
    Task<IReadOnlyList<Session>> GetAllAsync();

    /// <summary>
    /// 根据条件获取实体
    /// </summary>
    /// <param name="predicate">条件表达式</param>
    /// <returns>实体列表</returns>
    Task<IReadOnlyList<Session>> GetByConditionAsync(Expression<Func<Session, bool>> predicate);

    /// <summary>
    /// 根据条件获取第一个实体
    /// </summary>
    /// <param name="predicate">条件表达式</param>
    /// <returns>实体或 null</returns>
    Task<Session?> GetFirstAsync(Expression<Func<Session, bool>> predicate);

    /// <summary>
    /// 添加实体
    /// </summary>
    /// <param name="entity">实体</param>
    Task AddAsync(Session entity);

    /// <summary>
    /// 添加多个实体
    /// </summary>
    /// <param name="entities">实体列表</param>
    Task AddRangeAsync(IEnumerable<Session> entities);

    /// <summary>
    /// 更新实体
    /// </summary>
    /// <param name="entity">实体</param>
    Task UpdateAsync(Session entity);

    /// <summary>
    /// 删除实体
    /// </summary>
    /// <param name="entity">实体</param>
    Task DeleteAsync(Session entity);

    /// <summary>
    /// 删除多个实体
    /// </summary>
    /// <param name="entities">实体列表</param>
    Task DeleteRangeAsync(IEnumerable<Session> entities);

    /// <summary>
    /// 检查实体是否存在
    /// </summary>
    /// <param name="predicate">条件表达式</param>
    /// <returns>是否存在</returns>
    Task<bool> ExistsAsync(Expression<Func<Session, bool>> predicate);

    /// <summary>
    /// 计算实体数量
    /// </summary>
    /// <param name="predicate">条件表达式</param>
    /// <returns>数量</returns>
    Task<int> CountAsync(Expression<Func<Session, bool>>? predicate = null);

    /// <summary>
    /// 根据 API 配置 ID 获取会话列表
    /// </summary>
    /// <param name="apiConfigId">API 配置 ID</param>
    /// <returns>会话列表</returns>
    Task<IReadOnlyList<Session>> GetByApiConfigIdAsync(Guid apiConfigId);

    /// <summary>
    /// 获取最新的会话列表
    /// </summary>
    /// <param name="count">数量</param>
    /// <returns>会话列表</returns>
    Task<IReadOnlyList<Session>> GetLatestSessionsAsync(int count);

    /// <summary>
    /// 搜索会话
    /// </summary>
    /// <param name="title">标题关键词</param>
    /// <returns>会话列表</returns>
    Task<IReadOnlyList<Session>> SearchAsync(string title);

    /// <summary>
    /// 获取群聊会话列表
    /// </summary>
    Task<IReadOnlyList<Session>> GetGroupSessionsAsync();

    /// <summary>
    /// 获取单聊会话列表
    /// </summary>
    Task<IReadOnlyList<Session>> GetSingleSessionsAsync();

    /// <summary>
    /// 获取会话及其成员
    /// </summary>
    Task<Session?> GetSessionWithMembersAsync(Guid sessionId);

    /// <summary>
    /// 添加成员到会话
    /// </summary>
    Task AddMemberAsync(Guid sessionId, Guid memberId);

    /// <summary>
    /// 从会话中移除成员
    /// </summary>
    Task RemoveMemberAsync(Guid sessionId, Guid memberId);
}
