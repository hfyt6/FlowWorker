using FlowWorker.Core.Services;

namespace FlowWorker.Tests.Core;

/// <summary>
/// ToolCallParser 测试类
/// </summary>
public class ToolCallParserTests
{
    [Fact]
    public void ParseToolCalls_EmptyContent_ReturnsEmptyList()
    {
        // Arrange
        var content = "";

        // Act
        var result = ToolCallParser.ParseToolCalls(content);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void ParseToolCalls_NullContent_ReturnsEmptyList()
    {
        // Arrange
        string? content = null;

        // Act
        var result = ToolCallParser.ParseToolCalls(content!);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void ParseToolCalls_WhitespaceContent_ReturnsEmptyList()
    {
        // Arrange
        var content = "   \n\t  ";

        // Act
        var result = ToolCallParser.ParseToolCalls(content);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void ParseToolCalls_SingleToolCall_ReturnsOneToolCall()
    {
        // Arrange
        var content = "<calculator><expression>1+2</expression></calculator>";

        // Act
        var result = ToolCallParser.ParseToolCalls(content);

        // Assert
        Assert.Single(result);
        Assert.Equal("calculator", result[0].ToolName);
        Assert.Equal("1+2", result[0].GetParameter("expression"));
    }

    [Fact]
    public void ParseToolCalls_MultipleToolCalls_ReturnsMultipleToolCalls()
    {
        // Arrange
        var content = "<calculator><expression>1+2</expression></calculator><filesystem><path>/test</path></filesystem>";

        // Act
        var result = ToolCallParser.ParseToolCalls(content);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("calculator", result[0].ToolName);
        Assert.Equal("filesystem", result[1].ToolName);
    }

    [Fact]
    public void ParseToolCalls_WithCData_ParsesCorrectly()
    {
        // Arrange
        var content = "<code_analysis><code><![CDATA[function test() { return 1; }]]></code></code_analysis>";

        // Act
        var result = ToolCallParser.ParseToolCalls(content);

        // Assert
        Assert.Single(result);
        Assert.Equal("code_analysis", result[0].ToolName);
        Assert.Equal("function test() { return 1; }", result[0].GetParameter("code"));
    }

    [Fact]
    public void ExtractTextContent_RemovesToolCalls()
    {
        // Arrange
        var content = "Hello <calculator><expression>1+2</expression></calculator> World";

        // Act
        var result = ToolCallParser.ExtractTextContent(content);

        // Assert
        Assert.Equal("Hello  World", result);
    }

    [Fact]
    public void ContainsToolCalls_WithToolCall_ReturnsTrue()
    {
        // Arrange
        var content = "Hello <calculator><expression>1+2</expression></calculator>";

        // Act
        var result = ToolCallParser.ContainsToolCalls(content);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ContainsToolCalls_WithoutToolCall_ReturnsFalse()
    {
        // Arrange
        var content = "Hello World";

        // Act
        var result = ToolCallParser.ContainsToolCalls(content);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void TryParseToolCall_ValidToolCall_ReturnsTrue()
    {
        // Arrange
        var content = "<calculator><expression>1+2</expression></calculator>";

        // Act
        var success = ToolCallParser.TryParseToolCall(content, out var toolCall);

        // Assert
        Assert.True(success);
        Assert.NotNull(toolCall);
        Assert.Equal("calculator", toolCall!.ToolName);
    }

    [Fact]
    public void TryParseToolCall_InvalidToolCall_ReturnsFalse()
    {
        // Arrange
        var content = "Hello World";

        // Act
        var success = ToolCallParser.TryParseToolCall(content, out var toolCall);

        // Assert
        Assert.False(success);
        Assert.Null(toolCall);
    }

    [Fact]
    public void ToolCall_GetParameter_WithDefaultValue_ReturnsDefaultWhenNotFound()
    {
        // Arrange
        var toolCall = new ToolCall
        {
            ToolName = "test",
            Parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["existing"] = "value"
            }
        };

        // Act
        var existingValue = toolCall.GetParameter("existing", "default");
        var missingValue = toolCall.GetParameter("missing", "default");

        // Assert
        Assert.Equal("value", existingValue);
        Assert.Equal("default", missingValue);
    }

    [Fact]
    public void ToolCall_GetParameter_CaseInsensitive()
    {
        // Arrange
        var toolCall = new ToolCall
        {
            ToolName = "test",
            Parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["ParamName"] = "value"
            }
        };

        // Act
        var result = toolCall.GetParameter("paramname");

        // Assert
        Assert.Equal("value", result);
    }
}
