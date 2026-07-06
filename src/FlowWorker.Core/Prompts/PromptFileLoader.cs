using System.Reflection;

namespace FlowWorker.Core.Prompts;

/// <summary>
/// 提示词文件加载器
/// 从 prompts 文件夹中读取提示词文件
/// </summary>
public static class PromptFileLoader
{
    private static readonly Dictionary<string, string> _roleNameMapping = new()
    {
        { "coder", "coder" },
        { "ui-designer", "ui-designer" },
        { "architect", "architect" },
        { "reviewer", "reviewer" },
        { "general", "general" },
        { "novelist", "novelist" },
        { "assistant", "assistant" }
    };

    /// <summary>
    /// 获取提示词文件夹的基础路径
    /// </summary>
    public static string GetBasePath()
    {
        // 获取程序集所在目录
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var assemblyDir = Path.GetDirectoryName(assemblyLocation);
        
        // 从程序集目录向上查找，直到找到包含 prompts 文件夹的目录
        var currentDir = assemblyDir;
        while (!string.IsNullOrEmpty(currentDir))
        {
            var promptsPath = Path.Combine(currentDir, "prompts");
            if (Directory.Exists(promptsPath))
            {
                return promptsPath;
            }
            currentDir = Path.GetDirectoryName(currentDir);
        }

        // 如果找不到，使用当前工作目录下的 prompts 文件夹
        return Path.Combine(Directory.GetCurrentDirectory(), "prompts");
    }

    /// <summary>
    /// 读取指定角色和语言的系统提示词
    /// </summary>
    /// <param name="roleName">角色名称</param>
    /// <param name="language">语言代码 (en/zh)</param>
    /// <returns>提示词内容，如果文件不存在则返回 null</returns>
    public static async Task<string?> LoadSystemPromptAsync(string roleName, string language = "zh")
    {
        if (!_roleNameMapping.ContainsKey(roleName.ToLower()))
        {
            return null;
        }

        var normalizedRoleName = roleName.ToLower();
        var normalizedLanguage = language.ToLower();

        // 构建文件路径: prompts/{role}/{language}/system.md
        var basePath = GetBasePath();
        var filePath = Path.Combine(basePath, normalizedRoleName, normalizedLanguage, "system.md");

        if (!File.Exists(filePath))
        {
            // 尝试使用默认语言
            if (normalizedLanguage != "zh")
            {
                var fallbackPath = Path.Combine(basePath, normalizedRoleName, "zh", "system.md");
                if (File.Exists(fallbackPath))
                {
                    return await File.ReadAllTextAsync(fallbackPath);
                }
            }
            return null;
        }

        return await File.ReadAllTextAsync(filePath);
    }

    /// <summary>
    /// 读取指定角色和语言的系统提示词（同步版本）
    /// </summary>
    public static string? LoadSystemPrompt(string roleName, string language = "zh")
    {
        return LoadSystemPromptAsync(roleName, language).GetAwaiter().GetResult();
    }

    /// <summary>
    /// 获取所有可用的角色列表
    /// </summary>
    public static IReadOnlyList<string> GetAvailableRoles()
    {
        var basePath = GetBasePath();
        if (!Directory.Exists(basePath))
        {
            return _roleNameMapping.Values.ToList();
        }

        return Directory.GetDirectories(basePath)
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrEmpty(name))
            .Select(name => name!)
            .ToList();
    }

    /// <summary>
    /// 检查指定角色的提示词是否存在
    /// </summary>
    public static bool IsPromptAvailable(string roleName, string language = "zh")
    {
        var basePath = GetBasePath();
        var filePath = Path.Combine(basePath, roleName.ToLower(), language.ToLower(), "system.md");
        return File.Exists(filePath);
    }

    /// <summary>
    /// 读取模板文件
    /// </summary>
    /// <param name="roleName">角色名称</param>
    /// <param name="templateName">模板名称</param>
    /// <param name="language">语言代码</param>
    /// <returns>模板内容，如果文件不存在则返回 null</returns>
    public static async Task<string?> LoadTemplateAsync(string roleName, string templateName, string language = "zh")
    {
        var basePath = GetBasePath();
        var filePath = Path.Combine(basePath, roleName.ToLower(), language.ToLower(), $"{templateName}.md");

        if (!File.Exists(filePath))
        {
            return null;
        }

        return await File.ReadAllTextAsync(filePath);
    }
}