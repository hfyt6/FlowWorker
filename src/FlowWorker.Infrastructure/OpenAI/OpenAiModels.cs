namespace FlowWorker.Infrastructure.OpenAI;

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