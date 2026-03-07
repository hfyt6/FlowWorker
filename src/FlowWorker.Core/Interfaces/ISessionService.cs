using FlowWorker.Core.DTOs;
using FlowWorker.Core.Repositories;
using FlowWorker.Shared.Entities;

namespace FlowWorker.Core.Interfaces;

/// <summary>
/// 会话服务接口
/// </summary>
public interface ISessionService
{
    /// <summary>
    /// 获取会话列表
    /// </summary>
    /// <param name="apiConfigId">API 配置 ID（可选）</param>
    /// <returns>会话列表</returns>
    Task<IReadOnlyList<SessionListItemDto>> GetSessionsAsync(Guid? apiConfigId = null);

    /// <summary>
    /// 获取会话详情
    /// </summary>
    /// <param name="id">会话 ID</param>
    /// <returns>会话详情</returns>
    Task<SessionDetailDto?> GetSessionDetailAsync(Guid id);

    /// <summary>
    /// 创建新会话
    /// </summary>
    /// <param name="request">创建请求</param>
    /// <returns>会话 ID</returns>
    Task<Guid> CreateSessionAsync(CreateSessionRequest request);

    /// <summary>
    /// 更新会话
    /// </summary>
    /// <param name="id">会话 ID</param>
    /// <param name="request">更新请求</param>
    Task UpdateSessionAsync(Guid id, UpdateSessionRequest request);

    /// <summary>
    /// 删除会话
    /// </summary>
    /// <param name="id">会话 ID</param>
    Task DeleteSessionAsync(Guid id);

    /// <summary>
    /// 搜索会话
    /// </summary>
    /// <param name="title">标题关键词</param>
    /// <returns>会话列表</returns>
    Task<IReadOnlyList<SessionListItemDto>> SearchSessionsAsync(string title);
}