using FlowWorker.Core.DTOs;
using FlowWorker.Core.Repositories;
using FlowWorker.Shared.Entities;

namespace FlowWorker.Core.Interfaces;

/// <summary>
/// 消息服务接口
/// </summary>
public interface IMessageService
{
    /// <summary>
    /// 获取消息列表
    /// </summary>
    /// <param name="sessionId">会话 ID</param>
    /// <returns>消息列表</returns>
    Task<IReadOnlyList<MessageListItemDto>> GetMessagesAsync(Guid sessionId);

    /// <summary>
    /// 获取会话的最后 N 条消息
    /// </summary>
    /// <param name="sessionId">会话 ID</param>
    /// <param name="count">数量</param>
    /// <returns>消息列表</returns>
    Task<IReadOnlyList<MessageListItemDto>> GetLastMessagesAsync(Guid sessionId, int count);

    /// <summary>
    /// 创建消息
    /// </summary>
    /// <param name="sessionId">会话 ID</param>
    /// <param name="request">创建请求</param>
    /// <returns>消息 ID</returns>
    Task<Guid> CreateMessageAsync(Guid sessionId, CreateMessageRequest request);

    /// <summary>
    /// 删除消息
    /// </summary>
    /// <param name="id">消息 ID</param>
    Task DeleteMessageAsync(Guid id);

    /// <summary>
    /// 发送消息到 AI
    /// </summary>
    /// <param name="sessionId">会话 ID</param>
    /// <param name="content">消息内容</param>
    /// <returns>AI 响应</returns>
    Task<SendMessageResponse> SendMessageAsync(Guid sessionId, string content);

    /// <summary>
    /// 重新生成回复
    /// </summary>
    /// <param name="sessionId">会话 ID</param>
    /// <returns>AI 响应</returns>
    Task<SendMessageResponse> RegenerateResponseAsync(Guid sessionId);
}