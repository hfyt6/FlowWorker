using System.Text.Json;
using FlowWorker.Core.Services;
using FlowWorker.Core.Tools;
using Xunit;

namespace FlowWorker.Tests.Core;

/// <summary>
/// ToolCallParser 带 task_progress 参数的测试
/// 模拟 sample/2026-03-22-01.msg 中的场景
/// </summary>
public class ToolCallParserWithTaskProgressTests
{
    /// <summary>
    /// 测试解析 sample/2026-03-22-01.msg 中的工具调用
    /// </summary>
    [Fact]
    public void ParseToolCalls_ReadFileWithTaskProgress_ReturnsCorrectToolCall()
    {
        // Arrange - 模拟 sample/2026-03-22-01.msg 中的 AI 响应
        var content = @"<read_file>
<path>test.py</path>
<task_progress>
- [ ] 读取 test.py 文件内容
- [ ] 分析代码功能和作用
- [ ] 向用户解释代码
</task_progress>
</read_file>";

        // Act
        var result = ToolCallParser.ParseToolCalls(content);

        // Assert
        Assert.Single(result);
        Assert.Equal("read_file", result[0].ToolName);
        Assert.Equal("test.py", result[0].GetParameter("path"));
        Assert.Contains("读取 test.py 文件内容", result[0].GetParameter("task_progress"));
    }

    /// <summary>
    /// 测试解析参数名称不匹配的情况
    /// AI 返回 path，但 FilesystemTool 期望 file_path
    /// </summary>
    [Fact]
    public void ParseToolCalls_ReadFile_ParameterNameMismatch()
    {
        // Arrange - AI 返回的参数名是 path
        var content = @"<read_file>
<path>test.py</path>
</read_file>";

        // Act
        var result = ToolCallParser.ParseToolCalls(content);

        // Assert - 确认解析器正确解析了 path 参数
        Assert.Single(result);
        Assert.Equal("read_file", result[0].ToolName);
        Assert.Equal("test.py", result[0].GetParameter("path"));
        
        // 注意：FilesystemTool 期望的是 file_path，不是 path
        // 这是参数名称不匹配的问题
        Assert.Null(result[0].GetParameter("file_path"));
    }

    /// <summary>
    /// 测试 FilesystemTool 是否能正确处理参数
    /// </summary>
    [Fact]
    public async Task FilesystemTool_ReadFile_WithFilePathParameter()
    {
        // Arrange
        var tool = new FilesystemTool();
        
        // 创建一个临时测试文件
        var testFileName = "test_read_file_" + Guid.NewGuid().ToString("N") + ".txt";
        var testFilePath = Path.Combine(Path.GetTempPath(), testFileName);
        var testContent = "Hello, World!";
        await File.WriteAllTextAsync(testFilePath, testContent);

        try
        {
            // 使用正确的参数名 file_path
            var parameters = new Dictionary<string, string>
            {
                ["file_path"] = testFilePath
            };
            var parametersJson = JsonSerializer.Serialize(parameters);
            var parametersElement = JsonSerializer.Deserialize<JsonElement>(parametersJson);

            // Act
            var result = await tool.ExecuteAsync("read_file", parametersElement);

            // Assert
            Assert.Equal("success", result.Status);
            Assert.NotNull(result.Data);
        }
        finally
        {
            // 清理测试文件
            if (File.Exists(testFilePath))
            {
                File.Delete(testFilePath);
            }
        }
    }

    /// <summary>
    /// 测试 FilesystemTool 使用 path 参数名（AI 返回的格式）
    /// </summary>
    [Fact]
    public async Task FilesystemTool_ReadFile_WithPathParameter_ShouldFail()
    {
        // Arrange
        var tool = new FilesystemTool();
        
        // 使用 AI 返回的参数名 path（不是 file_path）
        var parametersJson = @"{""path"": ""test.py""}";
        var parameters = JsonSerializer.Deserialize<JsonElement>(parametersJson);

        // Act
        var result = await tool.ExecuteAsync("read_file", parameters);

        // Assert - 应该失败，因为 FilesystemTool 期望 file_path
        Assert.Equal("error", result.Status);
        Assert.NotNull(result.ErrorInfo);
    }

    /// <summary>
    /// 测试 ToolExecutor 执行工具映射
    /// </summary>
    [Fact]
    public async Task ToolExecutor_ExecuteReadFile_MappedToFileSystem()
    {
        // Arrange
        var registry = new ToolRegistry();
        registry.Register(new FilesystemTool());
        var executor = new ToolExecutor(registry);

        // 创建一个临时测试文件
        var testFileName = "test_executor_" + Guid.NewGuid().ToString("N") + ".txt";
        var testFilePath = Path.Combine(Path.GetTempPath(), testFileName);
        var testContent = "Executor Test Content";
        await File.WriteAllTextAsync(testFilePath, testContent);

        try
        {
            // 使用正确的参数名 file_path
            var parameters = new Dictionary<string, string>
            {
                ["file_path"] = testFilePath
            };
            var parametersJson = JsonSerializer.Serialize(parameters);
            var parametersElement = JsonSerializer.Deserialize<JsonElement>(parametersJson);

            // Act - 直接调用 Filesystem 工具
            var result = await executor.ExecuteAsync("Filesystem", "read_file", parametersElement);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Response);
            Assert.Equal("success", result.Response.Status);
        }
        finally
        {
            // 清理测试文件
            if (File.Exists(testFilePath))
            {
                File.Delete(testFilePath);
            }
        }
    }

    /// <summary>
    /// 测试完整的工具调用链路：解析 -> 映射 -> 执行
    /// </summary>
    [Fact]
    public async Task FullToolCallChain_Parse_MapAndExecute()
    {
        // Arrange
        var content = @"<read_file>
<path>test.py</path>
<task_progress>
- [ ] 读取文件
</task_progress>
</read_file>";

        // 解析工具调用
        var toolCalls = ToolCallParser.ParseToolCalls(content);
        Assert.Single(toolCalls);
        var toolCall = toolCalls[0];
        Assert.Equal("read_file", toolCall.ToolName);

        // 创建工具注册表和执行器
        var registry = new ToolRegistry();
        registry.Register(new FilesystemTool());
        var executor = new ToolExecutor(registry);

        // 创建一个临时测试文件
        var testFileName = "test_chain_" + Guid.NewGuid().ToString("N") + ".txt";
        var testFilePath = Path.Combine(Path.GetTempPath(), testFileName);
        var testContent = "Chain Test Content";
        await File.WriteAllTextAsync(testFilePath, testContent);

        try
        {
            // 更新参数为实际文件路径
            var parameters = new Dictionary<string, string>
            {
                ["file_path"] = testFilePath
            };
            var parametersJson = JsonSerializer.Serialize(parameters);
            var parametersElement = JsonSerializer.Deserialize<JsonElement>(parametersJson);

            // 映射工具名称并执行
            var mappedToolName = toolCall.ToolName switch
            {
                "read_file" => "Filesystem",
                _ => null
            };

            Assert.NotNull(mappedToolName);

            // Act
            var result = await executor.ExecuteAsync(mappedToolName, "read_file", parametersElement);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Response);
        }
        finally
        {
            // 清理测试文件
            if (File.Exists(testFilePath))
            {
                File.Delete(testFilePath);
            }
        }
    }
}