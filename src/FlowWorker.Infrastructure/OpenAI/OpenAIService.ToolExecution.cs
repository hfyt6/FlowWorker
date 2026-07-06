using System.Text.Json;
using FlowWorker.Core.Services;
using FlowWorker.Shared.Entities;
using Microsoft.Extensions.Logging;

namespace FlowWorker.Infrastructure.OpenAI;

/// <summary>
/// OpenAI 服务实现 - 工具执行相关方法
/// </summary>
public partial class OpenAIService
{
    /// <summary>
    /// 获取工具调用的标识符（用于构建结果消息）
    /// </summary>
    /// <param name="toolCall">工具调用信息</param>
    /// <returns>工具调用标识符字符串</returns>
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

    /// <summary>
    /// 格式化工具执行结果（发送给 AI 的格式）
    /// </summary>
    /// <param name="toolName">工具名称</param>
    /// <param name="parameters">工具参数</param>
    /// <param name="resultData">工具执行结果数据</param>
    /// <returns>格式化后的结果字符串</returns>
    private string FormatToolResult(string toolName, Dictionary<string, string> parameters, object? resultData)
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
    /// 映射参数名称，将 AI 返回的参数名映射为工具处理器期望的参数名
    /// </summary>
    /// <param name="toolName">工具名称</param>
    /// <param name="parameters">原始参数</param>
    /// <returns>映射后的参数</returns>
    private Dictionary<string, string> MapParameters(string toolName, Dictionary<string, string> parameters)
    {
        _logger.LogInformation("[工具调用链路]   ========== 开始参数名称映射 ==========");
        _logger.LogInformation("[工具调用链路]   工具名称：{ToolName}", toolName);
        _logger.LogInformation("[工具调用链路]   原始参数数量：{Count}", parameters.Count);
        
        // 记录所有原始参数
        foreach (var param in parameters)
        {
            _logger.LogInformation("[工具调用链路]     原始参数 [{Key}] = {Value}", param.Key, param.Value);
        }
        
        var mappedParameters = new Dictionary<string, string>(parameters, StringComparer.OrdinalIgnoreCase);
        var hasMapping = false;
        
        // 参数名称映射规则
        // AI 返回 path，但 FilesystemTool 期望 file_path
        if (parameters.TryGetValue("path", out var pathValue))
        {
            mappedParameters["file_path"] = pathValue;
            _logger.LogInformation("[工具调用链路]   参数映射：path -> file_path = {Value}", pathValue);
            hasMapping = true;
        }
        
        // directory_path 映射
        if (parameters.TryGetValue("directory", out var dirValue))
        {
            mappedParameters["directory_path"] = dirValue;
            _logger.LogInformation("[工具调用链路]   参数映射：directory -> directory_path = {Value}", dirValue);
            hasMapping = true;
        }
        
        // command 映射（ProcessTool）
        if (parameters.TryGetValue("cmd", out var cmdValue))
        {
            mappedParameters["command"] = cmdValue;
            _logger.LogInformation("[工具调用链路]   参数映射：cmd -> command = {Value}", cmdValue);
            hasMapping = true;
        }
        
        // expression 映射（CalculatorTool）
        if (parameters.TryGetValue("expr", out var exprValue))
        {
            mappedParameters["expression"] = exprValue;
            _logger.LogInformation("[工具调用链路]   参数映射：expr -> expression = {Value}", exprValue);
            hasMapping = true;
        }
        
        // 记录映射后的参数
        _logger.LogInformation("[工具调用链路]   映射后参数数量：{Count}", mappedParameters.Count);
        foreach (var param in mappedParameters)
        {
            _logger.LogInformation("[工具调用链路]     映射后参数 [{Key}] = {Value}", param.Key, param.Value);
        }
        
        if (hasMapping)
        {
            _logger.LogInformation("[工具调用链路]   参数名称映射已完成，存在参数名称变更");
        }
        else
        {
            _logger.LogInformation("[工具调用链路]   参数名称映射已完成，无需映射");
        }
        _logger.LogInformation("[工具调用链路]   ========== 参数名称映射结束 ==========");
        
        return mappedParameters;
    }

    /// <summary>
    /// 执行工具调用并返回结果
    /// </summary>
    /// <param name="toolCall">工具调用信息</param>
    /// <param name="workingDirectory">工作目录</param>
    /// <returns>工具执行结果字符串</returns>
    private async Task<string> ExecuteToolCallAsync(ToolCall toolCall, string workingDirectory)
    {
        string toolResult;
        
        try
        {
            // 将参数字典转换为 JsonElement
            var parametersJson = JsonSerializer.Serialize(toolCall.Parameters);
            var parametersElement = JsonSerializer.Deserialize<JsonElement>(parametersJson);
            
            // 参数名称映射
            var mappedParameters = MapParameters(toolCall.ToolName, toolCall.Parameters);
            
            // 如果参数中有相对路径，需要转换为绝对路径（相对于工作目录）
            if (mappedParameters.ContainsKey("file_path") || mappedParameters.ContainsKey("directory_path"))
            {
                var pathKey = mappedParameters.ContainsKey("file_path") ? "file_path" : "directory_path";
                var pathValue = mappedParameters[pathKey];
                
                if (!Path.IsPathRooted(pathValue))
                {
                    var absolutePath = Path.Combine(workingDirectory, pathValue);
                    mappedParameters[pathKey] = absolutePath;
                    _logger.LogInformation("[工具调用链路]   相对路径转换为绝对路径：{RelativePath} -> {AbsolutePath}", pathValue, absolutePath);
                }
            }
            
            var mappedParametersJson = JsonSerializer.Serialize(mappedParameters);
            var mappedParametersElement = JsonSerializer.Deserialize<JsonElement>(mappedParametersJson);
            _logger.LogDebug("[工具调用链路]   映射后的参数：{Parameters}", mappedParametersJson);
            
            // 检查工具是否在注册表中存在
            if (_toolRegistry.HasTool(toolCall.ToolName))
            {
                _logger.LogInformation("[工具调用链路]   找到已注册的工具：{ToolName}", toolCall.ToolName);
                
                // 获取工具处理器
                var toolHandler = _toolRegistry.GetTool(toolCall.ToolName);
                if (toolHandler == null)
                {
                    _logger.LogError("[工具调用链路]   获取工具处理器失败：{ToolName}", toolCall.ToolName);
                    return $"[Error] Failed to get tool handler: {toolCall.ToolName}";
                }
                
                // 使用细粒度工具名作为 action
                var action = toolCall.ToolName;
                
                _logger.LogInformation("[工具调用链路]   执行工具：{ToolName}，操作：{Action}", toolHandler.Name, action);
                
                var response = await toolHandler.ExecuteAsync(action, mappedParametersElement);
                response.ExecutionTime = 0; // 可以在外部计算
                
                if (response.Status == "success")
                {
                    _logger.LogInformation("[工具调用链路]   工具执行成功");
                    
                    if (response.Data != null)
                    {
                        _logger.LogInformation("[工具调用链路]   工具返回数据：{Data}", response.Data.ToString() ?? "null");
                    }
                    
                    toolResult = FormatToolResult(toolCall.ToolName, toolCall.Parameters, response.Data);
                }
                else
                {
                    _logger.LogError("[工具调用链路]   工具执行失败：{Error}", response.ErrorInfo?.Message);
                    toolResult = $"[Error] Tool '{toolCall.ToolName}' execution failed: {response.ErrorInfo?.Message ?? "Unknown error"}";
                }
            }
            else
            {
                _logger.LogError("[工具调用链路]   工具 '{ToolName}' 未在注册表中找到", toolCall.ToolName);
                _logger.LogDebug("[工具调用链路]   已注册的工具列表：{Tools}", string.Join(", ", _toolRegistry.GetAllTools().Select(t => t.Name)));
                toolResult = $"[Error] Tool '{toolCall.ToolName}' not found in registry";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("[工具调用链路]   工具执行异常：{Error}", ex.Message);
            toolResult = $"[Error] Tool '{toolCall.ToolName}' execution exception: {ex.Message}";
        }
        
        return toolResult;
    }
}