using FlowWorker.Core.Prompts;

namespace FlowWorker.Tests.Core;

/// <summary>
/// BuiltInPrompts 测试类
/// </summary>
public class BuiltInPromptsTests
{
    [Fact]
    public async Task GetAllPromptsAsync_ShouldReturnNonEmptyList()
    {
        // Arrange & Act
        var templates = await BuiltInPrompts.GetAllPromptsAsync();

        // Assert
        Assert.NotNull(templates);
        Assert.True(templates.Count > 0);
    }

    [Fact]
    public async Task GetAllPromptsAsync_ShouldNotHaveDuplicateRoleNameTemplateTypeCombination()
    {
        // Arrange & Act
        var templates = await BuiltInPrompts.GetAllPromptsAsync();

        // Assert - 验证 (Role, Name, TemplateType) 组合唯一
        var grouped = templates.GroupBy(t => (t.Role, t.Name, t.TemplateType));
        foreach (var group in grouped)
        {
            Assert.True(group.Count() == 1, 
                $"发现重复的 (Role, Name, TemplateType) 组合: ({group.Key.Role}, {group.Key.Name}, {group.Key.TemplateType})，数量: {group.Count()}");
        }
    }

    [Fact]
    public async Task GetAllPromptsAsync_ShouldHaveBothZhAndEnTemplates()
    {
        // Arrange & Act
        var templates = await BuiltInPrompts.GetAllPromptsAsync();
        var roles = BuiltInPrompts.GetAvailableRoles();

        // Assert - 每个角色应该有中文和英文两个模板
        foreach (var role in roles)
        {
            var zhTemplate = templates.FirstOrDefault(t => t.Role == role && t.Name == "system_zh");
            var enTemplate = templates.FirstOrDefault(t => t.Role == role && t.Name == "system_en");

            Assert.NotNull(zhTemplate);
            Assert.NotNull(enTemplate);
        }
    }

    [Fact]
    public async Task GetAllPromptsAsync_ShouldSetCorrectProperties()
    {
        // Arrange & Act
        var templates = await BuiltInPrompts.GetAllPromptsAsync();

        // Assert
        foreach (var template in templates)
        {
            Assert.NotEqual(Guid.Empty, template.Id);
            Assert.False(string.IsNullOrEmpty(template.Name));
            Assert.False(string.IsNullOrEmpty(template.Role));
            Assert.False(string.IsNullOrEmpty(template.TemplateType));
            Assert.False(string.IsNullOrEmpty(template.Content));
            Assert.True(template.Version >= 1);
        }
    }

    [Fact]
    public void GetAvailableRoles_ShouldReturnExpectedRoles()
    {
        // Arrange & Act
        var roles = BuiltInPrompts.GetAvailableRoles();

        // Assert
        Assert.Contains("coder", roles);
        Assert.Contains("ui-designer", roles);
        Assert.Contains("architect", roles);
        Assert.Contains("reviewer", roles);
        Assert.Contains("general", roles);
    }

    [Theory]
    [InlineData("coder", "zh")]
    [InlineData("coder", "en")]
    [InlineData("ui-designer", "zh")]
    [InlineData("architect", "zh")]
    public void IsPromptAvailable_ShouldReturnTrue_ForValidRoles(string roleName, string language)
    {
        // Arrange & Act
        var isAvailable = BuiltInPrompts.IsPromptAvailable(roleName, language);

        // Assert
        Assert.True(isAvailable);
    }

    [Fact]
    public void IsPromptAvailable_ShouldReturnFalse_ForInvalidRole()
    {
        // Arrange & Act
        var isAvailable = BuiltInPrompts.IsPromptAvailable("non-existent-role", "zh");

        // Assert
        Assert.False(isAvailable);
    }

    [Theory]
    [InlineData("coder")]
    [InlineData("ui-designer")]
    [InlineData("architect")]
    public async Task GetSystemPromptAsync_ShouldReturnContent_ForValidRoles(string roleName)
    {
        // Arrange & Act
        var content = await BuiltInPrompts.GetSystemPromptAsync(roleName, "zh");

        // Assert
        Assert.NotNull(content);
        Assert.False(string.IsNullOrWhiteSpace(content));
    }
}