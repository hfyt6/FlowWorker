using System.Text.Json;
using FlowWorker.Core.Interfaces;
using FlowWorker.Core.Services;
using FlowWorker.Core.Tools;

namespace FlowWorker.Tests.Core;

/// <summary>
/// 内置工具处理器测试类
/// </summary>
public class BuiltInToolsTests
{
    #region CalculatorTool Tests

    [Fact]
    public async Task CalculatorTool_Calculate_WithValidExpression_ReturnsSuccess()
    {
        // Arrange
        var tool = new CalculatorTool();
        var parameters = JsonDocument.Parse(@"{""expression"": ""2 + 3 * 4""}").RootElement;

        // Act
        var result = await tool.ExecuteAsync("calculate", parameters);

        // Assert
        Assert.Equal("success", result.Status);
        Assert.NotNull(result.Data);
    }

    [Fact]
    public async Task CalculatorTool_Calculate_WithEmptyExpression_ReturnsError()
    {
        // Arrange
        var tool = new CalculatorTool();
        var parameters = JsonDocument.Parse(@"{""expression"": """"}").RootElement;

        // Act
        var result = await tool.ExecuteAsync("calculate", parameters);

        // Assert
        Assert.Equal("error", result.Status);
        Assert.NotNull(result.ErrorInfo);
        Assert.Equal("INVALID_PARAMETERS", result.ErrorInfo!.Code);
    }

    [Fact]
    public async Task CalculatorTool_GenerateUuid_WithValidCount_ReturnsSuccess()
    {
        // Arrange
        var tool = new CalculatorTool();
        var parameters = JsonDocument.Parse(@"{""count"": 3}").RootElement;

        // Act
        var result = await tool.ExecuteAsync("generate_uuid", parameters);

        // Assert
        Assert.Equal("success", result.Status);
        Assert.NotNull(result.Data);
    }

    [Fact]
    public async Task CalculatorTool_GetTimestamp_WithDefaultFormat_ReturnsSuccess()
    {
        // Arrange
        var tool = new CalculatorTool();
        var parameters = JsonDocument.Parse(@"{}").RootElement;

        // Act
        var result = await tool.ExecuteAsync("get_timestamp", parameters);

        // Assert
        Assert.Equal("success", result.Status);
    }

    [Fact]
    public async Task CalculatorTool_ConvertUnit_Length_ReturnsSuccess()
    {
        // Arrange
        var tool = new CalculatorTool();
        var parameters = JsonDocument.Parse(@"{""value"": 1, ""from_unit"": ""km"", ""to_unit"": ""m""}").RootElement;

        // Act
        var result = await tool.ExecuteAsync("convert_unit", parameters);

        // Assert
        Assert.Equal("success", result.Status);
    }

    #endregion

    #region TextTool Tests

    [Fact]
    public async Task TextTool_SearchText_WithValidPattern_ReturnsSuccess()
    {
        // Arrange
        var tool = new TextTool();
        var parameters = JsonDocument.Parse(@"{""text"": ""Hello World"", ""pattern"": ""Hello""}").RootElement;

        // Act
        var result = await tool.ExecuteAsync("search_text", parameters);

        // Assert
        Assert.Equal("success", result.Status);
    }

    [Fact]
    public async Task TextTool_ReplaceText_WithValidParameters_ReturnsSuccess()
    {
        // Arrange
        var tool = new TextTool();
        var parameters = JsonDocument.Parse(@"{""text"": ""Hello World"", ""pattern"": ""World"", ""replacement"": ""FlowWorker""}").RootElement;

        // Act
        var result = await tool.ExecuteAsync("replace_text", parameters);

        // Assert
        Assert.Equal("success", result.Status);
    }

    [Fact]
    public async Task TextTool_CountLines_WithValidText_ReturnsSuccess()
    {
        // Arrange
        var tool = new TextTool();
        var parameters = JsonDocument.Parse(@"{""text"": ""Line 1\nLine 2\nLine 3""}").RootElement;

        // Act
        var result = await tool.ExecuteAsync("count_lines", parameters);

        // Assert
        Assert.Equal("success", result.Status);
    }

    [Fact]
    public async Task TextTool_ValidateJson_WithValidJson_ReturnsValid()
    {
        // Arrange
        var tool = new TextTool();
        var parameters = JsonDocument.Parse(@"{""json_string"": ""{\""name\"": \""test\""}""}").RootElement;

        // Act
        var result = await tool.ExecuteAsync("validate_json", parameters);

        // Assert
        Assert.Equal("success", result.Status);
    }

    [Fact]
    public async Task TextTool_ValidateJson_WithInvalidJson_ReturnsInvalid()
    {
        // Arrange
        var tool = new TextTool();
        var parameters = JsonDocument.Parse(@"{""json_string"": ""{invalid json}""}").RootElement;

        // Act
        var result = await tool.ExecuteAsync("validate_json", parameters);

        // Assert
        Assert.Equal("success", result.Status);
    }

    #endregion

    #region ToolRegistry Tests

    [Fact]
    public void ToolRegistry_Register_AddsTool()
    {
        // Arrange
        var registry = new ToolRegistry();
        var tool = new CalculatorTool();

        // Act
        registry.Register(tool);

        // Assert
        Assert.True(registry.HasTool("Calculator"));
        Assert.Equal(1, registry.Count);
    }

    [Fact]
    public void ToolRegistry_GetTool_RegisteredTool_ReturnsTool()
    {
        // Arrange
        var registry = new ToolRegistry();
        var tool = new CalculatorTool();
        registry.Register(tool);

        // Act
        var retrievedTool = registry.GetTool("Calculator");

        // Assert
        Assert.NotNull(retrievedTool);
        Assert.IsType<CalculatorTool>(retrievedTool);
    }

    [Fact]
    public void ToolRegistry_GetTool_UnregisteredTool_ReturnsNull()
    {
        // Arrange
        var registry = new ToolRegistry();

        // Act
        var retrievedTool = registry.GetTool("NonExistent");

        // Assert
        Assert.Null(retrievedTool);
    }

    [Fact]
    public void ToolRegistry_GetAllTools_ReturnsAllRegisteredTools()
    {
        // Arrange
        var registry = new ToolRegistry();
        registry.Register(new CalculatorTool());
        registry.Register(new TextTool());

        // Act
        var tools = registry.GetAllTools().ToList();

        // Assert
        Assert.Equal(2, tools.Count);
        Assert.Contains(tools, tool => tool.Name == "Calculator");
        Assert.Contains(tools, tool => tool.Name == "Text");
    }

    [Fact]
    public void ToolRegistry_Unregister_ExistingTool_ReturnsTrue()
    {
        // Arrange
        var registry = new ToolRegistry();
        var tool = new CalculatorTool();
        registry.Register(tool);

        // Act
        var result = registry.Unregister("Calculator");

        // Assert
        Assert.True(result);
        Assert.False(registry.HasTool("Calculator"));
    }

    [Fact]
    public void ToolRegistry_Clear_RemovesAllTools()
    {
        // Arrange
        var registry = new ToolRegistry();
        registry.Register(new CalculatorTool());
        registry.Register(new TextTool());

        // Act
        registry.Clear();

        // Assert
        Assert.Equal(0, registry.Count);
    }

    #endregion

    #region ToolExecutor Tests

    [Fact]
    public async Task ToolExecutor_ExecuteAsync_RegisteredTool_ReturnsSuccess()
    {
        // Arrange
        var registry = new ToolRegistry();
        registry.Register(new CalculatorTool());
        var executor = new ToolExecutor(registry);
        var parameters = JsonDocument.Parse(@"{""expression"": ""2 + 2""}").RootElement;

        // Act
        var result = await executor.ExecuteAsync("Calculator", "calculate", parameters);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Response);
        Assert.Equal("success", result.Response.Status);
    }

    [Fact]
    public async Task ToolExecutor_ExecuteAsync_UnregisteredTool_ReturnsError()
    {
        // Arrange
        var registry = new ToolRegistry();
        var executor = new ToolExecutor(registry);
        var parameters = JsonDocument.Parse(@"{}").RootElement;

        // Act
        var result = await executor.ExecuteAsync("NonExistent", "action", parameters);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("不存在", result.Error);
    }

    [Fact]
    public async Task ToolExecutor_HasTool_RegisteredTool_ReturnsTrue()
    {
        // Arrange
        var registry = new ToolRegistry();
        registry.Register(new CalculatorTool());
        var executor = new ToolExecutor(registry);

        // Act
        var result = executor.HasTool("Calculator");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ToolExecutor_GetAllTools_ReturnsAllTools()
    {
        // Arrange
        var registry = new ToolRegistry();
        registry.Register(new CalculatorTool());
        registry.Register(new TextTool());
        var executor = new ToolExecutor(registry);

        // Act
        var tools = executor.GetAllTools().ToList();

        // Assert
        Assert.Equal(2, tools.Count);
    }

    #endregion

    #region CodeAnalysisTool Tests

    [Fact]
    public async Task CodeAnalysisTool_ParseCode_WithValidCode_ReturnsSuccess()
    {
        // Arrange
        var tool = new CodeAnalysisTool();
        var code = "class Test { void Method() { } }";
        var parameters = new
        {
            code = code,
            language = "csharp"
        };
        var json = System.Text.Json.JsonSerializer.Serialize(parameters);
        var parametersElement = JsonDocument.Parse(json).RootElement;

        // Act
        var result = await tool.ExecuteAsync("parse_code", parametersElement);

        // Assert
        Assert.Equal("success", result.Status);
    }

    #endregion

    #region CodeManipulationTool Tests

    [Fact]
    public async Task CodeManipulationTool_ApplyEdit_WithValidEdit_ReturnsSuccess()
    {
        // Arrange
        var tool = new CodeManipulationTool();
        var code = "public void OldMethod() { }";
        var parameters = JsonDocument.Parse($@"{{""code"": ""{code}"", ""search"": ""OldMethod"", ""replace"": ""NewMethod""}}").RootElement;

        // Act
        var result = await tool.ExecuteAsync("apply_edit", parameters);

        // Assert
        Assert.Equal("success", result.Status);
    }

    [Fact]
    public async Task CodeManipulationTool_FormatCode_WithValidCode_ReturnsSuccess()
    {
        // Arrange
        var tool = new CodeManipulationTool();
        var code = "public void Test(){var x=1;}";
        var parameters = JsonDocument.Parse($@"{{""code"": ""{code}"", ""language"": ""csharp""}}").RootElement;

        // Act
        var result = await tool.ExecuteAsync("format_code", parameters);

        // Assert
        Assert.Equal("success", result.Status);
    }

    #endregion
}