using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FlowWorker.Core.Interfaces;
using FlowWorker.Shared.DTOs;
using FlowWorker.Shared.Entities;

namespace FlowWorker.Infrastructure.OpenAI;

/// <summary>
/// OpenAI 服务实现
/// </summary>
public class OpenAIService : IOpenAIService
{
    private readonly HttpClient _httpClient;

    public OpenAIService(HttpClient httpClient)
    {
        _httpClient = httpClient;
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

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/chat/completions")
        {
            Content = JsonContent.Create(request, options: new JsonSerializerOptions { PropertyNamingPolicy = null })
        };

        httpRequest.Headers.Add("Authorization", $"Bearer {apiKey}");
        httpRequest.Headers.Add("Content-Type", "application/json");

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
        Action<StreamContentChunk> onChunk,
        string? systemPrompt = null,
        decimal? temperature = null,
        int? maxTokens = null)
    {
        var request = BuildRequest(messages, systemPrompt, model, temperature, maxTokens);
        request.Stream = true;

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/chat/completions")
        {
            Content = JsonContent.Create(request, options: new JsonSerializerOptions { PropertyNamingPolicy = null })
        };

        httpRequest.Headers.Add("Authorization", $"Bearer {apiKey}");
        httpRequest.Headers.Add("Content-Type", "application/json");

        using var response = await _httpClient.SendAsync(httpRequest);
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
                    var chunk = JsonSerializer.Deserialize<OpenAiStreamChunk>(data, new JsonSerializerOptions { PropertyNamingPolicy = null });
                    
                    if (chunk?.Choices?.FirstOrDefault()?.Delta?.Content != null)
                    {
                        fullContent.Append(chunk.Choices[0].Delta.Content);
                        onChunk(new StreamContentChunk
                        {
                            Type = "content",
                            Content = chunk.Choices[0].Delta.Content
                        });
                    }
                }
                catch
                {
                    // 忽略解析错误
                }
            }
        }

        return fullContent.ToString();
    }

    public async Task<IReadOnlyList<string>> GetModelsAsync(string apiKey, string baseUrl)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl.TrimEnd('/')}/models");
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