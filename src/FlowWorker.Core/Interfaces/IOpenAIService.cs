using FlowWorker.Shared.DTOs;
using FlowWorker.Shared.Entities;

namespace FlowWorker.Core.Interfaces;

/// <summary>
/// OpenAI 服务接口
/// 支持多种请求格式模式
/// </summary>
public interface IOpenAIService
{
    /// <summary>
    /// 发送消息到 OpenAI API
    /// </summary>
    /// <param name="apiKey">API 密钥</param>
    /// <param name="baseUrl">API 基础 URL</param>
    /// <param name="model">模型名称</param>
    /// <param name="messages">消息列表</param>
    /// <param name="systemPrompt">系统提示词</param>
    /// <param name="temperature">温度</param>
    /// <param name="maxTokens">最大 Token 数</param>
    /// <param name="requestFormat">请求格式模式，默认为 cline 模式</param>
    /// <param name="session">会话信息（可选，用于传递环境信息）</param>
    /// <returns>AI 响应内容</returns>
    Task<string> SendMessageAsync(
        string apiKey,
        string baseUrl,
        string model,
        IEnumerable<Message> messages,
        string? systemPrompt = null,
        decimal? temperature = null,
        int? maxTokens = null,
        string? requestFormat = null,
        Session? session = null);

    /// <summary>
    /// 流式发送消息到 OpenAI API
    /// </summary>
    /// <param name="apiKey">API 密钥</param>
    /// <param name="baseUrl">API 基础 URL</param>
    /// <param name="model">模型名称</param>
    /// <param name="messages">消息列表</param>
    /// <param name="onChunk">每块响应的回调（支持异步）</param>
    /// <param name="systemPrompt">系统提示词</param>
    /// <param name="temperature">温度</param>
    /// <param name="maxTokens">最大 Token 数</param>
    /// <param name="requestFormat">请求格式模式，默认为 cline 模式</param>
    /// <param name="session">会话信息（可选，用于传递环境信息）</param>
    /// <returns>完整的响应内容</returns>
    Task<string> SendMessageStreamAsync(
        string apiKey,
        string baseUrl,
        string model,
        IEnumerable<Message> messages,
        Func<StreamContentChunk, Task> onChunk,
        string? systemPrompt = null,
        decimal? temperature = null,
        int? maxTokens = null,
        string? requestFormat = null,
        Session? session = null);

    /// <summary>
    /// 获取可用模型列表
    /// </summary>
    /// <param name="apiKey">API 密钥</param>
    /// <param name="baseUrl">API 基础 URL</param>
    /// <param name="requestFormat">请求格式模式，默认为 cline 模式</param>
    /// <returns>模型列表</returns>
    Task<IReadOnlyList<string>> GetModelsAsync(string apiKey, string baseUrl, string? requestFormat = null);
}
