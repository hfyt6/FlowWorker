using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace FlowWorker.Core.Services;

/// <summary>
/// 工具调用解析器
/// 解析AI返回的XML格式工具调用
/// </summary>
public class ToolCallParser
{
    /// <summary>
    /// 工具调用正则表达式模式
    /// 匹配格式：<tool_name>...</tool_name>
    /// </summary>
    private static readonly Regex ToolCallRegex = new(
        @"<(\w+)>(.*?)</\1>",
        RegexOptions.Singleline | RegexOptions.Compiled);

    /// <summary>
    /// 参数正则表达式模式
    /// 匹配格式：<parameter_name>value</parameter_name>
    /// </summary>
    private static readonly Regex ParameterRegex = new(
        @"<(\w+)>(.*?)</\1>",
        RegexOptions.Singleline | RegexOptions.Compiled);

    /// <summary>
    /// 从AI响应内容中解析工具调用
    /// </summary>
    /// <param name="content">AI响应内容</param>
    /// <returns>工具调用列表</returns>
    public static List<ToolCall> ParseToolCalls(string content)
    {
        var toolCalls = new List<ToolCall>();

        if (string.IsNullOrWhiteSpace(content))
            return toolCalls;

        // 使用正则表达式匹配所有工具调用
        var matches = ToolCallRegex.Matches(content);

        foreach (Match match in matches)
        {
            if (match.Groups.Count >= 3)
            {
                var toolName = match.Groups[1].Value;
                var toolContent = match.Groups[2].Value;

                // 解析参数
                var parameters = ParseParameters(toolContent);

                toolCalls.Add(new ToolCall
                {
                    ToolName = toolName,
                    Parameters = parameters,
                    RawContent = match.Value
                });
            }
        }

        return toolCalls;
    }

    /// <summary>
    /// 解析工具调用的参数
    /// </summary>
    /// <param name="toolContent">工具调用内容</param>
    /// <returns>参数字典</returns>
    private static Dictionary<string, string> ParseParameters(string toolContent)
    {
        var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(toolContent))
            return parameters;

        // 使用正则表达式匹配所有参数
        var matches = ParameterRegex.Matches(toolContent);

        foreach (Match match in matches)
        {
            if (match.Groups.Count >= 3)
            {
                var paramName = match.Groups[1].Value;
                var paramValue = match.Groups[2].Value.Trim();

                // 处理CDATA包裹的内容
                if (paramValue.StartsWith("<![CDATA[", StringComparison.OrdinalIgnoreCase) &&
                    paramValue.EndsWith("]]>", StringComparison.OrdinalIgnoreCase))
                {
                    paramValue = paramValue[9..^3];
                }

                parameters[paramName] = paramValue;
            }
        }

        return parameters;
    }

    /// <summary>
    /// 从内容中移除工具调用标记，返回纯文本内容
    /// </summary>
    /// <param name="content">原始内容</param>
    /// <returns>移除工具调用后的纯文本</returns>
    public static string ExtractTextContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return content;

        // 移除所有工具调用标记
        var result = ToolCallRegex.Replace(content, "").Trim();

        return result;
    }

    /// <summary>
    /// 检查内容是否包含工具调用
    /// </summary>
    /// <param name="content">内容</param>
    /// <returns>是否包含工具调用</returns>
    public static bool ContainsToolCalls(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return false;

        return ToolCallRegex.IsMatch(content);
    }

    /// <summary>
    /// 尝试解析单个工具调用
    /// </summary>
    /// <param name="content">内容</param>
    /// <param name="toolCall">解析出的工具调用</param>
    /// <returns>是否解析成功</returns>
    public static bool TryParseToolCall(string content, out ToolCall? toolCall)
    {
        toolCall = null;

        if (string.IsNullOrWhiteSpace(content))
            return false;

        var toolCalls = ParseToolCalls(content);

        if (toolCalls.Count > 0)
        {
            toolCall = toolCalls[0];
            return true;
        }

        return false;
    }
}

/// <summary>
/// 工具调用模型
/// </summary>
public class ToolCall
{
    /// <summary>
    /// 工具名称
    /// </summary>
    public string ToolName { get; set; } = string.Empty;

    /// <summary>
    /// 工具参数
    /// </summary>
    public Dictionary<string, string> Parameters { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 原始内容
    /// </summary>
    public string RawContent { get; set; } = string.Empty;

    /// <summary>
    /// 获取参数值
    /// </summary>
    /// <param name="paramName">参数名</param>
    /// <returns>参数值，如果不存在返回null</returns>
    public string? GetParameter(string paramName)
    {
        return Parameters.TryGetValue(paramName, out var value) ? value : null;
    }

    /// <summary>
    /// 获取参数值（带默认值）
    /// </summary>
    /// <param name="paramName">参数名</param>
    /// <param name="defaultValue">默认值</param>
    /// <returns>参数值，如果不存在返回默认值</returns>
    public string GetParameter(string paramName, string defaultValue)
    {
        return Parameters.TryGetValue(paramName, out var value) ? value : defaultValue;
    }
}
