using FlowWorker.Core.DTOs;
using FlowWorker.Core.Repositories;
using FlowWorker.Shared.Entities;

namespace FlowWorker.Core.Interfaces;

/// <summary>
/// API 配置服务接口
/// </summary>
public interface IApiConfigService
{
    /// <summary>
    /// 获取配置列表
    /// </summary>
    /// <returns>配置列表</returns>
    Task<IReadOnlyList<ApiConfigListItemDto>> GetConfigsAsync();

    /// <summary>
    /// 获取配置详情
    /// </summary>
    /// <param name="id">配置 ID</param>
    /// <returns>配置详情</returns>
    Task<ApiConfigDetailDto?> GetConfigDetailAsync(Guid id);

    /// <summary>
    /// 获取默认配置
    /// </summary>
    /// <returns>默认配置或 null</returns>
    Task<ApiConfigDetailDto?> GetDefaultConfigAsync();

    /// <summary>
    /// 创建配置
    /// </summary>
    /// <param name="request">创建请求</param>
    /// <returns>配置 ID</returns>
    Task<Guid> CreateConfigAsync(CreateApiConfigRequest request);

    /// <summary>
    /// 更新配置
    /// </summary>
    /// <param name="id">配置 ID</param>
    /// <param name="request">更新请求</param>
    Task UpdateConfigAsync(Guid id, UpdateApiConfigRequest request);

    /// <summary>
    /// 删除配置
    /// </summary>
    /// <param name="id">配置 ID</param>
    Task DeleteConfigAsync(Guid id);

    /// <summary>
    /// 设置默认配置
    /// </summary>
    /// <param name="id">配置 ID</param>
    Task SetDefaultConfigAsync(Guid id);
}