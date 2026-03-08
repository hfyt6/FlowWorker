using FlowWorker.Core.DTOs;
using FlowWorker.Shared.Enums;

namespace FlowWorker.Core.Interfaces;

/// <summary>
/// 成员服务接口
/// </summary>
public interface IMemberService
{
    /// <summary>
    /// 获取所有成员列表
    /// </summary>
    Task<IReadOnlyList<MemberListItemDto>> GetAllMembersAsync();

    /// <summary>
    /// 根据类型获取成员列表
    /// </summary>
    Task<IReadOnlyList<MemberListItemDto>> GetMembersByTypeAsync(MemberType type);

    /// <summary>
    /// 获取成员详情
    /// </summary>
    Task<MemberDetailDto?> GetMemberByIdAsync(Guid id);

    /// <summary>
    /// 创建AI成员
    /// </summary>
    Task<Guid> CreateAIMemberAsync(CreateAIMemberRequest request);

    /// <summary>
    /// 创建用户成员
    /// </summary>
    Task<Guid> CreateUserMemberAsync(string name, string? avatar = null);

    /// <summary>
    /// 更新成员信息
    /// </summary>
    Task UpdateMemberAsync(Guid id, UpdateMemberRequest request);

    /// <summary>
    /// 删除AI成员
    /// </summary>
    Task DeleteAIMemberAsync(Guid id);

    /// <summary>
    /// 检查成员是否存在
    /// </summary>
    Task<bool> ExistsAsync(Guid id);
}
