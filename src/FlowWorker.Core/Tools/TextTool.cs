using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using FlowWorker.Core.Interfaces;

namespace FlowWorker.Core.Tools;

/// <summary>
/// 文本处理工具处理器
/// 提供文本搜索、替换、统计、JSON 格式化等功能
/// </summary>
public class TextTool : IToolHandler
{
    public string Name => "Text";

    public async Task<ToolResponse> ExecuteAsync(string action, JsonElement parameters)
    {
        return action.ToLowerInvariant() switch
        {
            "search_text" => await SearchTextAsync(parameters),
            "replace_text" => await ReplaceTextAsync(parameters),
            "count_lines" => await CountLinesAsync(parameters),
            "format_json" => await FormatJsonAsync(parameters),
            "validate_json" => await ValidateJsonAsync(parameters),
            _ => ToolResponse.Error("UNKNOWN_ACTION", $"未知的操作：{action}")
        };
    }

    /// <summary>
    /// 文本搜索，支持正则表达式
    /// </summary>
    private Task<ToolResponse> SearchTextAsync(JsonElement parameters)
    {
        try
        {
            var text = parameters.GetProperty("text").GetString();
            var pattern = parameters.GetProperty("pattern").GetString();

            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(pattern))
            {
                return Task.FromResult(ToolResponse.Error("INVALID_PARAMETERS", "文本和搜索模式不能为空"));
            }

            var caseSensitive = parameters.TryGetProperty("case_sensitive", out var caseProp) 
                ? caseProp.GetBoolean() 
                : true;

            var maxResults = parameters.TryGetProperty("max_results", out var maxProp) 
                ? maxProp.GetInt32() 
                : 100;

            var regexOptions = caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
            var matches = new List<object>();

            try
            {
                var regex = new Regex(pattern, regexOptions);
                var matchCollection = regex.Matches(text);

                var lines = text.Split('\n');
                var lineStartIndices = new List<int> { 0 };
                var currentIndex = 0;
                foreach (var line in lines)
                {
                    currentIndex += line.Length + 1;
                    lineStartIndices.Add(currentIndex);
                }

                int count = 0;
                foreach (Match match in matchCollection)
                {
                    if (count >= maxResults) break;

                    int lineNumber = 1;
                    int column = match.Index + 1;
                    for (int i = 0; i < lineStartIndices.Count - 1; i++)
                    {
                        if (match.Index < lineStartIndices[i + 1])
                        {
                            lineNumber = i + 1;
                            column = match.Index - lineStartIndices[i] + 1;
                            break;
                        }
                    }

                    var matchInfo = new
                    {
                        index = match.Index,
                        length = match.Length,
                        value = match.Value,
                        line_number = lineNumber,
                        column = column,
                        groups = match.Groups.Cast<Group>().Select((g, i) => new
                        {
                            index = g.Index,
                            length = g.Length,
                            value = g.Value,
                            name = regex.GroupNameFromNumber(i)
                        }).ToList()
                    };

                    matches.Add(matchInfo);
                    count++;
                }

                return Task.FromResult(ToolResponse.Success(new
                {
                    matches = matches,
                    total_count = matchCollection.Count
                }));
            }
            catch (RegexParseException ex)
            {
                return Task.FromResult(ToolResponse.Error("INVALID_PATTERN", $"正则表达式错误：{ex.Message}"));
            }
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResponse.Error("EXECUTION_FAILED", ex.Message));
        }
    }

    /// <summary>
    /// 文本替换，支持正则表达式
    /// </summary>
    private Task<ToolResponse> ReplaceTextAsync(JsonElement parameters)
    {
        try
        {
            var text = parameters.GetProperty("text").GetString();
            var pattern = parameters.GetProperty("pattern").GetString();
            var replacement = parameters.GetProperty("replacement").GetString();

            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(pattern))
            {
                return Task.FromResult(ToolResponse.Error("INVALID_PARAMETERS", "文本和替换模式不能为空"));
            }

            var isRegex = parameters.TryGetProperty("is_regex", out var regexProp) 
                ? regexProp.GetBoolean() 
                : false;

            var replaceAll = parameters.TryGetProperty("replace_all", out var allProp) 
                ? allProp.GetBoolean() 
                : true;

            string result;
            int replacementCount = 0;

            if (isRegex)
            {
                try
                {
                    var regex = new Regex(pattern);
                    if (replaceAll)
                    {
                        result = regex.Replace(text, replacement);
                        replacementCount = regex.Matches(text).Count;
                    }
                    else
                    {
                        result = regex.Replace(text, replacement, 1);
                        replacementCount = Math.Min(1, regex.Matches(text).Count);
                    }
                }
                catch (RegexParseException ex)
                {
                    return Task.FromResult(ToolResponse.Error("INVALID_PATTERN", $"正则表达式错误：{ex.Message}"));
                }
            }
            else
            {
                if (replaceAll)
                {
                    result = text.Replace(pattern, replacement ?? string.Empty);
                    replacementCount = (text.Length - text.Replace(pattern, string.Empty).Length) / pattern.Length;
                }
                else
                {
                    int index = text.IndexOf(pattern);
                    if (index >= 0)
                    {
                        result = text.Substring(0, index) + replacement + text.Substring(index + pattern.Length);
                        replacementCount = 1;
                    }
                    else
                    {
                        result = text;
                        replacementCount = 0;
                    }
                }
            }

            return Task.FromResult(ToolResponse.Success(new
            {
                result = result,
                replacements_count = replacementCount
            }));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResponse.Error("EXECUTION_FAILED", ex.Message));
        }
    }

    /// <summary>
    /// 统计文本行数
    /// </summary>
    private Task<ToolResponse> CountLinesAsync(JsonElement parameters)
    {
        try
        {
            var text = parameters.GetProperty("text").GetString();

            if (text == null)
            {
                return Task.FromResult(ToolResponse.Error("INVALID_PARAMETERS", "文本不能为空"));
            }

            var includeEmpty = parameters.TryGetProperty("include_empty", out var emptyProp) 
                ? emptyProp.GetBoolean() 
                : true;

            var lines = text.Split(new[] { '\n' }, StringSplitOptions.None);
            var totalLines = lines.Length;
            var nonEmptyLines = lines.Count(l => !string.IsNullOrWhiteSpace(l));
            var emptyLines = totalLines - nonEmptyLines;

            var characterCount = text.Length;
            var wordCount = text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;

            return Task.FromResult(ToolResponse.Success(new
            {
                total_lines = totalLines,
                non_empty_lines = nonEmptyLines,
                empty_lines = emptyLines,
                character_count = characterCount,
                word_count = wordCount
            }));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResponse.Error("EXECUTION_FAILED", ex.Message));
        }
    }

    /// <summary>
    /// JSON 格式化
    /// </summary>
    private Task<ToolResponse> FormatJsonAsync(JsonElement parameters)
    {
        try
        {
            var jsonString = parameters.GetProperty("json_string").GetString();

            if (string.IsNullOrWhiteSpace(jsonString))
            {
                return Task.FromResult(ToolResponse.Error("INVALID_PARAMETERS", "JSON 字符串不能为空"));
            }

            var indentSize = parameters.TryGetProperty("indent_size", out var indentProp) 
                ? indentProp.GetInt32() 
                : 2;

            var sortKeys = parameters.TryGetProperty("sort_keys", out var sortProp) 
                ? sortProp.GetBoolean() 
                : false;

            try
            {
                using var doc = JsonDocument.Parse(jsonString);
                
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = null
                };

                if (sortKeys)
                {
                    var sorted = SortJsonElement(doc.RootElement);
                    var formatted = JsonSerializer.Serialize(sorted, options);
                    formatted = AdjustIndentation(formatted, indentSize);
                    return Task.FromResult(ToolResponse.Success(new
                    {
                        formatted = formatted,
                        is_valid = true
                    }));
                }
                else
                {
                    var formatted = JsonSerializer.Serialize(doc.RootElement, options);
                    formatted = AdjustIndentation(formatted, indentSize);
                    return Task.FromResult(ToolResponse.Success(new
                    {
                        formatted = formatted,
                        is_valid = true
                    }));
                }
            }
            catch (JsonException ex)
            {
                return Task.FromResult(ToolResponse.Error("INVALID_JSON", $"JSON 解析错误：{ex.Message}"));
            }
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResponse.Error("EXECUTION_FAILED", ex.Message));
        }
    }

    /// <summary>
    /// JSON 验证
    /// </summary>
    private Task<ToolResponse> ValidateJsonAsync(JsonElement parameters)
    {
        try
        {
            var jsonString = parameters.GetProperty("json_string").GetString();

            if (string.IsNullOrWhiteSpace(jsonString))
            {
                return Task.FromResult(ToolResponse.Success(new
                {
                    is_valid = false,
                    error = "JSON 字符串为空",
                    error_position = 0,
                    error_line = 1,
                    error_column = 1
                }));
            }

            try
            {
                using var doc = JsonDocument.Parse(jsonString);
                return Task.FromResult(ToolResponse.Success(new
                {
                    is_valid = true,
                    error = (string?)null,
                    error_position = (int?)null,
                    error_line = (int?)null,
                    error_column = (int?)null
                }));
            }
            catch (JsonException ex)
            {
                var (line, column) = CalculateErrorPosition(jsonString, ex.BytePositionInLine ?? 0);

                return Task.FromResult(ToolResponse.Success(new
                {
                    is_valid = false,
                    error = ex.Message,
                    error_position = (int?)(ex.BytePositionInLine ?? 0),
                    error_line = line,
                    error_column = column
                }));
            }
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResponse.Error("EXECUTION_FAILED", ex.Message));
        }
    }

    /// <summary>
    /// 调整 JSON 缩进
    /// </summary>
    private string AdjustIndentation(string json, int indentSize)
    {
        if (indentSize == 2) return json;

        var lines = json.Split('\n');
        var sb = new StringBuilder();
        var indent = new string(' ', indentSize);

        foreach (var line in lines)
        {
            var trimmed = line;
            var currentIndent = 0;
            while (trimmed.StartsWith("  "))
            {
                trimmed = trimmed.Substring(2);
                currentIndent++;
            }
            sb.AppendLine(new string(' ', currentIndent * indentSize) + trimmed);
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// 对 JSON 元素进行排序
    /// </summary>
    private object SortJsonElement(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var dict = new SortedDictionary<string, object>();
                foreach (var property in element.EnumerateObject().OrderBy(p => p.Name))
                {
                    dict[property.Name] = SortJsonElement(property.Value);
                }
                return dict;

            case JsonValueKind.Array:
                return element.EnumerateArray().Select(SortJsonElement).ToList();

            case JsonValueKind.String:
                return element.GetString();

            case JsonValueKind.Number:
                if (element.TryGetInt64(out var longValue))
                    return longValue;
                if (element.TryGetDouble(out var doubleValue))
                    return doubleValue;
                return element.GetRawText();

            case JsonValueKind.True:
                return true;

            case JsonValueKind.False:
                return false;

            case JsonValueKind.Null:
                return null;

            default:
                return element.GetRawText();
        }
    }

    /// <summary>
    /// 计算错误位置
    /// </summary>
    private (int line, int column) CalculateErrorPosition(string text, long bytePosition)
    {
        int line = 1;
        int column = 1;

        for (int i = 0; i < bytePosition && i < text.Length; i++)
        {
            if (text[i] == '\n')
            {
                line++;
                column = 1;
            }
            else
            {
                column++;
            }
        }

        return (line, column);
    }
}