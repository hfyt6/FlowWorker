namespace FlowWorker.Shared.DTOs;

/// <summary>
/// 流式响应内容
/// </summary>
public class StreamContentChunk
{
    /// <summary>
    /// 类型（content/tool_call/complete/error/member_start/member_complete/group_complete/user_message）
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

    /// <summary>
    /// 消息ID
    /// </summary>
    public string? MessageId { get; set; }

    /// <summary>
    /// 成员ID
    /// </summary>
    public string? MemberId { get; set; }

    /// <summary>
    /// 成员名称
    /// </summary>
    public string? MemberName { get; set; }

    /// <summary>
    /// 使用的模型
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// 完成原因
    /// </summary>
    public string? FinishReason { get; set; }
}
