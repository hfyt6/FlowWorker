using System.Text.Json;
using System.Text.RegularExpressions;
using FlowWorker.Core.DTOs;
using FlowWorker.Core.Interfaces;
using FlowWorker.Core.Prompts;
using FlowWorker.Core.Repositories;
using FlowWorker.Shared.Entities;

namespace FlowWorker.Core.Services;

/// <summary>
/// 提示词模板服务实现
/// </summary>
public class PromptTemplateService : IPromptTemplateService
{
    private readonly IPromptTemplateRepository _promptTemplateRepository;
    private readonly IRoleRepository _roleRepository;

    public PromptTemplateService(
        IPromptTemplateRepository promptTemplateRepository,
        IRoleRepository roleRepository)
    {
        _promptTemplateRepository = promptTemplateRepository;
        _roleRepository = roleRepository;
    }

    public async Task<IReadOnlyList<PromptTemplateListItemDto>> GetAllTemplatesAsync()
    {
        var templates = await _promptTemplateRepository.GetAllAsync();
        return templates.Select(MapToListItemDto).ToList();
    }

    public async Task<IReadOnlyList<PromptTemplateListItemDto>> GetTemplatesByRoleAsync(string role)
    {
        var templates = await _promptTemplateRepository.GetByRoleAsync(role);
        return templates.Select(MapToListItemDto).ToList();
    }

    public async Task<PromptTemplateDetailDto?> GetTemplateByIdAsync(Guid id)
    {
        var template = await _promptTemplateRepository.GetByIdAsync(id);
        if (template == null) return null;

        return MapToDetailDto(template);
    }

    public async Task<Guid> CreateTemplateAsync(CreatePromptTemplateRequest request)
    {
        // 检查是否已存在
        if (await _promptTemplateRepository.ExistsAsync(t =>
            t.Role == request.Role && t.Name == request.Name))
        {
            throw new InvalidOperationException($"Template '{request.Name}' for role '{request.Role}' already exists");
        }

        var template = new PromptTemplate
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Role = request.Role,
            TemplateType = request.TemplateType,
            Content = request.Content,
            Variables = SerializeVariables(request.Variables),
            IsBuiltIn = false,
            Description = request.Description,
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _promptTemplateRepository.AddAsync(template);
        return template.Id;
    }

    public async Task UpdateTemplateAsync(Guid id, UpdatePromptTemplateRequest request)
    {
        var template = await _promptTemplateRepository.GetByIdAsync(id);
        if (template == null)
            throw new InvalidOperationException($"Template with ID {id} not found");

        if (template.IsBuiltIn)
            throw new InvalidOperationException("Built-in templates cannot be modified");

        template.Name = request.Name;
        template.Content = request.Content;
        template.Variables = SerializeVariables(request.Variables);
        template.Description = request.Description;
        template.Version++;
        template.UpdatedAt = DateTime.UtcNow;

        await _promptTemplateRepository.UpdateAsync(template);
    }

    public async Task DeleteTemplateAsync(Guid id)
    {
        var template = await _promptTemplateRepository.GetByIdAsync(id);
        if (template == null)
            throw new InvalidOperationException($"Template with ID {id} not found");

        if (template.IsBuiltIn)
            throw new InvalidOperationException("Built-in templates cannot be deleted");

        await _promptTemplateRepository.DeleteAsync(template);
    }

    public async Task<string?> GetSystemPromptByRoleAsync(string role)
    {
        var template = await _promptTemplateRepository.GetSystemTemplateByRoleAsync(role);
        return template?.Content ?? BuiltInPrompts.GetSystemPrompt(role);
    }

    public Task<string> RenderTemplateAsync(string template, Dictionary<string, string> variables)
    {
        var result = template;
        foreach (var kvp in variables)
        {
            result = result.Replace($"{{{kvp.Key}}}", kvp.Value);
        }
        return Task.FromResult(result);
    }

    public async Task<PreviewPromptTemplateResponse> PreviewTemplateAsync(PreviewPromptTemplateRequest request)
    {
        var detectedVariables = ExtractVariables(request.Template);
        var renderedContent = await RenderTemplateAsync(request.Template, request.Variables);

        return new PreviewPromptTemplateResponse
        {
            RenderedContent = renderedContent,
            DetectedVariables = detectedVariables
        };
    }

    public List<string> ExtractVariables(string template)
    {
        var variables = new List<string>();
        var matches = Regex.Matches(template, @"\{(\w+)\}");
        foreach (Match match in matches)
        {
            var varName = match.Groups[1].Value;
            if (!variables.Contains(varName))
            {
                variables.Add(varName);
            }
        }
        return variables;
    }

    public async Task InitializeBuiltInTemplatesAsync()
    {
        var roles = BuiltInPrompts.GetAvailableRoles();
        var now = DateTime.UtcNow;

        // 为每个角色创建系统提示词模板
        foreach (var role in roles)
        {
            var zhContent = BuiltInPrompts.GetSystemPrompt(role, "zh");
            if (!string.IsNullOrEmpty(zhContent))
            {
                var template = new PromptTemplate
                {
                    Id = Guid.NewGuid(),
                    Name = "system",
                    Role = role,
                    TemplateType = "system",
                    Content = zhContent,
                    Variables = SerializeVariables(new List<string> { "workspace", "mode", "customInstructions" }),
                    IsBuiltIn = true,
                    Description = $"{role}角色的系统提示词",
                    Version = 1,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                if (!await _promptTemplateRepository.ExistsAsync(t =>
                    t.Role == template.Role && t.Name == template.Name && t.TemplateType == template.TemplateType))
                {
                    await _promptTemplateRepository.AddAsync(template);
                }
            }
        }
    }

    public async Task ResetToBuiltInAsync(string role)
    {
        // 删除该角色的自定义模板
        var templates = await _promptTemplateRepository.GetByRoleAsync(role);
        foreach (var template in templates.Where(t => !t.IsBuiltIn))
        {
            await _promptTemplateRepository.DeleteAsync(template);
        }

        // 重新初始化该角色的内置模板
        await InitializeBuiltInTemplatesAsync();
    }

    public async Task<RolePromptConfigDto?> GetRolePromptConfigAsync(Guid roleId)
    {
        // 这里需要从 RolePromptConfig 表中获取，暂时返回默认配置
        var role = await _roleRepository.GetByIdAsync(roleId);
        if (role == null) return null;

        return new RolePromptConfigDto
        {
            Id = Guid.NewGuid(),
            RoleId = roleId,
            CurrentMode = "code",
            CustomInstructions = null,
            MaxIterations = 10,
            TokenBudget = 4000,
            ToolWhitelist = DeserializeTools(role.AllowedTools),
            ToolBlacklist = null,
            EnableChainOfThought = true,
            EnableTaskProgress = true,
            EnableSelfReflection = false,
            AdvancedTechniques = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public Task UpdateRolePromptConfigAsync(Guid roleId, UpdateRolePromptConfigRequest request)
    {
        // 实现配置更新逻辑
        return Task.CompletedTask;
    }

    public async Task<RenderedPromptDto> RenderRolePromptAsync(Guid roleId, Dictionary<string, string>? variables = null)
    {
        var role = await _roleRepository.GetByIdAsync(roleId);
        if (role == null)
            throw new InvalidOperationException($"Role with ID {roleId} not found");

        var config = await GetRolePromptConfigAsync(roleId);
        var systemPrompt = await GetSystemPromptByRoleAsync(role.Name);

        // 合并默认变量
        var mergedVariables = new Dictionary<string, string>
        {
            { "workspace", variables?.GetValueOrDefault("workspace", "") ?? "" },
            { "mode", config?.CurrentMode ?? "code" },
            { "os", variables?.GetValueOrDefault("os", "Windows") ?? "Windows" },
            { "shell", variables?.GetValueOrDefault("shell", "cmd") ?? "cmd" },
            { "customInstructions", config?.CustomInstructions ?? "" }
        };

        // 合并用户提供的变量
        if (variables != null)
        {
            foreach (var kvp in variables)
            {
                if (!mergedVariables.ContainsKey(kvp.Key))
                {
                    mergedVariables[kvp.Key] = kvp.Value;
                }
            }
        }

        // 渲染角色系统提示词
        var renderedPrompt = await RenderTemplateAsync(systemPrompt ?? "", mergedVariables);

        // 根据角色允许的工具列表动态生成工具提示词
        var allowedTools = DeserializeTools(role.AllowedTools);
        var toolPrompt = ToolPromptBuilder.BuildToolPrompt(allowedTools);

        // 组合完整的系统提示词：角色提示词 + 工具提示词
        var fullPrompt = renderedPrompt;
        if (!string.IsNullOrEmpty(toolPrompt))
        {
            fullPrompt = renderedPrompt + "\n" + toolPrompt;
        }

        return new RenderedPromptDto
        {
            SystemPrompt = fullPrompt,
            CustomInstructions = config?.CustomInstructions,
            Metadata = new Dictionary<string, object>
            {
                { "role", role.Name },
                { "mode", config?.CurrentMode ?? "code" },
                { "maxIterations", config?.MaxIterations ?? 10 },
                { "tokenBudget", config?.TokenBudget ?? 4000 },
                { "enableChainOfThought", config?.EnableChainOfThought ?? true },
                { "enableTaskProgress", config?.EnableTaskProgress ?? true },
                { "allowedTools", allowedTools ?? new List<string>() }
            }
        };
    }

    private static PromptTemplateListItemDto MapToListItemDto(PromptTemplate template)
    {
        return new PromptTemplateListItemDto
        {
            Id = template.Id,
            Name = template.Name,
            Role = template.Role,
            TemplateType = template.TemplateType,
            Description = template.Description,
            IsBuiltIn = template.IsBuiltIn,
            Version = template.Version,
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt
        };
    }

    private static PromptTemplateDetailDto MapToDetailDto(PromptTemplate template)
    {
        return new PromptTemplateDetailDto
        {
            Id = template.Id,
            Name = template.Name,
            Role = template.Role,
            TemplateType = template.TemplateType,
            Content = template.Content,
            Variables = DeserializeVariables(template.Variables),
            IsBuiltIn = template.IsBuiltIn,
            Description = template.Description,
            Version = template.Version,
            ParentId = template.ParentId,
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt
        };
    }

    private static string? SerializeVariables(List<string>? variables)
    {
        if (variables == null || variables.Count == 0)
            return null;
        return JsonSerializer.Serialize(variables);
    }

    private static List<string>? DeserializeVariables(string? json)
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

    private static List<string>? DeserializeTools(string? json)
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
