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
public partial class OpenAIService : IOpenAIService
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