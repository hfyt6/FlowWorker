using System.Net.Http.Json;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using FlowWorker.Core.Interfaces;
using FlowWorker.Core.Services;
using FlowWorker.Shared.DTOs;
using FlowWorker.Shared.Entities;
using Microsoft.Extensions.Logging;

namespace FlowWorker.Infrastructure.OpenAI;

/// <summary>
/// OpenAI 服务实现
/// 支持多种请求格式模式
/// </summary>
public class OpenAIService : IOpenAIService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenAIService> _logger;
    private readonly IRequestFormatterFactory _formatterFactory;
    private readonly ToolExecutor _toolExecutor;
    private readonly ToolRegistry _toolRegistry;

    public OpenAIService(
        HttpClient httpClient,
        ILogger<OpenAIService> logger,
        IRequestFormatterFactory formatterFactory,
        ToolExecutor toolExecutor,
        ToolRegistry toolRegistry)
    {
        _httpClient = httpClient;
        _logger = logger;
        _formatterFactory = formatterFactory;
        _toolExecutor = toolExecutor;
        _toolRegistry = toolRegistry;
    }

    public async Task<string> SendMessageAsync(
        string apiKey,
        string baseUrl,
        string model,
        IEnumerable<Message> messages,
        string? systemPrompt = null,
        decimal? temperature = null,
        int? maxTokens = null,
        string? requestFormat = null,
        Session? session = null)
    {
        // 获取请求格式化器（默认使用 cline 模式）
        var formatter = GetFormatter(requestFormat);

        // 构建请求体
        var requestBody = formatter.BuildRequestBody(
            model, messages, systemPrompt, temperature, maxTokens, false, session);

        // 构建 API URL
        var apiUrl = formatter.BuildApiUrl(baseUrl, "/chat/completions");

        // 序列化请求体，使用不转义 Unicode 的选项
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        var requestJson = JsonSerializer.Serialize(requestBody, jsonOptions);

        _logger.LogDebug("OpenAI 请求：{Request}", requestJson);

        // 创建 HTTP 请求，使用 StringContent 来确保 Content-Length 正确设置
        var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, apiUrl)
        {
            Content = content
        };

        // 添加请求头
        var headers = formatter.GetRequestHeaders(apiKey);
        foreach (var header in headers)
        {
            httpRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        // Debug 日志：记录完整请求信息
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("========== OpenAI API 请求开始 ==========");
            _logger.LogDebug("请求格式模式：{FormatMode}", formatter.Name);
            _logger.LogDebug("请求 URL: {Url}", apiUrl);
            _logger.LogDebug("请求方法：{Method}", httpRequest.Method);
            _logger.LogDebug("请求头:");
            foreach (var header in httpRequest.Headers)
            {
                var value = header.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase)
                    ? "Bearer ***"
                    : string.Join(", ", header.Value);
                _logger.LogDebug("  {Key}: {Value}", header.Key, value);
            }
            if (httpRequest.Content?.Headers != null)
            {
                foreach (var header in httpRequest.Content.Headers)
                {
                    _logger.LogDebug("  {Key}: {Value}", header.Key, string.Join(", ", header.Value));
                }
            }
            _logger.LogDebug("请求体：{RequestBody}", requestJson);
            _logger.LogDebug("========================================");
        }

        using var response = await _httpClient.SendAsync(httpRequest);

        // Debug 日志：记录响应信息
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("========== OpenAI API 响应开始 ==========");
            _logger.LogDebug("响应状态码：{StatusCode}", (int)response.StatusCode);
            _logger.LogDebug("响应头:");
            foreach (var header in response.Headers)
            {
                _logger.LogDebug("  {Key}: {Value}", header.Key, string.Join(", ", header.Value));
            }
            if (response.Content?.Headers != null)
            {
                foreach (var header in response.Content.Headers)
                {
                    _logger.LogDebug("  {Key}: {Value}", header.Key, string.Join(", ", header.Value));
                }
            }
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("OpenAI API 错误：StatusCode={StatusCode}, Response={Response}", response.StatusCode, errorContent);
        }

        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();

        // Debug 日志：记录响应体
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("响应体：{ResponseBody}", responseContent);
            _logger.LogDebug("========================================");
        }

        // ========== 工具调用解析开始 (SendMessageAsync) ==========
        _logger.LogInformation("========== 开始解析工具调用 (SendMessageAsync) ==========");
        var parsedResponse = formatter.ParseResponse(responseContent);
        _logger.LogDebug("待解析内容长度：{Length} 字符", parsedResponse.Length);
        _logger.LogDebug("待解析内容：{Content}", parsedResponse);
        
        bool hasToolCalls = ToolCallParser.ContainsToolCalls(parsedResponse);
        _logger.LogInformation("是否检测到工具调用标记：{HasToolCalls}", hasToolCalls);
        
        if (hasToolCalls)
        {
            var toolCalls = ToolCallParser.ParseToolCalls(parsedResponse);
            _logger.LogInformation("解析到的工具调用数量：{Count}", toolCalls.Count);
            
            foreach (var toolCall in toolCalls)
            {
                _logger.LogInformation("----------------------------------------");
                _logger.LogInformation("工具名称：{ToolName}", toolCall.ToolName);
                _logger.LogInformation("参数数量：{ParamCount}", toolCall.Parameters.Count);
                
                foreach (var param in toolCall.Parameters)
                {
                    _logger.LogInformation("  参数 [{Key}] = {Value}", param.Key, param.Value);
                }
                
                _logger.LogInformation("原始内容：{RawContent}", toolCall.RawContent);
            }
            _logger.LogInformation("========================================");
        }
        else
        {
            _logger.LogInformation("未检测到工具调用，返回纯文本响应");
        }
        // ========== 工具调用解析结束 ==========

        return parsedResponse;
    }

    public async Task<string> SendMessageStreamAsync(
        string apiKey,
        string baseUrl,
        string model,
        IEnumerable<Message> messages,
        Func<StreamContentChunk, Task> onChunk,
        string? systemPrompt = null,
        decimal? temperature = null,
        int? maxTokens = null,
        string? requestFormat = null,
        Session? session = null)
    {
        // 内部递归调用，最大深度为 5
        return await SendMessageStreamAsyncInternal(
            apiKey, baseUrl, model, messages, onChunk, 
            systemPrompt, temperature, maxTokens, requestFormat, session, 0);
    }

    /// <summary>
    /// 内部递归实现的流式消息发送方法
    /// </summary>
    private async Task<string> SendMessageStreamAsyncInternal(
        string apiKey,
        string baseUrl,
        string model,
        IEnumerable<Message> messages,
        Func<StreamContentChunk, Task> onChunk,
        string? systemPrompt,
        decimal? temperature,
        int? maxTokens,
        string? requestFormat,
        Session? session,
        int recursionDepth)
    {
        const int MaxRecursionDepth = 5;
        
        if (recursionDepth >= MaxRecursionDepth)
        {
            _logger.LogWarning("[工具调用链路] 达到最大递归深度 {MaxDepth}，停止递归", MaxRecursionDepth);
            return "达到最大递归深度，停止工具调用";
        }

        // 获取当前会话的工作目录
        var workingDirectory = session?.WorkingDirectory ?? Directory.GetCurrentDirectory();
        _logger.LogInformation("[工具调用链路] 当前工作目录：{WorkingDirectory}", workingDirectory);
        
        // 获取请求格式化器（默认使用 cline 模式）
        var formatter = GetFormatter(requestFormat);

        // 构建请求体
        var requestBody = formatter.BuildRequestBody(
            model, messages, systemPrompt, temperature, maxTokens, true, session);

        // 构建 API URL
        var apiUrl = formatter.BuildApiUrl(baseUrl, "/chat/completions");

        // 序列化请求体，使用不转义 Unicode 的选项
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        var requestJson = JsonSerializer.Serialize(requestBody, jsonOptions);

        // 创建 HTTP 请求，使用 StringContent 来确保 Content-Length 正确设置
        var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, apiUrl)
        {
            Content = content
        };

        // 添加请求头
        var headers = formatter.GetRequestHeaders(apiKey);
        foreach (var header in headers)
        {
            httpRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        // Debug 日志：记录完整请求信息
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("========== OpenAI API 流式请求开始 ==========");
            _logger.LogDebug("请求格式模式：{FormatMode}", formatter.Name);
            _logger.LogDebug("请求 URL: {Url}", apiUrl);
            _logger.LogDebug("请求方法：{Method}", httpRequest.Method);
            _logger.LogDebug("请求头:");
            foreach (var header in httpRequest.Headers)
            {
                var value = header.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase)
                    ? "Bearer ***"
                    : string.Join(", ", header.Value);
                _logger.LogDebug("  {Key}: {Value}", header.Key, value);
            }
            if (httpRequest.Content?.Headers != null)
            {
                foreach (var header in httpRequest.Content.Headers)
                {
                    _logger.LogDebug("  {Key}: {Value}", header.Key, string.Join(", ", header.Value));
                }
            }
            _logger.LogDebug("请求体：{RequestBody}", requestJson);
            _logger.LogDebug("========================================");
        }

        using var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead);

        // Debug 日志：记录响应头信息
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("========== OpenAI API 流式响应开始 ==========");
            _logger.LogDebug("响应状态码：{StatusCode}", (int)response.StatusCode);
            _logger.LogDebug("响应头:");
            foreach (var header in response.Headers)
            {
                _logger.LogDebug("  {Key}: {Value}", header.Key, string.Join(", ", header.Value));
            }
            if (response.Content?.Headers != null)
            {
                foreach (var header in response.Content.Headers)
                {
                    _logger.LogDebug("  {Key}: {Value}", header.Key, string.Join(", ", header.Value));
                }
            }
            _logger.LogDebug("流式响应内容:");
        }

        if (response.StatusCode != System.Net.HttpStatusCode.OK)
        {
            string errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("请求不 OK： {Error}", errorContent);
        }
        response.EnsureSuccessStatusCode();

        var fullContent = new StringBuilder();

        using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (line.StartsWith("data: "))
            {
                var data = line.Substring(6);
                if (data == "[DONE]")
                {
                    if (_logger.IsEnabled(LogLevel.Debug))
                    {
                        _logger.LogDebug("  [DONE]");
                    }
                    break;
                }

                try
                {
                    // 使用格式化器解析流式块
                    var chunkContent = formatter.ParseStreamChunk(data);

                    if (chunkContent != null)
                    {
                        fullContent.Append(chunkContent);

                        // Debug 日志：记录每个流式块
                        if (_logger.IsEnabled(LogLevel.Debug))
                        {
                            _logger.LogDebug("  Chunk: {Content}", chunkContent);
                        }

                        await onChunk(new StreamContentChunk
                        {
                            Type = "content",
                            Content = chunkContent
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("[OpenAI Stream] 解析 chunk 失败：{Error}", ex.Message);
                }
            }
        }

        // Debug 日志：记录完整响应内容
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("完整响应内容：{FullContent}", fullContent.ToString());
            _logger.LogDebug("========================================");
        }

        // ========== 工具调用解析开始 ==========
        _logger.LogInformation("========================================");
        _logger.LogInformation("[工具调用链路] 开始解析工具调用");
        _logger.LogInformation("[工具调用链路] 完整响应内容长度：{Length} 字符", fullContent.Length);
        
        var completeContent = fullContent.ToString();
        
        // 记录完整响应内容（用于调试）
        _logger.LogDebug("[工具调用链路] 完整响应内容:\n{Content}", completeContent);
        
        bool hasToolCalls = ToolCallParser.ContainsToolCalls(completeContent);
        _logger.LogInformation("[工具调用链路] 是否检测到工具调用标记：{HasToolCalls}", hasToolCalls);
        
        if (hasToolCalls)
        {
            var toolCalls = ToolCallParser.ParseToolCalls(completeContent);
            _logger.LogInformation("[工具调用链路] 解析到的工具调用数量：{Count}", toolCalls.Count);
            
            // 收集所有工具执行结果
            var toolResults = new List<string>();
            
            for (int i = 0; i < toolCalls.Count; i++)
            {
                var toolCall = toolCalls[i];
                _logger.LogInformation("----------------------------------------");
                _logger.LogInformation("[工具调用链路] 工具调用 #{Index}", i + 1);
                _logger.LogInformation("[工具调用链路]   工具名称：{ToolName}", toolCall.ToolName);
                _logger.LogInformation("[工具调用链路]   参数数量：{ParamCount}", toolCall.Parameters.Count);
                
                foreach (var param in toolCall.Parameters)
                {
                    _logger.LogInformation("[工具调用链路]   参数 [{Key}] = {Value}", param.Key, param.Value);
                }
                
                _logger.LogInformation("[工具调用链路]   原始 XML 内容：{RawContent}", toolCall.RawContent.Replace("\n", "\\n"));
                
                // ========== 后端执行工具调用 ==========
                _logger.LogInformation("[工具调用链路] >>> 开始在后端执行工具...");
                
                // 执行工具并获取结果
                string toolResult;
                
                try
                {
                    // 将参数字典转换为 JsonElement
                    var parametersJson = JsonSerializer.Serialize(toolCall.Parameters);
                    var parametersElement = JsonSerializer.Deserialize<JsonElement>(parametersJson);
                    
                    // 使用工具名称作为 action（对于需要 action 的工具）
                    var action = toolCall.ToolName;
                    
                    // ========== 参数名称映射 ==========
                    // AI 返回的参数名可能与工具处理器期望的参数名不一致，需要进行映射
                    var mappedParameters = MapParameters(toolCall.ToolName, toolCall.Parameters);
                    
                    // 如果参数中有相对路径，需要转换为绝对路径（相对于工作目录）
                    if (mappedParameters.ContainsKey("file_path") || mappedParameters.ContainsKey("directory_path"))
                    {
                        var pathKey = mappedParameters.ContainsKey("file_path") ? "file_path" : "directory_path";
                        var pathValue = mappedParameters[pathKey];
                        
                        // 如果是相对路径，转换为绝对路径
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
                        
                        // 执行工具
                        var result = await _toolExecutor.ExecuteAsync(toolCall.ToolName, action, mappedParametersElement);
                        
                        if (result.Success)
                        {
                            _logger.LogInformation("[工具调用链路]   工具执行成功，耗时：{ExecutionTime}ms", result.ExecutionTime);
                            
                            // 记录工具返回的数据内容
                            if (result.Response?.Data != null)
                            {
                                _logger.LogInformation("[工具调用链路]   工具返回数据：{Data}", result.Response.Data.ToString() ?? "null");
                            }
                            
                            // 构建格式化的工具执行结果（发送给 AI 的格式）
                            toolResult = FormatToolResult(toolCall.ToolName, toolCall.Parameters, result.Response?.Data);
                        }
                        else
                        {
                            _logger.LogError("[工具调用链路]   工具执行失败：{Error}", result.Error);
                            toolResult = $"[Error] Tool '{toolCall.ToolName}' execution failed: {result.Error}";
                        }
                    }
                    else
                    {
                        // 工具不存在，尝试使用工具名称作为 action 调用 FilesystemTool 等通用工具
                        _logger.LogInformation("[工具调用链路]   工具 '{ToolName}' 未在注册表中找到，尝试作为操作名执行..." , toolCall.ToolName);
                        
                        // 记录所有已注册的工具名称
                        _logger.LogDebug("[工具调用链路]   已注册的工具列表：{Tools}", string.Join(", ", _toolRegistry.GetAllTools().Select(t => t.Name)));
                        
                        // 对于 read_file、write_file 等操作，映射到 FilesystemTool
                        var mappedToolName = toolCall.ToolName switch
                        {
                            "read_file" => "Filesystem",
                            "write_file" => "Filesystem",
                            "list_files" => "Filesystem",
                            "file_exists" => "Filesystem",
                            "delete_file" => "Filesystem",
                            "create_directory" => "Filesystem",
                            "execute_command" => "Process",
                            "run_process" => "Process",
                            "kill_process" => "Process",
                            "list_processes" => "Process",
                            "get_process_info" => "Process",
                            "http_request" => "Network",
                            "download_file" => "Network",
                            "ping" => "Network",
                            "dns_lookup" => "Network",
                            "check_port" => "Network",
                            "calculate" => "Calculator",
                            "read_code" => "CodeAnalysis",
                            "write_code" => "CodeManipulation",
                            "search_code" => "CodeAnalysis",
                            "analyze_code" => "CodeAnalysis",
                            "git_status" => "VersionControl",
                            "git_commit" => "VersionControl",
                            "git_push" => "VersionControl",
                            "git_pull" => "VersionControl",
                            _ => null
                        };
                        
                        _logger.LogInformation("[工具调用链路]   尝试映射到工具：{MappedToolName}", mappedToolName ?? "null");
                        
                        if (mappedToolName != null)
                        {
                            // 检查映射后的工具是否存在
                            if (_toolRegistry.HasTool(mappedToolName))
                            {
                                _logger.LogInformation("[工具调用链路]   找到映射的工具：{MappedToolName}", mappedToolName);
                                
                                // 记录要执行的 action 和参数
                                _logger.LogInformation("[工具调用链路]   执行操作：{Action}", toolCall.ToolName);
                                _logger.LogInformation("[工具调用链路]   执行参数：{Parameters}", mappedParametersJson);
                                
                                var result = await _toolExecutor.ExecuteAsync(mappedToolName, toolCall.ToolName, mappedParametersElement);
                                
                                _logger.LogInformation("[工具调用链路]   工具执行结果：Success={Success}, Error={Error}, Response.Status={ResponseStatus}, Response.ErrorInfo={ErrorInfo}", 
                                    result.Success, 
                                    result.Error ?? "null",
                                    result.Response?.Status ?? "null",
                                    result.Response?.ErrorInfo != null ? $"{result.Response.ErrorInfo.Code}: {result.Response.ErrorInfo.Message}" : "null");
                                
                                if (result.Success)
                                {
                                    _logger.LogInformation("[工具调用链路]   工具执行成功，耗时：{ExecutionTime}ms", result.ExecutionTime);
                                    
                                    // 记录工具返回的数据内容
                                    if (result.Response?.Data != null)
                                    {
                                        _logger.LogInformation("[工具调用链路]   工具返回数据：{Data}", result.Response.Data.ToString() ?? "null");
                                    }
                                    
                                    // 构建格式化的工具执行结果（发送给 AI 的格式）
                                    toolResult = FormatToolResult(toolCall.ToolName, toolCall.Parameters, result.Response?.Data, mappedToolName);
                                }
                                else
                                {
                                    _logger.LogError("[工具调用链路]   工具执行失败：{Error}, Response Status={Status}, Response Data={Data}", 
                                        result.Error ?? "null", 
                                        result.Response?.Status ?? "null",
                                        result.Response?.Data ?? "null");
                                    toolResult = $"[Error] Tool '{toolCall.ToolName}' (mapped to '{mappedToolName}') execution failed: {result.Error ?? result.Response?.ErrorInfo?.Message ?? "Unknown error"}";
                                }
                            }
                            else
                            {
                                _logger.LogError("[工具调用链路]   映射的工具 '{MappedToolName}' 也未在注册表中找到", mappedToolName);
                                toolResult = $"[Error] Tool processor not found: {mappedToolName}";
                            }
                        }
                        else
                        {
                            _logger.LogError("[工具调用链路]   未找到对应的工具处理器：{ToolName}", toolCall.ToolName);
                            toolResult = $"[Error] Tool processor not found: {toolCall.ToolName}";
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("[工具调用链路]   工具执行异常：{Error}", ex.Message);
                    toolResult = $"[Error] Tool '{toolCall.ToolName}' execution exception: {ex.Message}";
                }
                
                // 收集工具执行结果
                var toolResultMessage = $"[{toolCall.ToolName} for '{GetToolCallIdentifier(toolCall)}'] Result:\n{toolResult}";
                _logger.LogInformation("[工具调用链路]   构建工具执行结果消息：{Message}", toolResultMessage);
                toolResults.Add(toolResultMessage);
            }
            
            // 将所有工具执行结果合并
            var allToolResults = string.Join("\n\n", toolResults);
            
            // 发送工具执行结果到前端
            await onChunk(new StreamContentChunk
            {
                Type = "content",
                Content = allToolResults + "\n\n"
            });
            
            // ========== 递归调用：将工具执行结果发送给 AI 获取下一步响应 ==========
            _logger.LogInformation("[工具调用链路] >>> 开始递归调用：将工具执行结果发送给 AI");
            
            // 构建新的消息列表，添加工具执行结果作为 user 消息
            var messagesList = messages.ToList();
            
            // 添加 assistant 消息（AI 的工具调用请求）
            messagesList.Add(new Message
            {
                Role = MessageRole.Assistant,
                Content = completeContent
            });
            
            // 添加 user 消息（工具执行结果）
            messagesList.Add(new Message
            {
                Role = MessageRole.User,
                Content = allToolResults
            });
            
            // 递归调用，深度 +1
            var nextResponse = await SendMessageStreamAsyncInternal(
                apiKey, 
                baseUrl, 
                model, 
                messagesList, 
                onChunk, 
                systemPrompt, 
                temperature, 
                maxTokens, 
                requestFormat, 
                session, 
                recursionDepth + 1);
            
            _logger.LogInformation("[工具调用链路] >>> 递归调用完成");
            
            return nextResponse;
        }
        else
        {
            _logger.LogInformation("[工具调用链路] 未检测到工具调用，返回纯文本响应");
        }
        _logger.LogInformation("========================================");
        // ========== 工具调用解析结束 ==========

        return completeContent;
    }

    public async Task<IReadOnlyList<string>> GetModelsAsync(string apiKey, string baseUrl, string? requestFormat = null)
    {
        // 获取请求格式化器（默认使用 cline 模式）
        var formatter = GetFormatter(requestFormat);

        // 构建 API URL
        var apiUrl = formatter.BuildApiUrl(baseUrl, "/models");

        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, apiUrl);

        // 添加请求头
        var headers = formatter.GetRequestHeaders(apiKey);
        foreach (var header in headers)
        {
            httpRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        using var response = await _httpClient.SendAsync(httpRequest);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OpenAiModelsResponse>();
        return result?.Data?.Select(m => m.Id).ToList() ?? new List<string>();
    }

    /// <summary>
    /// 获取请求格式化器
    /// </summary>
    /// <param name="requestFormat">请求格式名称，如果为空则使用默认格式</param>
    /// <returns>请求格式化器</returns>
    private IRequestFormatter GetFormatter(string? requestFormat)
    {
        if (string.IsNullOrWhiteSpace(requestFormat))
        {
            return _formatterFactory.GetDefaultFormatter();
        }

        return _formatterFactory.GetFormatter(requestFormat);
    }

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
    /// <param name="mappedToolName">映射后的工具名称（可选）</param>
    /// <returns>格式化后的结果字符串</returns>
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
            var sb = new StringBuilder();
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
}

/// <summary>
/// OpenAI 模型响应
/// </summary>
public class OpenAiModelsResponse
{
    public string Object { get; set; } = string.Empty;
    public List<OpenAiModel> Data { get; set; } = new();
}

/// <summary>
/// OpenAI 模型
/// </summary>
public class OpenAiModel
{
    public string Id { get; set; } = string.Empty;
    public string Object { get; set; } = string.Empty;
    public DateTime Created { get; set; }
    public string Owner { get; set; } = string.Empty;
}