using FlowWorker.Core.Prompts;

namespace FlowWorker.Tests.Core;

/// <summary>
/// PromptFileLoader 测试类
/// </summary>
public class PromptFileLoaderTests
{
    [Fact]
    public void GetBasePath_ShouldReturnPromptsDirectory()
    {
        // Arrange & Act
        var basePath = PromptFileLoader.GetBasePath();

        // Assert
        Assert.NotNull(basePath);
        Assert.EndsWith("prompts", basePath);
    }

    [Theory]
    [InlineData("coder")]
    [InlineData("ui-designer")]
    [InlineData("architect")]
    [InlineData("reviewer")]
    [InlineData("general")]
    [InlineData("novelist")]
    public void IsPromptAvailable_ShouldReturnTrue_ForAvailableRoles(string roleName)
    {
        // Arrange & Act
        var isAvailable = PromptFileLoader.IsPromptAvailable(roleName, "zh");

        // Assert
        Assert.True(isAvailable);
    }

    [Fact]
    public void IsPromptAvailable_ShouldReturnFalse_ForUnknownRole()
    {
        // Arrange & Act
        var isAvailable = PromptFileLoader.IsPromptAvailable("unknown-role", "zh");

        // Assert
        Assert.False(isAvailable);
    }

    [Theory]
    [InlineData("coder")]
    [InlineData("ui-designer")]
    [InlineData("architect")]
    [InlineData("reviewer")]
    [InlineData("general")]
    [InlineData("novelist")]
    public async Task LoadSystemPromptAsync_ShouldReturnContent_ForAvailableRoles(string roleName)
    {
        // Arrange & Act
        var content = await PromptFileLoader.LoadSystemPromptAsync(roleName, "zh");

        // Assert
        Assert.NotNull(content);
        Assert.False(string.IsNullOrWhiteSpace(content));
    }

    [Fact]
    public void GetAvailableRoles_ShouldReturnNonEmptyList()
    {
        // Arrange & Act
        var roles = PromptFileLoader.GetAvailableRoles();

        // Assert
        Assert.NotNull(roles);
        Assert.True(roles.Count > 0);
    }

    [Theory]
    [InlineData("coder")]
    [InlineData("ui-designer")]
    [InlineData("architect")]
    [InlineData("reviewer")]
    [InlineData("general")]
    [InlineData("novelist")]
    public async Task LoadSystemPromptAsync_ShouldReturnContent_ContainingRoleDefinition(string roleName)
    {
        // Arrange & Act
        var content = await PromptFileLoader.LoadSystemPromptAsync(roleName, "zh");

        // Assert
        Assert.NotNull(content);
        Assert.True(content.Contains("角色") || content.Contains("Role"));
    }

    [Fact]
    public void LoadSystemPrompt_ShouldReturnContent_ForEnglishRole()
    {
        // Arrange & Act - Test with a role that has both en and zh
        var content = PromptFileLoader.LoadSystemPrompt("coder", "en");

        // Assert - Should return content (either en or fallback to zh)
        Assert.NotNull(content);
    }

    [Fact]
    public void LoadSystemPrompt_ShouldReturnSameContent_ForSyncAndAsync()
    {
        // Arrange & Act
        var syncContent = PromptFileLoader.LoadSystemPrompt("coder", "zh");
        var asyncContent = PromptFileLoader.LoadSystemPromptAsync("coder", "zh").GetAwaiter().GetResult();

        // Assert
        Assert.Equal(syncContent, asyncContent);
    }
}