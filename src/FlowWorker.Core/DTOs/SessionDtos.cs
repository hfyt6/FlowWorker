using FlowWorker.Shared.Entities;

namespace FlowWorker.Core.DTOs;

/// <summary>
/// 创建会话请求
/// </summary>
public class CreateSessionRequest
{
    /// <summary>
    /// 会话标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// API 配置 ID
    /// </summary>
    public Guid ApiConfigId { get; set; }

    /// <summary>
    /// 使用的模型
    /// </summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// 系统提示词
    /// </summary>
    public string SystemPrompt { get; set; } = string.Empty;

    /// <summary>
    /// 生成温度
    /// </summary>
    public decimal Temperature { get; set; } = 0.7m;

    /// <summary>
    /// 最大 Token 数
    /// </summary>
    public int? MaxTokens { get; set; }
}

/// <summary>
/// 更新会话请求
/// </summary>
public class UpdateSessionRequest
{
    /// <summary>
    /// 会话标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// API 配置 ID
    /// </summary>
    public Guid ApiConfigId { get; set; }

    /// <summary>
    /// 使用的模型
    /// </summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// 系统提示词
    /// </summary>
    public string SystemPrompt { get; set; } = string.Empty;

    /// <summary>
    /// 生成温度
    /// </summary>
    public decimal Temperature { get; set; } = 0.7m;

    /// <summary>
    /// 最大 Token 数
    /// </summary>
    public int? MaxTokens { get; set; }
}

/// <summary>
/// 会话列表项
/// </summary>
public class SessionListItemDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int MessageCount { get; set; }
}

/// <summary>
/// 会话详情
/// </summary>
public class SessionDetailDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public Guid ApiConfigId { get; set; }
    public string ApiConfigName { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string SystemPrompt { get; set; } = string.Empty;
    public decimal Temperature { get; set; }
    public int? MaxTokens { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
