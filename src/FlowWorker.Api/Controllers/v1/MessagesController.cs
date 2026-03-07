using FlowWorker.Core.DTOs;
using FlowWorker.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FlowWorker.Api.Controllers.v1;

/// <summary>
/// 消息管理 API
/// </summary>
[ApiController]
[Route("api/v1/sessions/{sessionId}/messages")]
public class MessagesController : ControllerBase
{
    private readonly IMessageService _messageService;

    public MessagesController(IMessageService messageService)
    {
        _messageService = messageService;
    }

    /// <summary>
    /// 获取消息列表
    /// </summary>
    /// <param name="sessionId">会话 ID</param>
    /// <returns>消息列表</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MessageListItemDto>>> GetMessages(Guid sessionId)
    {
        var messages = await _messageService.GetMessagesAsync(sessionId);
        return Ok(messages);
    }

    /// <summary>
    /// 获取最后 N 条消息
    /// </summary>
    /// <param name="sessionId">会话 ID</param>
    /// <param name="count">数量</param>
    /// <returns>消息列表</returns>
    [HttpGet("last")]
    public async Task<ActionResult<IEnumerable<MessageListItemDto>>> GetLastMessages(Guid sessionId, [FromQuery] int count = 10)
    {
        var messages = await _messageService.GetLastMessagesAsync(sessionId, count);
        return Ok(messages);
    }

    /// <summary>
    /// 创建消息
    /// </summary>
    /// <param name="sessionId">会话 ID</param>
    /// <param name="request">创建请求</param>
    /// <returns>消息 ID</returns>
    [HttpPost]
    public async Task<ActionResult<Guid>> CreateMessage(Guid sessionId, CreateMessageRequest request)
    {
        var id = await _messageService.CreateMessageAsync(sessionId, request);
        return CreatedAtAction(nameof(GetMessages), new { sessionId = sessionId, id = id }, id);
    }

    /// <summary>
    /// 删除消息
    /// </summary>
    /// <param name="id">消息 ID</param>
    /// <returns></returns>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteMessage(Guid id)
    {
        await _messageService.DeleteMessageAsync(id);
        return NoContent();
    }

    /// <summary>
    /// 发送消息到 AI
    /// </summary>
    /// <param name="sessionId">会话 ID</param>
    /// <param name="content">消息内容</param>
    /// <returns>AI 响应</returns>
    [HttpPost("send")]
    public async Task<ActionResult<SendMessageResponse>> SendMessage(Guid sessionId, [FromBody] string content)
    {
        var response = await _messageService.SendMessageAsync(sessionId, content);
        return Ok(response);
    }

    /// <summary>
    /// 重新生成回复
    /// </summary>
    /// <param name="sessionId">会话 ID</param>
    /// <returns>AI 响应</returns>
    [HttpPost("regenerate")]
    public async Task<ActionResult<SendMessageResponse>> RegenerateResponse(Guid sessionId)
    {
        var response = await _messageService.RegenerateResponseAsync(sessionId);
        return Ok(response);
    }
}