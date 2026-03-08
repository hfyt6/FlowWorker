using FlowWorker.Core.DTOs;
using FlowWorker.Core.Interfaces;
using FlowWorker.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace FlowWorker.Api.Controllers.v1;

/// <summary>
/// 消息管理 API
/// </summary>
[ApiController]
[Route("api/v1/sessions/{sessionId}/messages")]
public class MessagesController : ControllerBase
{
    private readonly IMessageService _messageService;
    private readonly ILogger<MessagesController> _logger;

    public MessagesController(IMessageService messageService, ILogger<MessagesController> logger)
    {
        _messageService = messageService;
        _logger = logger;
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
    /// <param name="request">发送消息请求</param>
    /// <returns>AI 响应</returns>
    [HttpPost("send")]
    public async Task<ActionResult<SendMessageResponse>> SendMessage(Guid sessionId, [FromBody] SendMessageRequest request)
    {
        var response = await _messageService.SendMessageAsync(sessionId, request.Content);
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

    /// <summary>
    /// 流式发送消息到 AI
    /// </summary>
    /// <param name="sessionId">会话 ID</param>
    /// <param name="request">发送消息请求</param>
    /// <returns>流式 AI 响应</returns>
    [HttpPost("send-stream")]
    public async Task SendMessageStream(Guid sessionId, [FromBody] SendMessageRequest request, CancellationToken cancellationToken)
    {
        // 设置响应头，确保流式传输
        Response.ContentType = "application/x-ndjson";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["Connection"] = "keep-alive";
        Response.Headers["X-Accel-Buffering"] = "no"; // 禁用 Nginx 缓冲
        
        await _messageService.SendMessageStreamAsync(sessionId, request.Content, async chunk =>
        {
            // 将每个 chunk 序列化为 JSON 并写入响应流
            var json = JsonSerializer.Serialize(chunk, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            });
            await Response.WriteAsync(json, cancellationToken);
            await Response.WriteAsync("\n", cancellationToken); // NDJSON 格式：每行一个 JSON 对象
            await Response.Body.FlushAsync(cancellationToken); // 立即刷新到客户端
        });
    }

    /// <summary>
    /// 流式重新生成回复
    /// </summary>
    /// <param name="sessionId">会话 ID</param>
    /// <returns>流式 AI 响应</returns>
    [HttpPost("regenerate-stream")]
    public async Task RegenerateResponseStream(Guid sessionId, CancellationToken cancellationToken)
    {
        // 设置响应头，确保流式传输
        Response.ContentType = "application/x-ndjson";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["Connection"] = "keep-alive";
        Response.Headers["X-Accel-Buffering"] = "no"; // 禁用 Nginx 缓冲
        
        await _messageService.RegenerateResponseStreamAsync(sessionId, async chunk =>
        {
            // 将每个 chunk 序列化为 JSON 并写入响应流
            var json = JsonSerializer.Serialize(chunk, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            });
            await Response.WriteAsync(json, cancellationToken);
            await Response.WriteAsync("\n", cancellationToken); // NDJSON 格式：每行一个 JSON 对象
            await Response.Body.FlushAsync(cancellationToken); // 立即刷新到客户端
        });
    }

    /// <summary>
    /// 发送群聊消息（支持多AI响应）- 流式
    /// </summary>
    /// <param name="sessionId">会话 ID</param>
    /// <param name="request">发送消息请求</param>
    /// <returns>流式多AI响应</returns>
    [HttpPost("send-group-stream")]
    public async Task SendMessageGroupStream(Guid sessionId, [FromBody] SendMessageRequest request, CancellationToken cancellationToken)
    {
        // 设置响应头，确保流式传输
        Response.ContentType = "application/x-ndjson";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["Connection"] = "keep-alive";
        Response.Headers["X-Accel-Buffering"] = "no"; // 禁用 Nginx 缓冲
        
        await _messageService.SendMessageGroupStreamAsync(
            sessionId,
            request.Content,
            request.SenderMemberId,
            async (chunk, memberId) =>
            {
                // 添加成员ID到响应中
                var responseChunk = new
                {
                    chunk.Type,
                    chunk.Content,
                    chunk.MessageId,
                    MemberId = memberId != Guid.Empty ? memberId.ToString() : chunk.MemberId,
                    chunk.MemberName,
                    chunk.Model,
                    chunk.FinishReason
                };
                
                // 将每个 chunk 序列化为 JSON 并写入响应流
                var json = JsonSerializer.Serialize(responseChunk, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                });
                await Response.WriteAsync(json, cancellationToken);
                await Response.WriteAsync("\n", cancellationToken); // NDJSON 格式：每行一个 JSON 对象
                await Response.Body.FlushAsync(cancellationToken); // 立即刷新到客户端
            });
    }

    /// <summary>
    /// 发送群聊消息（支持多AI响应）- 非流式
    /// </summary>
    /// <param name="sessionId">会话 ID</param>
    /// <param name="request">发送消息请求</param>
    /// <returns>多AI响应列表</returns>
    [HttpPost("group")]
    public async Task<ActionResult<List<SendMessageResponse>>> SendMessageGroup(Guid sessionId, [FromBody] SendMessageRequest request)
    {
        var responses = await _messageService.SendMessageGroupAsync(
            sessionId,
            request.Content,
            request.SenderMemberId);
        return Ok(responses);
    }
}
