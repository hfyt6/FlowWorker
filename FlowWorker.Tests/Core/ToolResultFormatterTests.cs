using System.Text.Json;
using FlowWorker.Core.Services;
using FlowWorker.Core.Tools;
using FlowWorker.Infrastructure.OpenAI;
using Xunit;

namespace FlowWorker.Tests.Core;

/// <summary>
/// 工具执行结果格式化器测试
/// 测试 FormatToolResult 和 GetToolCallIdentifier 方法
/// </summary>
public class ToolResultFormatterTests
{
    /// <summary>
    /// 测试 FormatToolResult 方法 - 成功情况
    /// </summary>
    [Fact]
    public void FormatToolResult_WithContentData_ReturnsContent()
    {
        // Arrange
        var toolName = "read_file";
        var parameters = new Dictionary<string, string> { ["file_path"] = "test.txt" };
        var resultData = new { content = "This is the file content" };
        
        // 创建测试类实例来访问私有方法
        var formatter = new TestableOpenAIService();
        
        // Act
        var result = formatter.TestFormatToolResult(toolName, parameters, resultData);
        
        // Assert
        Assert.Equal("This is the file content", result);
    }

    /// <summary>
    /// 测试 FormatToolResult 方法 - 使用 data 字段
    /// </summary>
    [Fact]
    public void FormatToolResult_WithDataField_ReturnsData()
    {
        // Arrange
        var toolName = "list_files";
        var parameters = new Dictionary<string, string> { ["directory_path"] = "/test" };
        var resultData = new { data = new { files = new[] { "file1.txt", "file2.txt" } } };
        
        var formatter = new TestableOpenAIService();
        
        // Act
        var result = formatter.TestFormatToolResult(toolName, parameters, resultData);
        
        // Assert
        Assert.Contains("files", result);
    }

    /// <summary>
    /// 测试 FormatToolResult 方法 - null 数据
    /// </summary>
    [Fact]
    public void FormatToolResult_WithNullData_ReturnsNull()
    {
        // Arrange
        var toolName = "read_file";
        var parameters = new Dictionary<string, string>();
        object? resultData = null;
        
        var formatter = new TestableOpenAIService();
        
        // Act
        var result = formatter.TestFormatToolResult(toolName, parameters, resultData);
        
        // Assert
        Assert.Equal("null", result);
    }

    /// <summary>
    /// 测试 FormatToolResult 方法 - 文件列表
    /// </summary>
    [Fact]
    public void FormatToolResult_WithFilesArray_ReturnsFormattedList()
    {
        // Arrange
        var toolName = "list_files";
        var parameters = new Dictionary<string, string> { ["directory_path"] = "/test" };
        var resultData = new 
        { 
            files = new[] 
            { 
                new { path = "/test/file1.txt", type = "file" },
                new { path = "/test/dir1", type = "directory" }
            }
        };
        
        var formatter = new TestableOpenAIService();
        
        // Act
        var result = formatter.TestFormatToolResult(toolName, parameters, resultData);
        
        // Assert
        Assert.Contains("Files:", result);
        Assert.Contains("file1.txt", result);
        Assert.Contains("dir1", result);
    }

    /// <summary>
    /// 测试 GetToolCallIdentifier 方法 - file_path 参数
    /// </summary>
    [Fact]
    public void GetToolCallIdentifier_WithFilePath_ReturnsFilePath()
    {
        // Arrange
        var toolCall = new ToolCall
        {
            ToolName = "read_file",
            Parameters = new Dictionary<string, string> { ["file_path"] = "src/test.cs" }
        };
        
        var formatter = new TestableOpenAIService();
        
        // Act
        var result = formatter.TestGetToolCallIdentifier(toolCall);
        
        // Assert
        Assert.Equal("src/test.cs", result);
    }

    /// <summary>
    /// 测试 GetToolCallIdentifier 方法 - directory_path 参数
    /// </summary>
    [Fact]
    public void GetToolCallIdentifier_WithDirectoryPath_ReturnsDirectoryPath()
    {
        // Arrange
        var toolCall = new ToolCall
        {
            ToolName = "list_files",
            Parameters = new Dictionary<string, string> { ["directory_path"] = "/src" }
        };
        
        var formatter = new TestableOpenAIService();
        
        // Act
        var result = formatter.TestGetToolCallIdentifier(toolCall);
        
        // Assert
        Assert.Equal("/src", result);
    }

    /// <summary>
    /// 测试 GetToolCallIdentifier 方法 - path 参数
    /// </summary>
    [Fact]
    public void GetToolCallIdentifier_WithPath_ReturnsPath()
    {
        // Arrange
        var toolCall = new ToolCall
        {
            ToolName = "read_file",
            Parameters = new Dictionary<string, string> { ["path"] = "test.txt" }
        };
        
        var formatter = new TestableOpenAIService();
        
        // Act
        var result = formatter.TestGetToolCallIdentifier(toolCall);
        
        // Assert
        Assert.Equal("test.txt", result);
    }

    /// <summary>
    /// 测试 GetToolCallIdentifier 方法 - command 参数（长命令截断）
    /// </summary>
    [Fact]
    public void GetToolCallIdentifier_WithLongCommand_TruncatesTo50Chars()
    {
        // Arrange
        var longCommand = new string('a', 100); // 100 个字符的命令
        var toolCall = new ToolCall
        {
            ToolName = "execute_command",
            Parameters = new Dictionary<string, string> { ["command"] = longCommand }
        };
        
        var formatter = new TestableOpenAIService();
        
        // Act
        var result = formatter.TestGetToolCallIdentifier(toolCall);
        
        // Assert
        Assert.Equal(53, result.Length); // 50 个字符 + "..."
        Assert.EndsWith("...", result);
    }

    /// <summary>
    /// 测试 GetToolCallIdentifier 方法 - expression 参数
    /// </summary>
    [Fact]
    public void GetToolCallIdentifier_WithExpression_ReturnsExpression()
    {
        // Arrange
        var toolCall = new ToolCall
        {
            ToolName = "calculate",
            Parameters = new Dictionary<string, string> { ["expression"] = "2 + 2 * 5" }
        };
        
        var formatter = new TestableOpenAIService();
        
        // Act
        var result = formatter.TestGetToolCallIdentifier(toolCall);
        
        // Assert
        Assert.Equal("2 + 2 * 5", result);
    }

    /// <summary>
    /// 测试 GetToolCallIdentifier 方法 - url 参数
    /// </summary>
    [Fact]
    public void GetToolCallIdentifier_WithUrl_ReturnsUrl()
    {
        // Arrange
        var toolCall = new ToolCall
        {
            ToolName = "http_request",
            Parameters = new Dictionary<string, string> { ["url"] = "https://api.example.com/test" }
        };
        
        var formatter = new TestableOpenAIService();
        
        // Act
        var result = formatter.TestGetToolCallIdentifier(toolCall);
        
        // Assert
        Assert.Equal("https://api.example.com/test", result);
    }

    /// <summary>
    /// 测试 GetToolCallIdentifier 方法 - 无匹配参数时使用 JSON
    /// </summary>
    [Fact]
    public void GetToolCallIdentifier_NoMatchingParams_ReturnsJson()
    {
        // Arrange
        var toolCall = new ToolCall
        {
            ToolName = "custom_tool",
            Parameters = new Dictionary<string, string> { ["param1"] = "value1", ["param2"] = "value2" }
        };
        
        var formatter = new TestableOpenAIService();
        
        // Act
        var result = formatter.TestGetToolCallIdentifier(toolCall);
        
        // Assert
        Assert.Contains("param1", result);
        Assert.Contains("value1", result);
    }

    /// <summary>
    /// 测试 GetToolCallIdentifier 方法 - 长 JSON 截断
    /// </summary>
    [Fact]
    public void GetToolCallIdentifier_LongJson_TruncatesTo100Chars()
    {
        // Arrange
        var longParams = new Dictionary<string, string>();
        for (int i = 0; i < 50; i++)
        {
            longParams[$"param{i}"] = new string('a', 10);
        }
        
        var toolCall = new ToolCall
        {
            ToolName = "custom_tool",
            Parameters = longParams
        };
        
        var formatter = new TestableOpenAIService();
        
        // Act
        var result = formatter.TestGetToolCallIdentifier(toolCall);
        
        // Assert
        Assert.True(result.Length <= 103); // 100 个字符 + "..."
        Assert.EndsWith("...", result);
    }
}

/// <summary>
/// 用于测试的可测试 OpenAIService 类
/// 暴露内部方法用于单元测试
/// </summary>
public class TestableOpenAIService
{
    /// <summary>
    /// 测试 FormatToolResult 方法
    /// </summary>
    public string TestFormatToolResult(string toolName, Dictionary<string, string> parameters, object? resultData, string? mappedToolName = null)
    {
        return FormatToolResult(toolName, parameters, resultData, mappedToolName);
    }

    /// <summary>
    /// 测试 GetToolCallIdentifier 方法
    /// </summary>
    public string TestGetToolCallIdentifier(ToolCall toolCall)
    {
        return GetToolCallIdentifier(toolCall);
    }

    /// <summary>
    /// 格式化工具执行结果（发送给 AI 的格式）
    /// </summary>
    private string FormatToolResult(string toolName, Dictionary<string, string> parameters, object? resultData, string? mappedToolName = null)
    {
        if (resultData == null)
        {
            return "null";
        }

        // 将 object 转换为 JsonElement
        var resultElement = JsonSerializer.SerializeToElement(resultData);
        
        // 尝试获取 resultData 中的 content、data、result 等字段
        string? content = null;
        
        if (resultElement.TryGetProperty("content", out var contentProp))
        {
            content = contentProp.GetString();
        }
        else if (resultElement.TryGetProperty("data", out var dataProp))
        {
            content = dataProp.ToString();
        }
        else if (resultElement.TryGetProperty("result", out var resultProp))
        {
            content = resultProp.ToString();
        }
        else if (resultElement.TryGetProperty("output", out var outputProp))
        {
            content = outputProp.ToString();
        }
        else if (resultElement.TryGetProperty("files", out var filesProp) && filesProp.ValueKind == JsonValueKind.Array)
        {
            // 对于文件列表，格式化为易读的格式
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Files:");
            foreach (var file in filesProp.EnumerateArray())
            {
                var path = file.TryGetProperty("path", out var p) ? p.GetString() : "unknown";
                var type = file.TryGetProperty("type", out var t) ? t.GetString() : "file";
                sb.AppendLine($"  [{type}] {path}");
            }
            content = sb.ToString();
        }
        else
        {
            // 直接使用原始 JSON
            content = resultElement.ToString();
        }

        return content ?? "null";
    }

    /// <summary>
    /// 获取工具调用的标识符（用于构建结果消息）
    /// </summary>
    private string GetToolCallIdentifier(ToolCall toolCall)
    {
        // 尝试从参数中获取有意义的标识符
        // 优先使用 file_path, directory_path, path 等作为标识符
        if (toolCall.Parameters.TryGetValue("file_path", out var filePath))
        {
            return filePath;
        }
        
        if (toolCall.Parameters.TryGetValue("directory_path", out var dirPath))
        {
            return dirPath;
        }
        
        if (toolCall.Parameters.TryGetValue("path", out var path))
        {
            return path;
        }
        
        if (toolCall.Parameters.TryGetValue("command", out var command))
        {
            // 命令可能很长，截取前 50 个字符
            return command.Length > 50 ? command.Substring(0, 50) + "..." : command;
        }
        
        if (toolCall.Parameters.TryGetValue("expression", out var expression))
        {
            return expression;
        }
        
        if (toolCall.Parameters.TryGetValue("url", out var url))
        {
            return url;
        }
        
        // 如果没有找到合适的参数，使用所有参数的 JSON 表示（截取前 100 个字符）
        var allParams = JsonSerializer.Serialize(toolCall.Parameters);
        if (allParams.Length > 100)
        {
            allParams = allParams.Substring(0, 100) + "...";
        }
        return allParams;
    }
}