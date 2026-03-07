using FlowWorker.Shared.Entities;

namespace FlowWorker.Core.DTOs;

/// <summary>
/// 创建 API 配置请求
/// </summary>
public class CreateApiConfigRequest
{
    /// <summary>
    /// 配置名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// API 基础 URL
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// API 密钥
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// 默认模型
    /// </summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// 是否为默认配置
    /// </summary>
    public bool IsDefault { get; set; }
}

/// <summary>
/// 更新 API 配置请求
/// </summary>
public class UpdateApiConfigRequest
{
    /// <summary>
    /// 配置名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// API 基础 URL
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// API 密钥
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// 默认模型
    /// </summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// 是否为默认配置
    /// </summary>
    public bool IsDefault { get; set; }
}

/// <summary>
/// API 配置列表项
/// </summary>
public class ApiConfigListItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// API 配置详情
/// </summary>
public class ApiConfigDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }
}