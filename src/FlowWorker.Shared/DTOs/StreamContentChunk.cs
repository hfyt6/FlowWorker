namespace FlowWorker.Shared.DTOs;

/// <summary>
/// 流式响应内容
/// </summary>
public class StreamContentChunk
{
    /// <summary>
    /// 类型（content/tool_call/complete/error）
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 内容
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? Error { get; set; }
}