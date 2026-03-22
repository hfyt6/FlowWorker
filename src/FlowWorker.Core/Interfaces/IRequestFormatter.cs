using FlowWorker.Shared.Entities;

namespace FlowWorker.Core.Interfaces;

/// <summary>
/// HTTP请求格式化器接口
/// 用于支持不同的AI服务请求格式
/// </summary>
public interface IRequestFormatter
{
    /// <summary>
    /// 格式化器名称
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 格式化器描述
    /// </summary>
    string Description { get; }

    /// <summary>
    /// 构建请求体
    /// </summary>
    /// <param name="model">模型名称</param>
    /// <param name="messages">消息列表</param>
    /// <param name="systemPrompt">系统提示词</param>
    /// <param name="temperature">温度</param>
    /// <param name="maxTokens">最大Token数</param>
    /// <param name="stream">是否流式</param>
    /// <param name="session">会话信息（可选，用于获取环境信息如工作目录）</param>
    /// <returns>请求体对象</returns>
    object BuildRequestBody(
        string model,
        IEnumerable<Message> messages,
        string? systemPrompt = null,
        decimal? temperature = null,
        int? maxTokens = null,
        bool stream = false,
        Session? session = null);

    /// <summary>
    /// 获取请求头
    /// </summary>
    /// <param name="apiKey">API密钥</param>
    /// <returns>请求头字典</returns>
    Dictionary<string, string> GetRequestHeaders(string apiKey);

    /// <summary>
    /// 构建API URL
    /// </summary>
    /// <param name="baseUrl">基础URL</param>
    /// <param name="endpoint">端点路径</param>
    /// <returns>完整的API URL</returns>
    string BuildApiUrl(string baseUrl, string endpoint);

    /// <summary>
    /// 解析响应内容
    /// </summary>
    /// <param name="responseContent">响应内容</param>
    /// <returns>解析后的AI响应文本</returns>
    string ParseResponse(string responseContent);

    /// <summary>
    /// 解析流式响应块
    /// </summary>
    /// <param name="chunkData">流式数据块</param>
    /// <returns>解析后的内容块，如果无内容则返回null</returns>
    string? ParseStreamChunk(string chunkData);
}
