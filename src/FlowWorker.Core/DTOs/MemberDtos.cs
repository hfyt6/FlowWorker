using FlowWorker.Shared.Enums;

namespace FlowWorker.Core.DTOs;

/// <summary>
/// 成员列表项 DTO
/// </summary>
public class MemberListItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public MemberType Type { get; set; }
    public string? Avatar { get; set; }
    public MemberStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }

    // AI类型特有字段
    public Guid? RoleId { get; set; }
    public string? RoleName { get; set; }
    public string? RoleDisplayName { get; set; }
    public string? ApiConfigName { get; set; }
    public string? Model { get; set; }
}

/// <summary>
/// 成员详情 DTO
/// </summary>
public class MemberDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public MemberType Type { get; set; }
    public string? Avatar { get; set; }
    public MemberStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // AI类型特有字段
    public Guid? RoleId { get; set; }
    public RoleDetailDto? Role { get; set; }
    public Guid? ApiConfigId { get; set; }
    public string? ApiConfigName { get; set; }
    public string? Model { get; set; }
}

/// <summary>
/// 创建AI成员请求
/// </summary>
public class CreateAIMemberRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public Guid RoleId { get; set; }
    public Guid ApiConfigId { get; set; }
    public string? Model { get; set; }
}

/// <summary>
/// 更新成员请求
/// </summary>
public class UpdateMemberRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public MemberStatus Status { get; set; }

    // AI类型可更新字段
    public Guid? RoleId { get; set; }
    public Guid? ApiConfigId { get; set; }
    public string? Model { get; set; }
}
