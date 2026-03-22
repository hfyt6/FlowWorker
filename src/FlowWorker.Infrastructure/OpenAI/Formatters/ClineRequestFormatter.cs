using System.Text.Json;
using System.Text.Json.Serialization;
using FlowWorker.Core.Interfaces;
using FlowWorker.Shared.Entities;

namespace FlowWorker.Infrastructure.OpenAI.Formatters;

/// <summary>
/// Cline 请求格式化器
/// 实现与 Cline 兼容的 OpenAI API 请求格式
/// </summary>
public class ClineRequestFormatter : IRequestFormatter
{
    public string Name => "cline";
    public string Description => "Cline 模式 - 兼容 Cline 扩展的 OpenAI API 请求格式";

    public object BuildRequestBody(
        string model,
        IEnumerable<Message> messages,
        string? systemPrompt = null,
        decimal? temperature = null,
        int? maxTokens = null,
        bool stream = false,
        Session? session = null)
    {
        var request = new ClineRequest
        {
            Model = model,
            Temperature = temperature ?? 0,
            Stream = stream,
            StreamOptions = stream ? new StreamOptions { IncludeUsage = true } : null
        };

        // 构建系统提示词，包含环境信息
        var fullSystemPrompt = BuildSystemPrompt(systemPrompt, session);
        
        // 添加系统提示
        if (!string.IsNullOrWhiteSpace(fullSystemPrompt))
        {
            request.Messages.Add(new ClineMessage 
            { 
                Role = "system", 
                Content = fullSystemPrompt 
            });
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

            // 用户消息使用JSON数组格式（多模态格式）
            if (message.Role == MessageRole.User)
            {
                var contentItems = new List<ClineContentItem>
                {
                    // 1. Task 部分
                    new ClineContentItem
                    {
                        Type = "text",
                        Text = $"<task>\n{message.Content}\n</task>"
                    },
                    // 2. Task Progress Recommended 部分
                    new ClineContentItem
                    {
                        Type = "text",
                        Text = "\n# task_progress RECOMMENDED\n\nWhen starting a new task, it is recommended to include a todo list using the task_progress parameter.\n\n\n1. Include a todo list using the task_progress parameter in your next tool call\n2. Create a comprehensive checklist of all steps needed\n3. Use markdown format: - [ ] for incomplete, - [x] for complete\n\n**Benefits of creating a todo/task_progress list now:**\n\t- Clear roadmap for implementation\n\t- Progress tracking throughout the task\n\t- Nothing gets forgotten or missed\n\t- Users can see, monitor, and edit the plan\n\n**Example structure:**```\n- [ ] Analyze requirements\n- [ ] Set up necessary files\n- [ ] Implement main functionality\n- [ ] Handle edge cases\n- [ ] Test the implementation\n- [ ] Verify results```\n\nKeeping the task_progress list updated helps track progress and ensures nothing is missed.\n"
                    },
                    // 3. Environment Details 部分
                    new ClineContentItem
                    {
                        Type = "text",
                        Text = BuildEnvironmentDetails(session)
                    }
                };

                request.Messages.Add(new ClineMessage
                {
                    Role = role,
                    Content = contentItems
                });
            }
            else
            {
                request.Messages.Add(new ClineMessage
                {
                    Role = role,
                    Content = message.Content
                });
            }
        }

        return request;
    }

    public Dictionary<string, string> GetRequestHeaders(string apiKey)
    {
        return new Dictionary<string, string>
        {
            { "Authorization", $"Bearer {apiKey}" },
            { "Content-Type", "application/json" },
            { "Accept", "*/*" },
            { "Accept-Encoding", "gzip, deflate" },
            { "Accept-Language", "*" },
            { "Connection", "keep-alive" },
            { "User-Agent", "node-fetch" }
        };
    }

    public string BuildApiUrl(string baseUrl, string endpoint)
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

    public string ParseResponse(string responseContent)
    {
        try
        {
            var response = JsonSerializer.Deserialize<ClineResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            var content = response?.Choices?.FirstOrDefault()?.Message?.Content;
            if (content == null)
                return string.Empty;
            
            // Content 可能是字符串或数组，需要处理
            if (content is string strContent)
                return strContent;
            
            // 如果是数组，尝试解析为内容项列表
            if (content is JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == JsonValueKind.String)
                    return jsonElement.GetString() ?? string.Empty;
                
                if (jsonElement.ValueKind == JsonValueKind.Array)
                {
                    var items = JsonSerializer.Deserialize<List<ClineContentItem>>(jsonElement.GetRawText());
                    if (items != null)
                    {
                        // 拼接所有文本内容
                        return string.Join("", items.Where(i => i.Type == "text" && i.Text != null).Select(i => i.Text));
                    }
                }
            }
            
            return content.ToString() ?? string.Empty;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

    public string? ParseStreamChunk(string chunkData)
    {
        try
        {
            var chunk = JsonSerializer.Deserialize<ClineStreamChunk>(chunkData, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            if (chunk?.Choices != null && chunk.Choices.Count > 0)
            {
                var choice = chunk.Choices[0];
                if (choice.Delta?.Content != null)
                {
                    return choice.Delta.Content;
                }
            }

            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// 构建系统提示词
    /// </summary>
    /// <param name="systemPrompt">原始系统提示词</param>
    /// <param name="session">会话信息</param>
    /// <returns>完整的系统提示词</returns>
    private string BuildSystemPrompt(string? systemPrompt, Session? session)
    {
        if (string.IsNullOrWhiteSpace(systemPrompt))
        {
            return string.Empty;
        }

        var fullSystemPrompt = systemPrompt.Trim();

        // 如果 session 有工作目录设置，替换系统提示词中的硬编码工作目录
        if (session != null && !string.IsNullOrWhiteSpace(session.WorkingDirectory))
        {
            // 将硬编码的 d:\\Test 替换为实际工作目录
            // 需要处理多种转义格式
            var workingDirectory = session.WorkingDirectory;
            
            // 替换 d:\\\\Test（四重转义，JSON 序列化后的格式）- 最先处理，因为最具体
            fullSystemPrompt = fullSystemPrompt.Replace("d:\\\\\\\\Test", workingDirectory);
            
            // 替换 d:\\Test（双重转义，C# 字面量格式）
            fullSystemPrompt = fullSystemPrompt.Replace("d:\\\\Test", workingDirectory);
            
            // 替换 d:\Test（普通格式）
            fullSystemPrompt = fullSystemPrompt.Replace(@"d:\Test", workingDirectory);
            
            // 替换当前工作目录的硬编码（例如 FlowWorker 项目目录）
            fullSystemPrompt = fullSystemPrompt.Replace(@"d:\sources\AIProjects\FlowWorker", workingDirectory);
        }

        return fullSystemPrompt;
    }

    /// <summary>
    /// 构建环境详情部分，包含当前session的工作目录
    /// </summary>
    /// <param name="session">会话信息</param>
    /// <returns>环境详情字符串</returns>
    private string BuildEnvironmentDetails(Session? session)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append("<environment_details>\n");
        sb.Append("# Visual Studio Code Visible Files\n\n");
        sb.Append("# Visual Studio Code Open Tabs\n\n");
        sb.Append("# Current Time\n");
        sb.Append(DateTime.Now.ToString("M/d/yyyy, h:mm:ss tt (\\U\\T\\C+8:00)"));
        sb.Append("\n\n");
        
        // 添加当前session的工作目录
        if (session != null && !string.IsNullOrWhiteSpace(session.WorkingDirectory))
        {
            sb.Append("# Current Working Directory\n");
            sb.Append(session.WorkingDirectory);
            sb.Append("\n\n");
        }
        
        sb.Append("# Current Working Directory Files\n\n");
        sb.Append("# Workspace Configuration\n{}\n\n");
        sb.Append("# Detected CLI Tools\n\n");
        sb.Append("# Context Window Usage\n0 / 128K tokens used (0%)\n\n");
        sb.Append("# Current Mode\nACT MODE\n");
        sb.Append("</environment_details>");
        
        return sb.ToString();
    }
}

/// <summary>
/// Cline 请求
/// </summary>
public class ClineRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("messages")]
    public List<ClineMessage> Messages { get; set; } = new();

    [JsonPropertyName("temperature")]
    public decimal Temperature { get; set; } = 0;

    [JsonPropertyName("stream")]
    public bool Stream { get; set; }

    [JsonPropertyName("stream_options")]
    public StreamOptions? StreamOptions { get; set; }
}

/// <summary>
/// Cline 消息内容项（用于多模态内容）
/// </summary>
public class ClineContentItem
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "text";

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("image_url")]
    public ClineImageUrl? ImageUrl { get; set; }
}

/// <summary>
/// Cline 图片URL
/// </summary>
public class ClineImageUrl
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}

/// <summary>
/// Cline 消息 - 支持字符串或内容数组
/// </summary>
public class ClineMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public object Content { get; set; } = string.Empty;
}

/// <summary>
/// 流式选项
/// </summary>
public class StreamOptions
{
    [JsonPropertyName("include_usage")]
    public bool IncludeUsage { get; set; }
}

/// <summary>
/// Cline 响应
/// </summary>
public class ClineResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("object")]
    public string Object { get; set; } = string.Empty;

    [JsonPropertyName("created")]
    public int Created { get; set; }

    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("choices")]
    public List<ClineChoice> Choices { get; set; } = new();

    [JsonPropertyName("usage")]
    public ClineUsage Usage { get; set; } = new();
}

/// <summary>
/// Cline 选择
/// </summary>
public class ClineChoice
{
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("message")]
    public ClineMessage Message { get; set; } = new();

    [JsonPropertyName("finish_reason")]
    public string FinishReason { get; set; } = string.Empty;
}

/// <summary>
/// Cline 使用量
/// </summary>
public class ClineUsage
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }

    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }

    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }
}

/// <summary>
/// Cline 流式响应块
/// </summary>
public class ClineStreamChunk
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("object")]
    public string Object { get; set; } = string.Empty;

    [JsonPropertyName("created")]
    public int Created { get; set; }

    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("choices")]
    public List<ClineStreamChoice> Choices { get; set; } = new();
}

/// <summary>
/// Cline 流式选择
/// </summary>
public class ClineStreamChoice
{
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("delta")]
    public ClineDelta Delta { get; set; } = new();

    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }
}

/// <summary>
/// Cline 增量
/// </summary>
public class ClineDelta
{
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("role")]
    public string? Role { get; set; }
}
