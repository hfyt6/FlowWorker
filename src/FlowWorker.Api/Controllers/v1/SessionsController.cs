using FlowWorker.Core.DTOs;
using FlowWorker.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FlowWorker.Api.Controllers.v1;

/// <summary>
/// 会话管理 API
/// </summary>
[ApiController]
[Route("api/v1/sessions")]
public class SessionsController : ControllerBase
{
    private readonly ISessionService _sessionService;

    public SessionsController(ISessionService sessionService)
    {
        _sessionService = sessionService;
    }

    /// <summary>
    /// 获取会话列表
    /// </summary>
    /// <param name="apiConfigId">API 配置 ID（可选）</param>
    /// <returns>会话列表</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SessionListItemDto>>> GetSessions([FromQuery] Guid? apiConfigId)
    {
        var sessions = await _sessionService.GetSessionsAsync(apiConfigId);
        return Ok(sessions);
    }

    /// <summary>
    /// 获取会话详情
    /// </summary>
    /// <param name="id">会话 ID</param>
    /// <returns>会话详情</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<SessionDetailDto>> GetSession(Guid id)
    {
        var session = await _sessionService.GetSessionDetailAsync(id);
        if (session == null)
        {
            return NotFound();
        }
        return Ok(session);
    }

    /// <summary>
    /// 创建新会话
    /// </summary>
    /// <param name="request">创建请求</param>
    /// <returns>会话 ID</returns>
    [HttpPost]
    public async Task<ActionResult<Guid>> CreateSession(CreateSessionRequest request)
    {
        var id = await _sessionService.CreateSessionAsync(request);
        return CreatedAtAction(nameof(GetSession), new { id = id }, id);
    }

    /// <summary>
    /// 更新会话
    /// </summary>
    /// <param name="id">会话 ID</param>
    /// <param name="request">更新请求</param>
    /// <returns></returns>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSession(Guid id, UpdateSessionRequest request)
    {
        await _sessionService.UpdateSessionAsync(id, request);
        return NoContent();
    }

    /// <summary>
    /// 删除会话
    /// </summary>
    /// <param name="id">会话 ID</param>
    /// <returns></returns>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSession(Guid id)
    {
        await _sessionService.DeleteSessionAsync(id);
        return NoContent();
    }

    /// <summary>
    /// 搜索会话
    /// </summary>
    /// <param name="title">标题关键词</param>
    /// <returns>会话列表</returns>
    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<SessionListItemDto>>> SearchSessions([FromQuery] string title)
    {
        var sessions = await _sessionService.SearchSessionsAsync(title);
        return Ok(sessions);
    }
}