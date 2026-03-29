using System.Diagnostics;
using System.Text.Json;
using FlowWorker.Core.Interfaces;

namespace FlowWorker.Core.Services;

/// <summary>
/// 工具执行结果
/// </summary>
public class ToolExecutionResult
{
    public bool Success { get; set; }
    public ToolResponse? Response { get; set; }
    public string? Error { get; set; }
    public long ExecutionTime { get; set; }
}

/// <summary>
/// 工具执行器
/// 负责执行已注册的工具
/// </summary>
public class ToolExecutor
{
    private readonly ToolRegistry _registry;

    public ToolExecutor(ToolRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    /// <summary>
    /// 执行工具
    /// </summary>
    /// <param name="toolName">工具名称</param>
    /// <param name="action">操作名称</param>
    /// <param name="parameters">参数</param>
    /// <param name="workingDirectory">工作目录（可选）</param>
    /// <returns>执行结果</returns>
    public async Task<ToolExecutionResult> ExecuteAsync(string toolName, string action, JsonElement parameters, string? workingDirectory = null)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var tool = _registry.GetTool(toolName);
            if (tool == null)
            {
                return new ToolExecutionResult
                {
                    Success = false,
                    Error = $"工具 '{toolName}' 不存在",
                    ExecutionTime = stopwatch.ElapsedMilliseconds
                };
            }

            // 如果有工作目录，将其添加到参数中
            var parametersWithWorkingDir = parameters;
            if (!string.IsNullOrWhiteSpace(workingDirectory))
            {
                var jsonString = parameters.GetRawText();
                using var jsonDoc = JsonDocument.Parse(jsonString);
                var jsonElement = jsonDoc.RootElement.Clone();
                
                // 使用 JsonSerializer 反序列化再添加工作目录
                var parametersDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonString) ?? new();
                if (!parametersDict.ContainsKey("working_directory"))
                {
                    parametersDict["working_directory"] = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(workingDirectory));
                    parametersWithWorkingDir = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(parametersDict));
                }
            }
            
            var response = await tool.ExecuteAsync(action, parametersWithWorkingDir);
            response.ExecutionTime = stopwatch.ElapsedMilliseconds;
            
            return new ToolExecutionResult
            {
                Success = response.Status == "success",
                Response = response,
                ExecutionTime = stopwatch.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            return new ToolExecutionResult
            {
                Success = false,
                Error = ex.Message,
                ExecutionTime = stopwatch.ElapsedMilliseconds
            };
        }
    }

    /// <summary>
    /// 检查工具是否存在
    /// </summary>
    /// <param name="toolName">工具名称</param>
    /// <returns>是否存在</returns>
    public bool HasTool(string toolName)
    {
        return _registry.HasTool(toolName);
    }

    /// <summary>
    /// 获取所有可用工具
    /// </summary>
    /// <returns>工具列表</returns>
    public IEnumerable<ToolInfo> GetAllTools()
    {
        return _registry.GetAllTools();
    }
}