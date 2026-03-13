using Microsoft.AspNetCore.Mvc;
using FlowWorker.Core.DTOs;
using FlowWorker.Core.Interfaces;

namespace FlowWorker.Api.Controllers.v1;

/// <summary>
/// 提示词模板 API 控制器
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class PromptTemplatesController : ControllerBase
{
    private readonly IPromptTemplateService _promptTemplateService;

    public PromptTemplatesController(IPromptTemplateService promptTemplateService)
    {
        _promptTemplateService = promptTemplateService;
    }

    /// <summary>
    /// 获取所有提示词模板
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PromptTemplateListItemDto>>> GetAll()
    {
        var templates = await _promptTemplateService.GetAllTemplatesAsync();
        return Ok(templates);
    }

    /// <summary>
    /// 根据角色获取模板
    /// </summary>
    [HttpGet("by-role/{role}")]
    public async Task<ActionResult<IReadOnlyList<PromptTemplateListItemDto>>> GetByRole(string role)
    {
        var templates = await _promptTemplateService.GetTemplatesByRoleAsync(role);
        return Ok(templates);
    }

    /// <summary>
    /// 获取模板详情
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PromptTemplateDetailDto>> GetById(Guid id)
    {
        var template = await _promptTemplateService.GetTemplateByIdAsync(id);
        if (template == null)
            return NotFound();
        return Ok(template);
    }

    /// <summary>
    /// 创建模板
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreatePromptTemplateRequest request)
    {
        var id = await _promptTemplateService.CreateTemplateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    /// <summary>
    /// 更新模板
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdatePromptTemplateRequest request)
    {
        await _promptTemplateService.UpdateTemplateAsync(id, request);
        return NoContent();
    }

    /// <summary>
    /// 删除模板
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _promptTemplateService.DeleteTemplateAsync(id);
        return NoContent();
    }

    /// <summary>
    /// 预览模板渲染效果
    /// </summary>
    [HttpPost("preview")]
    public async Task<ActionResult<PreviewPromptTemplateResponse>> Preview(PreviewPromptTemplateRequest request)
    {
        var result = await _promptTemplateService.PreviewTemplateAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// 提取模板变量
    /// </summary>
    [HttpPost("extract-variables")]
    public ActionResult<List<string>> ExtractVariables([FromBody] string template)
    {
        var variables = _promptTemplateService.ExtractVariables(template);
        return Ok(variables);
    }

    /// <summary>
    /// 初始化内置模板
    /// </summary>
    [HttpPost("initialize")]
    public async Task<IActionResult> Initialize()
    {
        await _promptTemplateService.InitializeBuiltInTemplatesAsync();
        return NoContent();
    }

    /// <summary>
    /// 重置角色模板为内置版本
    /// </summary>
    [HttpPost("reset/{role}")]
    public async Task<IActionResult> ResetToBuiltIn(string role)
    {
        await _promptTemplateService.ResetToBuiltInAsync(role);
        return NoContent();
    }

    /// <summary>
    /// 获取角色的系统提示词
    /// </summary>
    [HttpGet("system-prompt/{role}")]
    public async Task<ActionResult<string>> GetSystemPrompt(string role)
    {
        var prompt = await _promptTemplateService.GetSystemPromptByRoleAsync(role);
        if (prompt == null)
            return NotFound();
        return Ok(prompt);
    }

    /// <summary>
    /// 获取角色提示词配置
    /// </summary>
    [HttpGet("config/{roleId:guid}")]
    public async Task<ActionResult<RolePromptConfigDto>> GetRoleConfig(Guid roleId)
    {
        var config = await _promptTemplateService.GetRolePromptConfigAsync(roleId);
        if (config == null)
            return NotFound();
        return Ok(config);
    }

    /// <summary>
    /// 更新角色提示词配置
    /// </summary>
    [HttpPut("config/{roleId:guid}")]
    public async Task<IActionResult> UpdateRoleConfig(Guid roleId, UpdateRolePromptConfigRequest request)
    {
        await _promptTemplateService.UpdateRolePromptConfigAsync(roleId, request);
        return NoContent();
    }

    /// <summary>
    /// 渲染角色提示词
    /// </summary>
    [HttpPost("render/{roleId:guid}")]
    public async Task<ActionResult<RenderedPromptDto>> RenderRolePrompt(
        Guid roleId,
        [FromBody] Dictionary<string, string>? variables = null)
    {
        var result = await _promptTemplateService.RenderRolePromptAsync(roleId, variables);
        return Ok(result);
    }
}
