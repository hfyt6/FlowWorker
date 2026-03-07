using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FlowWorker.Core.Interfaces;
using FlowWorker.Shared.DTOs;
using FlowWorker.Shared.Entities;
using Microsoft.Extensions.Logging;

namespace FlowWorker.Infrastructure.OpenAI;

/// <summary>
/// OpenAI 服务实现
/// </summary>
public class OpenAIService : IOpenAIService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenAIService> _logger;

    public OpenAIService(HttpClient httpClient, ILogger<OpenAIService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<string> SendMessageAsync(
        string apiKey,
        string baseUrl,
        string model,
        IEnumerable<Message> messages,
        string? systemPrompt = null,
        decimal? temperature = null,
        int? maxTokens = null)
    {
        var request = BuildRequest(messages, systemPrompt, model, temperature, maxTokens);

        // 构建完整的 API URL
        var apiUrl = BuildApiUrl(baseUrl, "/chat/completions");

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, apiUrl)
        {
            Content = JsonContent.Create(request, options: new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower })
        };

        httpRequest.Headers.Add("Authorization", $"Bearer {apiKey}");

        using var response = await _httpClient.SendAsync(httpRequest);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OpenAiResponse>();
        return result?.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty;
    }

    public async Task<string> SendMessageStreamAsync(
        string apiKey,
        string baseUrl,
        string model,
        IEnumerable<Message> messages,
        Func<StreamContentChunk, Task> onChunk,
        string? systemPrompt = null,
        decimal? temperature = null,
        int? maxTokens = null)
    {
        var request = BuildRequest(messages, systemPrompt, model, temperature, maxTokens);
        request.Stream = true;

        // 构建完整的 API URL
        var apiUrl = BuildApiUrl(baseUrl, "/chat/completions");

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, apiUrl)
        {
            Content = JsonContent.Create(request, options: new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower })
        };

        httpRequest.Headers.Add("Authorization", $"Bearer {apiKey}");

        using var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead);
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
                    break;
                }

                try
                {
                    var chunk = JsonSerializer.Deserialize<OpenAiStreamChunk>(data, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });
                    
                    if (chunk?.Choices != null && chunk.Choices.Count > 0)
                    {
                        var choice = chunk.Choices[0];
                        if (choice.Delta?.Content != null)
                        {
                            var content = choice.Delta.Content;
                            fullContent.Append(content);
                            
                            await onChunk(new StreamContentChunk
                            {
                                Type = "content",
                                Content = content
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("[OpenAI Stream] 解析 chunk 失败: {Error}", ex.Message);
                }
            }
        }

        return fullContent.ToString();
    }

    public async Task<IReadOnlyList<string>> GetModelsAsync(string apiKey, string baseUrl)
    {
        // 构建完整的 API URL
        var apiUrl = BuildApiUrl(baseUrl, "/models");

        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, apiUrl);
        httpRequest.Headers.Add("Authorization", $"Bearer {apiKey}");

        using var response = await _httpClient.SendAsync(httpRequest);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OpenAiModelsResponse>();
        return result?.Data?.Select(m => m.Id).ToList() ?? new List<string>();
    }

    private OpenAiRequest BuildRequest(
        IEnumerable<Message> messages,
        string? systemPrompt,
        string model,
        decimal? temperature,
        int? maxTokens)
    {
        var request = new OpenAiRequest
        {
            Model = model,
            Temperature = temperature ?? 0.7m,
            MaxTokens = maxTokens
        };

        // 添加系统提示
        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            request.Messages.Add(new OpenAiMessage { Role = "system", Content = systemPrompt });
        }

        // 添加对话消息
        foreach (var message in messages)
        {
            var role = message.Role switch
            {
                MessageRole.System => "system",
                MessageRole.User => "user",
                MessageRole.Assistant => "assistant",
                MessageRole.Tool => "tool",
                _ => "user"
            };

            request.Messages.Add(new OpenAiMessage
            {
                Role = role,
                Content = message.Content
            });
        }

        return request;
    }

    /// <summary>
    /// 构建 API URL，处理 baseUrl 可能已包含路径的情况
    /// </summary>
    private string BuildApiUrl(string baseUrl, string endpoint)
    {
        var trimmedBaseUrl = baseUrl.TrimEnd('/');
        
        // 如果 baseUrl 已经包含完整的 endpoint 路径，直接返回
        if (trimmedBaseUrl.EndsWith(endpoint))
        {
            return trimmedBaseUrl;
        }
        
        // 如果 baseUrl 已经包含 /v1，则只添加 endpoint
        if (trimmedBaseUrl.Contains("/v1"))
        {
            return $"{trimmedBaseUrl}{endpoint}";
        }
        
        // 否则添加 /v1 + endpoint
        return $"{trimmedBaseUrl}/v1{endpoint}";
    }
}

/// <summary>
/// OpenAI 请求
/// </summary>
public class OpenAiRequest
{
    public string Model { get; set; } = string.Empty;
    public List<OpenAiMessage> Messages { get; set; } = new();
    public decimal Temperature { get; set; } = 0.7m;
    public int? MaxTokens { get; set; }
    public bool Stream { get; set; }
}

/// <summary>
/// OpenAI 消息
/// </summary>
public class OpenAiMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// OpenAI 响应
/// </summary>
public class OpenAiResponse
{
    public string Id { get; set; } = string.Empty;
    public string Object { get; set; } = string.Empty;
    public int Created { get; set; }
    public string Model { get; set; } = string.Empty;
    public List<OpenAiChoice> Choices { get; set; } = new();
    public OpenAiUsage Usage { get; set; } = new();
}

/// <summary>
/// OpenAI 选择
/// </summary>
public class OpenAiChoice
{
    public int Index { get; set; }
    public OpenAiMessage Message { get; set; } = new();
    public string FinishReason { get; set; } = string.Empty;
}

/// <summary>
/// OpenAI 使用量
/// </summary>
public class OpenAiUsage
{
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
}

/// <summary>
/// OpenAI 流式响应块
/// </summary>
public class OpenAiStreamChunk
{
    public string Id { get; set; } = string.Empty;
    public string Object { get; set; } = string.Empty;
    public int Created { get; set; }
    public string Model { get; set; } = string.Empty;
    public List<OpenAiStreamChoice> Choices { get; set; } = new();
}

/// <summary>
/// OpenAI 流式选择
/// </summary>
public class OpenAiStreamChoice
{
    public int Index { get; set; }
    public OpenAiDelta Delta { get; set; } = new();
    public string? FinishReason { get; set; }
}

/// <summary>
/// OpenAI 增量
/// </summary>
public class OpenAiDelta
{
    public string? Content { get; set; }
    public string? Role { get; set; }
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
