using FlowWorker.Core.DTOs;
using FlowWorker.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FlowWorker.Api.Controllers.v1;

/// <summary>
/// API 配置管理 API
/// </summary>
[ApiController]
[Route("api/v1/api-configs")]
public class ApiConfigsController : ControllerBase
{
    private readonly IApiConfigService _apiConfigService;

    public ApiConfigsController(IApiConfigService apiConfigService)
    {
        _apiConfigService = apiConfigService;
    }

    /// <summary>
    /// 获取配置列表
    /// </summary>
    /// <returns>配置列表</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ApiConfigListItemDto>>> GetConfigs()
    {
        var configs = await _apiConfigService.GetConfigsAsync();
        return Ok(configs);
    }

    /// <summary>
    /// 获取配置详情
    /// </summary>
    /// <param name="id">配置 ID</param>
    /// <returns>配置详情</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiConfigDetailDto>> GetConfig(Guid id)
    {
        var config = await _apiConfigService.GetConfigDetailAsync(id);
        if (config == null)
        {
            return NotFound();
        }
        return Ok(config);
    }

    /// <summary>
    /// 获取默认配置
    /// </summary>
    /// <returns>默认配置或 null</returns>
    [HttpGet("default")]
    public async Task<ActionResult<ApiConfigDetailDto>> GetDefaultConfig()
    {
        var config = await _apiConfigService.GetDefaultConfigAsync();
        if (config == null)
        {
            return NotFound();
        }
        return Ok(config);
    }

    /// <summary>
    /// 创建配置
    /// </summary>
    /// <param name="request">创建请求</param>
    /// <returns>配置 ID</returns>
    [HttpPost]
    public async Task<ActionResult<Guid>> CreateConfig(CreateApiConfigRequest request)
    {
        var id = await _apiConfigService.CreateConfigAsync(request);
        return CreatedAtAction(nameof(GetConfig), new { id = id }, id);
    }

    /// <summary>
    /// 更新配置
    /// </summary>
    /// <param name="id">配置 ID</param>
    /// <param name="request">更新请求</param>
    /// <returns></returns>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateConfig(Guid id, UpdateApiConfigRequest request)
    {
        await _apiConfigService.UpdateConfigAsync(id, request);
        return NoContent();
    }

    /// <summary>
    /// 删除配置
    /// </summary>
    /// <param name="id">配置 ID</param>
    /// <returns></returns>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteConfig(Guid id)
    {
        await _apiConfigService.DeleteConfigAsync(id);
        return NoContent();
    }

    /// <summary>
    /// 设置默认配置
    /// </summary>
    /// <param name="id">配置 ID</param>
    /// <returns></returns>
    [HttpPost("{id}/set-default")]
    public async Task<IActionResult> SetDefaultConfig(Guid id)
    {
        await _apiConfigService.SetDefaultConfigAsync(id);
        return NoContent();
    }
}