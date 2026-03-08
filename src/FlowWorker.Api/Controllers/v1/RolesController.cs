using Microsoft.AspNetCore.Mvc;
using FlowWorker.Core.DTOs;
using FlowWorker.Core.Interfaces;

namespace FlowWorker.Api.Controllers.v1;

/// <summary>
/// 角色管理 API
/// </summary>
[ApiController]
[Route("api/v1/roles")]
public class RolesController : ControllerBase
{
    private readonly IRoleService _roleService;
    private readonly ILogger<RolesController> _logger;

    public RolesController(
        IRoleService roleService,
        ILogger<RolesController> logger)
    {
        _roleService = roleService;
        _logger = logger;
    }

    /// <summary>
    /// 获取所有角色列表
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RoleListItemDto>>> GetAll()
    {
        var roles = await _roleService.GetAllRolesAsync();
        return Ok(roles);
    }

    /// <summary>
    /// 获取内置角色列表
    /// </summary>
    [HttpGet("built-in")]
    public async Task<ActionResult<IReadOnlyList<RoleListItemDto>>> GetBuiltIn()
    {
        var roles = await _roleService.GetBuiltInRolesAsync();
        return Ok(roles);
    }

    /// <summary>
    /// 获取自定义角色列表
    /// </summary>
    [HttpGet("custom")]
    public async Task<ActionResult<IReadOnlyList<RoleListItemDto>>> GetCustom()
    {
        var roles = await _roleService.GetCustomRolesAsync();
        return Ok(roles);
    }

    /// <summary>
    /// 获取角色详情
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RoleDetailDto>> GetById(Guid id)
    {
        var role = await _roleService.GetRoleByIdAsync(id);
        if (role == null)
            return NotFound();

        return Ok(role);
    }

    /// <summary>
    /// 根据名称获取角色
    /// </summary>
    [HttpGet("by-name/{name}")]
    public async Task<ActionResult<RoleDetailDto>> GetByName(string name)
    {
        var role = await _roleService.GetRoleByNameAsync(name);
        if (role == null)
            return NotFound();

        return Ok(role);
    }

    /// <summary>
    /// 创建自定义角色
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateRoleRequest request)
    {
        try
        {
            var id = await _roleService.CreateRoleAsync(request);
            return CreatedAtAction(nameof(GetById), new { id }, id);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to create role");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 更新角色配置
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRoleRequest request)
    {
        try
        {
            await _roleService.UpdateRoleAsync(id, request);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to update role");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 删除自定义角色
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _roleService.DeleteRoleAsync(id);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to delete role");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 获取角色的系统提示词模板
    /// </summary>
    [HttpGet("{id:guid}/system-prompt")]
    public async Task<ActionResult<string>> GetSystemPrompt(Guid id)
    {
        var prompt = await _roleService.GetSystemPromptTemplateAsync(id);
        if (prompt == null)
            return NotFound();

        return Ok(new { systemPrompt = prompt });
    }

    /// <summary>
    /// 初始化内置角色
    /// </summary>
    [HttpPost("initialize")]
    public async Task<IActionResult> Initialize()
    {
        await _roleService.InitializeBuiltInRolesAsync();
        return NoContent();
    }
}
