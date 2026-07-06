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
/// OpenAI 服务实现 - 流式消息处理相关方法
/// </summary>
public partial class OpenAIService
{
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
                string toolResult = await ExecuteToolCallAsync(toolCall, workingDirectory);
                
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
}