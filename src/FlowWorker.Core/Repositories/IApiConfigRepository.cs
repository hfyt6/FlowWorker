using FlowWorker.Shared.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace FlowWorker.Core.Repositories;

/// <summary>
/// API 配置仓储接口
/// </summary>
public interface IApiConfigRepository
{
    /// <summary>
    /// 根据主键获取实体
    /// </summary>
    /// <param name="id">主键</param>
    /// <returns>实体或 null</returns>
    Task<ApiConfig?> GetByIdAsync(Guid id);

    /// <summary>
    /// 获取所有实体
    /// </summary>
    /// <returns>实体列表</returns>
    Task<IReadOnlyList<ApiConfig>> GetAllAsync();

    /// <summary>
    /// 根据条件获取实体
    /// </summary>
    /// <param name="predicate">条件表达式</param>
    /// <returns>实体列表</returns>
    Task<IReadOnlyList<ApiConfig>> GetByConditionAsync(Expression<Func<ApiConfig, bool>> predicate);

    /// <summary>
    /// 根据条件获取第一个实体
    /// </summary>
    /// <param name="predicate">条件表达式</param>
    /// <returns>实体或 null</returns>
    Task<ApiConfig?> GetFirstAsync(Expression<Func<ApiConfig, bool>> predicate);

    /// <summary>
    /// 添加实体
    /// </summary>
    /// <param name="entity">实体</param>
    Task AddAsync(ApiConfig entity);

    /// <summary>
    /// 添加多个实体
    /// </summary>
    /// <param name="entities">实体列表</param>
    Task AddRangeAsync(IEnumerable<ApiConfig> entities);

    /// <summary>
    /// 更新实体
    /// </summary>
    /// <param name="entity">实体</param>
    Task UpdateAsync(ApiConfig entity);

    /// <summary>
    /// 删除实体
    /// </summary>
    /// <param name="entity">实体</param>
    Task DeleteAsync(ApiConfig entity);

    /// <summary>
    /// 删除多个实体
    /// </summary>
    /// <param name="entities">实体列表</param>
    Task DeleteRangeAsync(IEnumerable<ApiConfig> entities);

    /// <summary>
    /// 检查实体是否存在
    /// </summary>
    /// <param name="predicate">条件表达式</param>
    /// <returns>是否存在</returns>
    Task<bool> ExistsAsync(Expression<Func<ApiConfig, bool>> predicate);

    /// <summary>
    /// 计算实体数量
    /// </summary>
    /// <param name="predicate">条件表达式</param>
    /// <returns>数量</returns>
    Task<int> CountAsync(Expression<Func<ApiConfig, bool>>? predicate = null);

    /// <summary>
    /// 获取默认配置
    /// </summary>
    /// <returns>默认配置或 null</returns>
    Task<ApiConfig?> GetDefaultConfigAsync();

    /// <summary>
    /// 设置默认配置
    /// </summary>
    /// <param name="configId">配置 ID</param>
    Task SetDefaultConfigAsync(Guid configId);
}