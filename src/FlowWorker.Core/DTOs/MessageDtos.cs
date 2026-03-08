using FlowWorker.Shared.Entities;

namespace FlowWorker.Core.DTOs;

/// <summary>
/// 创建消息请求
/// </summary>
public class CreateMessageRequest
{
    /// <summary>
    /// 角色
    /// </summary>
    public MessageRole Role { get; set; }

    /// <summary>
    /// 消息内容
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 消耗的 Token 数
    /// </summary>
    public int? Tokens { get; set; }

    /// <summary>
    /// 使用的模型
    /// </summary>
    public string? Model { get; set; }
}

/// <summary>
/// 消息列表项
/// </summary>
public class MessageListItemDto
{
    public Guid Id { get; set; }
    public MessageRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
    public int? Tokens { get; set; }
    public string? Model { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// 发送消息响应
/// </summary>
public class SendMessageResponse
{
    /// <summary>
    /// 响应内容
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 使用的模型
    /// </summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// 消耗的 Token 数
    /// </summary>
    public int? Tokens { get; set; }

    /// <summary>
    /// 是否完成
    /// </summary>
    public bool IsComplete { get; set; }
}

/// <summary>
/// 发送消息请求
/// </summary>
public class SendMessageRequest
{
    /// <summary>
    /// 消息内容
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 发送者成员ID（群聊时使用）
    /// </summary>
    public Guid? SenderMemberId { get; set; }
}

/// <summary>
/// 群聊消息列表项
/// </summary>
public class GroupMessageListItemDto : MessageListItemDto
{
    /// <summary>
    /// 发送者成员ID
    /// </summary>
    public Guid? MemberId { get; set; }

    /// <summary>
    /// 发送者名称
    /// </summary>
    public string? MemberName { get; set; }

    /// <summary>
    /// 发送者类型
    /// </summary>
    public string? MemberType { get; set; }

    /// <summary>
    /// 发送者头像
    /// </summary>
    public string? MemberAvatar { get; set; }
}
