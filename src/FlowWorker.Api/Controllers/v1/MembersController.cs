using Microsoft.AspNetCore.Mvc;
using FlowWorker.Core.DTOs;
using FlowWorker.Core.Interfaces;
using FlowWorker.Shared.Enums;

namespace FlowWorker.Api.Controllers.v1;

/// <summary>
/// 成员管理 API
/// </summary>
[ApiController]
[Route("api/v1/members")]
public class MembersController : ControllerBase
{
    private readonly IMemberService _memberService;
    private readonly ILogger<MembersController> _logger;

    public MembersController(
        IMemberService memberService,
        ILogger<MembersController> logger)
    {
        _memberService = memberService;
        _logger = logger;
    }

    /// <summary>
    /// 获取所有成员列表
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MemberListItemDto>>> GetAll()
    {
        var members = await _memberService.GetAllMembersAsync();
        return Ok(members);
    }

    /// <summary>
    /// 根据类型获取成员列表
    /// </summary>
    [HttpGet("by-type/{type}")]
    public async Task<ActionResult<IReadOnlyList<MemberListItemDto>>> GetByType(MemberType type)
    {
        var members = await _memberService.GetMembersByTypeAsync(type);
        return Ok(members);
    }

    /// <summary>
    /// 获取成员详情
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MemberDetailDto>> GetById(Guid id)
    {
        var member = await _memberService.GetMemberByIdAsync(id);
        if (member == null)
            return NotFound();

        return Ok(member);
    }

    /// <summary>
    /// 创建AI成员
    /// </summary>
    [HttpPost("ai")]
    public async Task<ActionResult<Guid>> CreateAI([FromBody] CreateAIMemberRequest request)
    {
        try
        {
            var id = await _memberService.CreateAIMemberAsync(request);
            return CreatedAtAction(nameof(GetById), new { id }, id);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to create AI member");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 创建用户成员
    /// </summary>
    [HttpPost("user")]
    public async Task<ActionResult<Guid>> CreateUser([FromBody] CreateUserMemberRequest request)
    {
        try
        {
            var id = await _memberService.CreateUserMemberAsync(request.Name, request.Avatar);
            return CreatedAtAction(nameof(GetById), new { id }, id);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to create user member");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 更新成员信息
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateMemberRequest request)
    {
        try
        {
            await _memberService.UpdateMemberAsync(id, request);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to update member");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 删除AI成员
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _memberService.DeleteAIMemberAsync(id);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to delete member");
            return BadRequest(new { error = ex.Message });
        }
    }
}

/// <summary>
/// 创建用户成员请求
/// </summary>
public class CreateUserMemberRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Avatar { get; set; }
}
