using System.Text.Json;
using FlowWorker.Core.DTOs;
using FlowWorker.Core.Interfaces;
using FlowWorker.Core.Repositories;
using FlowWorker.Shared.Entities;

namespace FlowWorker.Core.Services;

/// <summary>
/// 角色服务实现
/// </summary>
public class RoleService : IRoleService
{
    private readonly IRoleRepository _roleRepository;

    public RoleService(IRoleRepository roleRepository)
    {
        _roleRepository = roleRepository;
    }

    public async Task<IReadOnlyList<RoleListItemDto>> GetAllRolesAsync()
    {
        var roles = await _roleRepository.GetAllAsync();
        return roles.Select(MapToListItemDto).ToList();
    }

    public async Task<IReadOnlyList<RoleListItemDto>> GetBuiltInRolesAsync()
    {
        var roles = await _roleRepository.GetBuiltInRolesAsync();
        return roles.Select(MapToListItemDto).ToList();
    }

    public async Task<IReadOnlyList<RoleListItemDto>> GetCustomRolesAsync()
    {
        var roles = await _roleRepository.GetCustomRolesAsync();
        return roles.Select(MapToListItemDto).ToList();
    }

    public async Task<RoleDetailDto?> GetRoleByIdAsync(Guid id)
    {
        var role = await _roleRepository.GetByIdAsync(id);
        if (role == null) return null;

        return MapToDetailDto(role);
    }

    public async Task<RoleDetailDto?> GetRoleByNameAsync(string name)
    {
        var role = await _roleRepository.GetByNameAsync(name);
        if (role == null) return null;

        return MapToDetailDto(role);
    }

    public async Task<Guid> CreateRoleAsync(CreateRoleRequest request)
    {
        // 检查名称是否已存在
        if (await _roleRepository.ExistsAsync(r => r.Name.ToLower() == request.Name.ToLower()))
            throw new InvalidOperationException($"Role with name '{request.Name}' already exists");

        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            DisplayName = request.DisplayName,
            Description = request.Description,
            SystemPrompt = request.SystemPrompt,
            AllowedTools = SerializeAllowedTools(request.AllowedTools),
            IsBuiltIn = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _roleRepository.AddAsync(role);
        return role.Id;
    }

    public async Task UpdateRoleAsync(Guid id, UpdateRoleRequest request)
    {
        var role = await _roleRepository.GetByIdAsync(id);
        if (role == null)
            throw new InvalidOperationException($"Role with ID {id} not found");

        // 内置角色可以编辑，但名称不可修改
        role.DisplayName = request.DisplayName;
        role.Description = request.Description;
        role.SystemPrompt = request.SystemPrompt;
        role.AllowedTools = SerializeAllowedTools(request.AllowedTools);
        role.UpdatedAt = DateTime.UtcNow;

        await _roleRepository.UpdateAsync(role);
    }

    public async Task DeleteRoleAsync(Guid id)
    {
        var role = await _roleRepository.GetByIdAsync(id);
        if (role == null)
            throw new InvalidOperationException($"Role with ID {id} not found");

        if (role.IsBuiltIn)
            throw new InvalidOperationException("Built-in roles cannot be deleted");

        // 检查角色是否被使用
        if (await _roleRepository.IsRoleInUseAsync(id))
            throw new InvalidOperationException("Cannot delete role that is in use by members");

        await _roleRepository.DeleteAsync(role);
    }

    public async Task<string?> GetSystemPromptTemplateAsync(Guid id)
    {
        var role = await _roleRepository.GetByIdAsync(id);
        return role?.SystemPrompt;
    }

    public async Task InitializeBuiltInRolesAsync()
    {
        var builtInRoles = new List<Role>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "assistant",
                DisplayName = "通用助手",
                Description = "通用的AI助手，可以回答各种问题",
                SystemPrompt = "你是一个有帮助的AI助手。请用简洁、准确的方式回答用户的问题。",
                AllowedTools = SerializeAllowedTools(new List<string>()),
                IsBuiltIn = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        foreach (var role in builtInRoles)
        {
            if (!await _roleRepository.ExistsAsync(r => r.Name == role.Name))
            {
                await _roleRepository.AddAsync(role);
            }
        }
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _roleRepository.ExistsAsync(r => r.Id == id);
    }

    private static RoleListItemDto MapToListItemDto(Role role)
    {
        return new RoleListItemDto
        {
            Id = role.Id,
            Name = role.Name,
            DisplayName = role.DisplayName,
            Description = role.Description,
            IsBuiltIn = role.IsBuiltIn,
            CreatedAt = role.CreatedAt
        };
    }

    private static RoleDetailDto MapToDetailDto(Role role)
    {
        return new RoleDetailDto
        {
            Id = role.Id,
            Name = role.Name,
            DisplayName = role.DisplayName,
            Description = role.Description,
            SystemPrompt = role.SystemPrompt,
            AllowedTools = DeserializeAllowedTools(role.AllowedTools),
            IsBuiltIn = role.IsBuiltIn,
            CreatedAt = role.CreatedAt,
            UpdatedAt = role.UpdatedAt
        };
    }

    /// <summary>
    /// 将工具列表序列化为JSON字符串
    /// </summary>
    private static string? SerializeAllowedTools(List<string>? tools)
    {
        if (tools == null || tools.Count == 0)
            return null;
        return JsonSerializer.Serialize(tools);
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
