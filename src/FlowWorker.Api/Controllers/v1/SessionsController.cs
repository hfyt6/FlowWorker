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
    /// 创建新会话（单聊）
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
    /// 创建群聊会话
    /// </summary>
    /// <param name="request">创建请求</param>
    /// <returns>会话 ID</returns>
    [HttpPost("group")]
    public async Task<ActionResult<Guid>> CreateGroupSession(CreateGroupSessionRequest request)
    {
        var id = await _sessionService.CreateGroupSessionAsync(request);
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

    /// <summary>
    /// 获取会话参与者列表
    /// </summary>
    /// <param name="id">会话 ID</param>
    /// <returns>参与者列表</returns>
    [HttpGet("{id}/members")]
    public async Task<ActionResult<IEnumerable<SessionMemberDto>>> GetSessionMembers(Guid id)
    {
        var members = await _sessionService.GetSessionMembersAsync(id);
        return Ok(members);
    }

    /// <summary>
    /// 添加参与者到会话
    /// </summary>
    /// <param name="id">会话 ID</param>
    /// <param name="request">添加请求</param>
    /// <returns></returns>
    [HttpPost("{id}/members")]
    public async Task<IActionResult> AddMemberToSession(Guid id, AddSessionMemberRequest request)
    {
        await _sessionService.AddMemberToSessionAsync(id, request.MemberId);
        return NoContent();
    }

    /// <summary>
    /// 从会话中移除参与者
    /// </summary>
    /// <param name="id">会话 ID</param>
    /// <param name="memberId">成员 ID</param>
    /// <returns></returns>
    [HttpDelete("{id}/members/{memberId}")]
    public async Task<IActionResult> RemoveMemberFromSession(Guid id, Guid memberId)
    {
        await _sessionService.RemoveMemberFromSessionAsync(id, memberId);
        return NoContent();
    }
}
