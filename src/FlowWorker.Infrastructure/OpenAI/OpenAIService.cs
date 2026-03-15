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

    public OpenAIService(
        HttpClient httpClient,
        ILogger<OpenAIService> logger,
        IRequestFormatterFactory formatterFactory)
    {
        _httpClient = httpClient;
        _logger = logger;
        _formatterFactory = formatterFactory;
    }

    public async Task<string> SendMessageAsync(
        string apiKey,
        string baseUrl,
        string model,
        IEnumerable<Message> messages,
        string? systemPrompt = null,
        decimal? temperature = null,
        int? maxTokens = null,
        string? requestFormat = null)
    {
        // 获取请求格式化器（默认使用 cline 模式）
        var formatter = GetFormatter(requestFormat);

        // 构建请求体
        var requestBody = formatter.BuildRequestBody(
            model, messages, systemPrompt, temperature, maxTokens, false);

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

        _logger.LogDebug("OpenAI请求: {Request}", requestJson);

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

        // Debug日志：记录完整请求信息
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("========== OpenAI API 请求开始 ==========");
            _logger.LogDebug("请求格式模式: {FormatMode}", formatter.Name);
            _logger.LogDebug("请求URL: {Url}", apiUrl);
            _logger.LogDebug("请求方法: {Method}", httpRequest.Method);
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
            _logger.LogDebug("请求体: {RequestBody}", requestJson);
            _logger.LogDebug("========================================");
        }

        using var response = await _httpClient.SendAsync(httpRequest);

        // Debug日志：记录响应信息
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("========== OpenAI API 响应开始 ==========");
            _logger.LogDebug("响应状态码: {StatusCode}", (int)response.StatusCode);
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
            _logger.LogError("OpenAI API错误: StatusCode={StatusCode}, Response={Response}", response.StatusCode, errorContent);
        }

        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();

        // Debug日志：记录响应体
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("响应体: {ResponseBody}", responseContent);
            _logger.LogDebug("========================================");
        }

        // 使用格式化器解析响应
        var parsedResponse = formatter.ParseResponse(responseContent);

        // 解析工具调用
        if (ToolCallParser.ContainsToolCalls(parsedResponse))
        {
            var toolCalls = ToolCallParser.ParseToolCalls(parsedResponse);
            foreach (var toolCall in toolCalls)
            {
                _logger.LogInformation("检测到工具调用: {ToolName}, 参数: {Parameters}",
                    toolCall.ToolName,
                    string.Join(", ", toolCall.Parameters.Select(p => $"{p.Key}={p.Value}")));
            }
        }

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
        string? requestFormat = null)
    {
        // 获取请求格式化器（默认使用 cline 模式）
        var formatter = GetFormatter(requestFormat);

        // 构建请求体
        var requestBody = formatter.BuildRequestBody(
            model, messages, systemPrompt, temperature, maxTokens, true);

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

        // Debug日志：记录完整请求信息
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("========== OpenAI API 流式请求开始 ==========");
            _logger.LogDebug("请求格式模式: {FormatMode}", formatter.Name);
            _logger.LogDebug("请求URL: {Url}", apiUrl);
            _logger.LogDebug("请求方法: {Method}", httpRequest.Method);
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
            _logger.LogDebug("请求体: {RequestBody}", requestJson);
            _logger.LogDebug("========================================");
        }

        using var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead);

        // Debug日志：记录响应头信息
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("========== OpenAI API 流式响应开始 ==========");
            _logger.LogDebug("响应状态码: {StatusCode}", (int)response.StatusCode);
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
            _logger.LogError("请求不OK： {Error}", errorContent);
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

                        // Debug日志：记录每个流式块
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
                    _logger.LogWarning("[OpenAI Stream] 解析 chunk 失败: {Error}", ex.Message);
                }
            }
        }

        // Debug日志：记录完整响应内容
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("完整响应内容: {FullContent}", fullContent.ToString());
            _logger.LogDebug("========================================");
        }

        // 解析工具调用
        var completeContent = fullContent.ToString();
        if (ToolCallParser.ContainsToolCalls(completeContent))
        {
            var toolCalls = ToolCallParser.ParseToolCalls(completeContent);
            foreach (var toolCall in toolCalls)
            {
                _logger.LogInformation("检测到工具调用: {ToolName}, 参数: {Parameters}",
                    toolCall.ToolName,
                    string.Join(", ", toolCall.Parameters.Select(p => $"{p.Key}={p.Value}")));

                // 发送工具调用信息
                await onChunk(new StreamContentChunk
                {
                    Type = "tool_call",
                    Content = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        tool_name = toolCall.ToolName,
                        parameters = toolCall.Parameters,
                        raw_content = toolCall.RawContent
                    })
                });
            }
        }

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
