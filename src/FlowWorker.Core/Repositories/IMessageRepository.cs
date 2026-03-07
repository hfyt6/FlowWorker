using FlowWorker.Shared.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace FlowWorker.Core.Repositories;

/// <summary>
/// 消息仓储接口
/// </summary>
public interface IMessageRepository
{
    /// <summary>
    /// 根据主键获取实体
    /// </summary>
    /// <param name="id">主键</param>
    /// <returns>实体或 null</returns>
    Task<Message?> GetByIdAsync(Guid id);

    /// <summary>
    /// 获取所有实体
    /// </summary>
    /// <returns>实体列表</returns>
    Task<IReadOnlyList<Message>> GetAllAsync();

    /// <summary>
    /// 根据条件获取实体
    /// </summary>
    /// <param name="predicate">条件表达式</param>
    /// <returns>实体列表</returns>
    Task<IReadOnlyList<Message>> GetByConditionAsync(Expression<Func<Message, bool>> predicate);

    /// <summary>
    /// 根据条件获取第一个实体
    /// </summary>
    /// <param name="predicate">条件表达式</param>
    /// <returns>实体或 null</returns>
    Task<Message?> GetFirstAsync(Expression<Func<Message, bool>> predicate);

    /// <summary>
    /// 添加实体
    /// </summary>
    /// <param name="entity">实体</param>
    Task AddAsync(Message entity);

    /// <summary>
    /// 添加多个实体
    /// </summary>
    /// <param name="entities">实体列表</param>
    Task AddRangeAsync(IEnumerable<Message> entities);

    /// <summary>
    /// 更新实体
    /// </summary>
    /// <param name="entity">实体</param>
    Task UpdateAsync(Message entity);

    /// <summary>
    /// 删除实体
    /// </summary>
    /// <param name="entity">实体</param>
    Task DeleteAsync(Message entity);

    /// <summary>
    /// 删除多个实体
    /// </summary>
    /// <param name="entities">实体列表</param>
    Task DeleteRangeAsync(IEnumerable<Message> entities);

    /// <summary>
    /// 检查实体是否存在
    /// </summary>
    /// <param name="predicate">条件表达式</param>
    /// <returns>是否存在</returns>
    Task<bool> ExistsAsync(Expression<Func<Message, bool>> predicate);

    /// <summary>
    /// 计算实体数量
    /// </summary>
    /// <param name="predicate">条件表达式</param>
    /// <returns>数量</returns>
    Task<int> CountAsync(Expression<Func<Message, bool>>? predicate = null);

    /// <summary>
    /// 根据会话 ID 获取消息列表
    /// </summary>
    /// <param name="sessionId">会话 ID</param>
    /// <returns>消息列表</returns>
    Task<IReadOnlyList<Message>> GetBySessionIdAsync(Guid sessionId);

    /// <summary>
    /// 获取会话的最后 N 条消息
    /// </summary>
    /// <param name="sessionId">会话 ID</param>
    /// <param name="count">数量</param>
    /// <returns>消息列表</returns>
    Task<IReadOnlyList<Message>> GetLastMessagesAsync(Guid sessionId, int count);

    /// <summary>
    /// 删除会话的所有消息
    /// </summary>
    /// <param name="sessionId">会话 ID</param>
    Task DeleteBySessionIdAsync(Guid sessionId);
}