using System.Text.Json;
using FlowWorker.Core.DTOs;
using FlowWorker.Core.Interfaces;
using FlowWorker.Core.Repositories;
using FlowWorker.Shared.Entities;
using FlowWorker.Shared.Enums;

namespace FlowWorker.Core.Services;

/// <summary>
/// 成员服务实现
/// </summary>
public class MemberService : IMemberService
{
    private readonly IMemberRepository _memberRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IApiConfigRepository _apiConfigRepository;

    public MemberService(
        IMemberRepository memberRepository,
        IRoleRepository roleRepository,
        IApiConfigRepository apiConfigRepository)
    {
        _memberRepository = memberRepository;
        _roleRepository = roleRepository;
        _apiConfigRepository = apiConfigRepository;
    }

    public async Task<IReadOnlyList<MemberListItemDto>> GetAllMembersAsync()
    {
        var members = await _memberRepository.GetAllAsync();
        return members.Select(MapToListItemDto).ToList();
    }

    public async Task<IReadOnlyList<MemberListItemDto>> GetMembersByTypeAsync(MemberType type)
    {
        var members = await _memberRepository.GetByTypeAsync(type);
        return members.Select(MapToListItemDto).ToList();
    }

    public async Task<MemberDetailDto?> GetMemberByIdAsync(Guid id)
    {
        var member = await _memberRepository.GetByIdAsync(id);
        if (member == null) return null;

        return MapToDetailDto(member);
    }

    public async Task<Guid> CreateAIMemberAsync(CreateAIMemberRequest request)
    {
        // 验证角色存在
        var role = await _roleRepository.GetByIdAsync(request.RoleId);
        if (role == null)
            throw new InvalidOperationException($"Role with ID {request.RoleId} not found");

        // 获取API配置，用于获取默认模型
        ApiConfig? apiConfig = null;
        if (request.ApiConfigId != Guid.Empty)
        {
            apiConfig = await _apiConfigRepository.GetByIdAsync(request.ApiConfigId);
        }

        // 如果未指定模型，使用API配置的默认模型
        var model = request.Model;
        if (string.IsNullOrEmpty(model) && apiConfig != null)
        {
            model = apiConfig.Model;
        }

        var member = new Member
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Type = MemberType.AI,
            Avatar = request.Avatar,
            Status = MemberStatus.Online,
            RoleId = request.RoleId,
            ApiConfigId = request.ApiConfigId,
            Model = model,
            Temperature = request.Temperature,
            MaxToken = request.MaxToken,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _memberRepository.AddAsync(member);
        return member.Id;
    }

    public async Task<Guid> CreateUserMemberAsync(string name, string? avatar = null)
    {
        var member = new Member
        {
            Id = Guid.NewGuid(),
            Name = name,
            Type = MemberType.User,
            Avatar = avatar,
            Status = MemberStatus.Online,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _memberRepository.AddAsync(member);
        return member.Id;
    }

    public async Task UpdateMemberAsync(Guid id, UpdateMemberRequest request)
    {
        var member = await _memberRepository.GetByIdAsync(id);
        if (member == null)
            throw new InvalidOperationException($"Member with ID {id} not found");

        // 用户类型只能更新基本信息
        if (member.Type == MemberType.User)
        {
            member.Name = request.Name;
            member.Avatar = request.Avatar;
            member.Status = request.Status;
        }
        else // AI类型可以更新更多字段
        {
            member.Name = request.Name;
            member.Avatar = request.Avatar;
            member.Status = request.Status;

            if (request.RoleId.HasValue)
            {
                var role = await _roleRepository.GetByIdAsync(request.RoleId.Value);
                if (role == null)
                    throw new InvalidOperationException($"Role with ID {request.RoleId} not found");
                member.RoleId = request.RoleId.Value;
            }

            if (request.ApiConfigId.HasValue)
                member.ApiConfigId = request.ApiConfigId.Value;

            member.Model = request.Model;
            
            if (request.Temperature.HasValue)
                member.Temperature = request.Temperature.Value;
            
            if (request.MaxToken.HasValue)
                member.MaxToken = request.MaxToken.Value;
        }

        member.UpdatedAt = DateTime.UtcNow;
        await _memberRepository.UpdateAsync(member);
    }

    public async Task DeleteAIMemberAsync(Guid id)
    {
        var member = await _memberRepository.GetByIdAsync(id);
        if (member == null)
            throw new InvalidOperationException($"Member with ID {id} not found");

        if (member.Type != MemberType.AI)
            throw new InvalidOperationException("Only AI members can be deleted");

        await _memberRepository.DeleteAsync(member);
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _memberRepository.ExistsAsync(p => p.Id == id);
    }

    private static MemberListItemDto MapToListItemDto(Member member)
    {
        return new MemberListItemDto
        {
            Id = member.Id,
            Name = member.Name,
            Type = member.Type,
            Avatar = member.Avatar,
            Status = member.Status,
            CreatedAt = member.CreatedAt,
            RoleId = member.RoleId,
            RoleName = member.Role?.Name,
            RoleDisplayName = member.Role?.DisplayName,
            ApiConfigName = member.ApiConfig?.Name,
            Model = member.Model,
            Temperature = member.Temperature,
            MaxToken = member.MaxToken
        };
    }

    private static MemberDetailDto MapToDetailDto(Member member)
    {
        return new MemberDetailDto
        {
            Id = member.Id,
            Name = member.Name,
            Type = member.Type,
            Avatar = member.Avatar,
            Status = member.Status,
            CreatedAt = member.CreatedAt,
            UpdatedAt = member.UpdatedAt,
            RoleId = member.RoleId,
            Role = member.Role == null ? null : new RoleDetailDto
            {
                Id = member.Role.Id,
                Name = member.Role.Name,
                DisplayName = member.Role.DisplayName,
                Description = member.Role.Description,
                SystemPrompt = member.Role.SystemPrompt,
                AllowedTools = DeserializeAllowedTools(member.Role.AllowedTools),
                IsBuiltIn = member.Role.IsBuiltIn,
                CreatedAt = member.Role.CreatedAt,
                UpdatedAt = member.Role.UpdatedAt
            },
            ApiConfigId = member.ApiConfigId,
            ApiConfigName = member.ApiConfig?.Name,
            Model = member.Model,
            Temperature = member.Temperature,
            MaxToken = member.MaxToken
        };
    }

    /// <summary>
    /// 将JSON字符串反序列化为工具列表
    /// </summary>
    private static List<string>? DeserializeAllowedTools(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return null;
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json);
        }
        catch
        {
            return null;
        }
    }
}
