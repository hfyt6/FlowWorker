namespace FlowWorker.Core.Configuration;

/// <summary>
/// 群聊配置选项
/// </summary>
public class GroupChatOptions
{
    /// <summary>
    /// 最大调用深度（防止无限循环）
    /// </summary>
    public int MaxCallDepth { get; set; } = 5;

    /// <summary>
    /// 每轮对话初始调用令牌数
    /// </summary>
    public int InitialCallTokens { get; set; } = 3;

    /// <summary>
    /// 单轮对话超时时间（分钟）
    /// </summary>
    public int RoundTimeoutMinutes { get; set; } = 5;

    /// <summary>
    /// 单个AI响应超时时间（分钟）
    /// </summary>
    public int ResponseTimeoutMinutes { get; set; } = 2;
}