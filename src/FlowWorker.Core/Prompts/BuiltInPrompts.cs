using System.Text.Json;
using FlowWorker.Shared.Entities;

namespace FlowWorker.Core.Prompts;

/// <summary>
/// 内置提示词模板
/// 所有提示词均从 prompts 文件夹读取
/// </summary>
public static class BuiltInPrompts
{
    /// <summary>
    /// 获取角色的系统提示词（从 prompts 文件夹读取）
    /// </summary>
    /// <param name="roleName">角色名称</param>
    /// <param name="language">语言代码，默认中文</param>
    public static string? GetSystemPrompt(string roleName, string language = "zh")
    {
        return PromptFileLoader.LoadSystemPrompt(roleName, language);
    }

    /// <summary>
    /// 异步获取角色的系统提示词
    /// </summary>
    public static async Task<string?> GetSystemPromptAsync(string roleName, string language = "zh")
    {
        return await PromptFileLoader.LoadSystemPromptAsync(roleName, language);
    }

    /// <summary>
    /// 获取所有可用的角色列表
    /// </summary>
    public static IReadOnlyList<string> GetAvailableRoles()
    {
        return PromptFileLoader.GetAvailableRoles();
    }

    /// <summary>
    /// 检查指定角色的提示词是否存在
    /// </summary>
    public static bool IsPromptAvailable(string roleName, string language = "zh")
    {
        return PromptFileLoader.IsPromptAvailable(roleName, language);
    }

    /// <summary>
    /// 从文件读取所有提示词模板
    /// </summary>
    public static async Task<List<PromptTemplate>> GetAllPromptsAsync()
    {
        var templates = new List<PromptTemplate>();
        var roles = GetAvailableRoles();
        var now = DateTime.UtcNow;

        foreach (var role in roles)
        {
            // 读取中文系统提示词
            var zhContent = await PromptFileLoader.LoadSystemPromptAsync(role, "zh");
            if (!string.IsNullOrEmpty(zhContent))
            {
                templates.Add(new PromptTemplate
                {
                    Id = Guid.NewGuid(),
                    Name = "system_zh",
                    Role = role,
                    TemplateType = "system",
                    Content = zhContent,
                    Variables = JsonSerializer.Serialize(new List<string> { "workspace", "mode", "customInstructions" }),
                    IsBuiltIn = false,
                    Description = $"{role} 角色的系统提示词模板 (zh)",
                    Version = 1,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }

            // 读取英文系统提示词
            var enContent = await PromptFileLoader.LoadSystemPromptAsync(role, "en");
            if (!string.IsNullOrEmpty(enContent))
            {
                templates.Add(new PromptTemplate
                {
                    Id = Guid.NewGuid(),
                    Name = "system_en",
                    Role = role,
                    TemplateType = "system",
                    Content = enContent,
                    Variables = JsonSerializer.Serialize(new List<string> { "workspace", "mode", "customInstructions" }),
                    IsBuiltIn = false,
                    Description = $"{role} role system prompt template (en)",
                    Version = 1,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
        }

        return templates;
    }
}