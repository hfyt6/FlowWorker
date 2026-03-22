using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using FlowWorker.Core.Interfaces;

namespace FlowWorker.Core.Tools;

/// <summary>
/// 代码操作工具处理器
/// 提供代码重构、格式化、生成、转换等功能
/// </summary>
public class CodeManipulationTool : IToolHandler
{
    public string Name => "CodeManipulation";

    public async Task<ToolResponse> ExecuteAsync(string action, JsonElement parameters)
    {
        return action.ToLowerInvariant() switch
        {
            "refactor_code" => await RefactorCodeAsync(parameters),
            "format_code" => await FormatCodeAsync(parameters),
            "generate_code" => await GenerateCodeAsync(parameters),
            "transform_code" => await TransformCodeAsync(parameters),
            "apply_edit" => await ApplyEditAsync(parameters),
            _ => ToolResponse.Error("UNKNOWN_ACTION", $"未知的操作：{action}")
        };
    }

    private Task<ToolResponse> RefactorCodeAsync(JsonElement parameters)
    {
        try
        {
            var code = parameters.GetProperty("code").GetString();
            var operation = parameters.GetProperty("operation").GetString()?.ToLowerInvariant();
            var target = parameters.GetProperty("target").GetString();
            var newName = parameters.TryGetProperty("new_name", out var newNameProp) 
                ? newNameProp.GetString() 
                : null;
            var language = parameters.TryGetProperty("language", out var langProp) 
                ? langProp.GetString()?.ToLowerInvariant() 
                : "csharp";

            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(operation) || string.IsNullOrWhiteSpace(target))
            {
                return Task.FromResult(ToolResponse.Error("INVALID_PARAMETERS", "代码内容、操作类型和目标不能为空"));
            }

            var refactorEngine = new RefactorEngine(language, code);
            var result = refactorEngine.Execute(operation, target, newName);

            return Task.FromResult(ToolResponse.Success(new
            {
                refactored_code = result.RefactoredCode,
                changes = result.Changes,
                affected_lines = result.AffectedLines
            }));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResponse.Error("EXECUTION_FAILED", $"代码重构失败：{ex.Message}"));
        }
    }

    private Task<ToolResponse> FormatCodeAsync(JsonElement parameters)
    {
        try
        {
            var code = parameters.GetProperty("code").GetString();
            var language = parameters.GetProperty("language").GetString()?.ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(language))
            {
                return Task.FromResult(ToolResponse.Error("INVALID_PARAMETERS", "代码内容和编程语言不能为空"));
            }

            var indentSize = parameters.TryGetProperty("indent_size", out var indentProp) 
                ? indentProp.GetInt32() 
                : 4;

            var formatter = new CodeFormatter(language, indentSize);
            var result = formatter.Format(code);

            return Task.FromResult(ToolResponse.Success(new
            {
                formatted_code = result.FormattedCode,
                changes_made = result.ChangesMade
            }));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResponse.Error("EXECUTION_FAILED", $"代码格式化失败：{ex.Message}"));
        }
    }

    private Task<ToolResponse> GenerateCodeAsync(JsonElement parameters)
    {
        try
        {
            var template = parameters.GetProperty("template").GetString();
            var language = parameters.GetProperty("language").GetString()?.ToLowerInvariant();
            var variables = parameters.TryGetProperty("variables", out var varsProp) 
                ? varsProp 
                : JsonDocument.Parse("{}").RootElement;

            if (string.IsNullOrWhiteSpace(template) || string.IsNullOrWhiteSpace(language))
            {
                return Task.FromResult(ToolResponse.Error("INVALID_PARAMETERS", "模板和编程语言不能为空"));
            }

            var generator = new CodeGenerator(language, template, variables);
            var result = generator.Generate();

            return Task.FromResult(ToolResponse.Success(new
            {
                generated_code = result.GeneratedCode,
                explanation = result.Explanation
            }));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResponse.Error("EXECUTION_FAILED", $"代码生成失败：{ex.Message}"));
        }
    }

    private Task<ToolResponse> TransformCodeAsync(JsonElement parameters)
    {
        try
        {
            var code = parameters.GetProperty("code").GetString();
            var sourceLanguage = parameters.GetProperty("source_language").GetString()?.ToLowerInvariant();
            var targetLanguage = parameters.GetProperty("target_language").GetString()?.ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(sourceLanguage) || string.IsNullOrWhiteSpace(targetLanguage))
            {
                return Task.FromResult(ToolResponse.Error("INVALID_PARAMETERS", "代码内容、源语言和目标语言不能为空"));
            }

            var transformer = new CodeTransformer(sourceLanguage, targetLanguage);
            var result = transformer.Transform(code);

            return Task.FromResult(ToolResponse.Success(new
            {
                transformed_code = result.TransformedCode,
                warnings = result.Warnings
            }));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResponse.Error("EXECUTION_FAILED", $"代码转换失败：{ex.Message}"));
        }
    }

    private Task<ToolResponse> ApplyEditAsync(JsonElement parameters)
    {
        try
        {
            var code = parameters.GetProperty("code").GetString();
            var search = parameters.GetProperty("search").GetString();
            var replace = parameters.GetProperty("replace").GetString();

            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(search))
            {
                return Task.FromResult(ToolResponse.Error("INVALID_PARAMETERS", "代码内容和搜索内容不能为空"));
            }

            var editor = new CodeEditor(code);
            var result = editor.ApplyEdit(search, replace ?? "");

            if (!result.Success && result.MatchCount == 0)
            {
                return Task.FromResult(ToolResponse.Error("NO_MATCH_FOUND", "未找到匹配的代码块"));
            }

            if (!result.Success && result.MatchCount > 1)
            {
                return Task.FromResult(ToolResponse.Error("MULTIPLE_MATCHES", $"找到 {result.MatchCount} 个匹配，请提供更精确的搜索内容"));
            }

            return Task.FromResult(ToolResponse.Success(new
            {
                edited_code = result.EditedCode,
                success = result.Success,
                match_count = result.MatchCount
            }));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResponse.Error("EXECUTION_FAILED", $"应用编辑失败：{ex.Message}"));
        }
    }
}

/// <summary>
/// 重构引擎
/// </summary>
public class RefactorEngine
{
    private readonly string _language;
    private readonly string _code;
    private readonly string[] _lines;

    public RefactorEngine(string language, string code)
    {
        _language = language.ToLowerInvariant();
        _code = code;
        _lines = code.Split('\n');
    }

    public RefactorResult Execute(string operation, string target, string? newName)
    {
        var result = new RefactorResult { OriginalCode = _code };

        switch (operation)
        {
            case "rename":
                result = Rename(target, newName);
                break;
            case "extract_method":
                result = ExtractMethod(target);
                break;
            case "inline_variable":
                result = InlineVariable(target);
                break;
            default:
                throw new ArgumentException($"不支持的重构操作：{operation}");
        }

        return result;
    }

    private RefactorResult Rename(string oldName, string? newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            throw new ArgumentException("重命名操作需要提供新名称");
        }

        var result = new RefactorResult();
        var changes = new List<ChangeInfo>();
        var affectedLines = new List<int>();

        var pattern = $@"\b{Regex.Escape(oldName)}\b";
        var refactoredCode = Regex.Replace(_code, pattern, newName);

        var matches = Regex.Matches(_code, pattern);
        foreach (Match match in matches)
        {
            var lineNumber = GetLineNumber(match.Index);
            changes.Add(new ChangeInfo
            {
                Type = "rename",
                Description = $"将 '{oldName}' 重命名为 '{newName}'",
                LineStart = lineNumber,
                LineEnd = lineNumber
            });
            
            if (!affectedLines.Contains(lineNumber))
            {
                affectedLines.Add(lineNumber);
            }
        }

        result.RefactoredCode = refactoredCode;
        result.Changes = changes;
        result.AffectedLines = affectedLines.OrderBy(l => l).ToList();

        return result;
    }

    private RefactorResult ExtractMethod(string lineRange)
    {
        var result = new RefactorResult();
        var (startLine, endLine) = ParseLineRange(lineRange);
        
        if (startLine < 1 || endLine > _lines.Length || startLine > endLine)
        {
            throw new ArgumentException("无效的行号范围");
        }

        var extractedLines = new List<string>();
        for (int i = startLine - 1; i < endLine && i < _lines.Length; i++)
        {
            extractedLines.Add(_lines[i]);
        }

        var extractedCode = string.Join("\n", extractedLines);
        var methodName = $"ExtractedMethod{Guid.NewGuid().ToString("N").Substring(0, 4)}";
        var newMethod = GenerateMethod(methodName, extractedCode);

        var refactoredLines = _lines.ToList();
        var indent = GetIndentation(_lines[startLine - 1]);
        refactoredLines.RemoveRange(startLine - 1, endLine - startLine + 1);
        refactoredLines.Insert(startLine - 1, $"{indent}{methodName}();");
        refactoredLines.Add("");
        refactoredLines.Add(newMethod);

        result.RefactoredCode = string.Join("\n", refactoredLines);
        result.Changes = new List<ChangeInfo>
        {
            new ChangeInfo
            {
                Type = "extract_method",
                Description = $"提取方法 '{methodName}'",
                LineStart = startLine,
                LineEnd = endLine
            }
        };
        result.AffectedLines = Enumerable.Range(startLine, endLine - startLine + 1).ToList();

        return result;
    }

    private RefactorResult InlineVariable(string variableName)
    {
        var result = new RefactorResult();
        var changes = new List<ChangeInfo>();
        var affectedLines = new List<int>();

        var declarationPattern = $@"\b{Regex.Escape(variableName)}\b\s*=\s*([^;]+);";
        var declarationMatch = Regex.Match(_code, declarationPattern);
        
        if (!declarationMatch.Success)
        {
            throw new ArgumentException($"未找到变量 '{variableName}' 的声明");
        }

        var variableValue = declarationMatch.Groups[1].Value.Trim();
        var refactoredCode = _code;
        
        var usagePattern = $@"\b{Regex.Escape(variableName)}\b";
        var matches = Regex.Matches(refactoredCode, usagePattern);
        
        foreach (Match match in matches.Cast<Match>().OrderByDescending(m => m.Index))
        {
            if (match.Index == declarationMatch.Groups[1].Index) continue;
            
            var lineNumber = GetLineNumber(match.Index);
            refactoredCode = refactoredCode.Substring(0, match.Index) + variableValue + refactoredCode.Substring(match.Index + match.Length);
            
            changes.Add(new ChangeInfo
            {
                Type = "inline_variable",
                Description = $"内联变量 '{variableName}'",
                LineStart = lineNumber,
                LineEnd = lineNumber
            });
            
            if (!affectedLines.Contains(lineNumber))
            {
                affectedLines.Add(lineNumber);
            }
        }

        result.RefactoredCode = refactoredCode;
        result.Changes = changes;
        result.AffectedLines = affectedLines.OrderBy(l => l).ToList();

        return result;
    }

    private (int start, int end) ParseLineRange(string range)
    {
        var parts = range.Split('-');
        if (parts.Length == 2 && int.TryParse(parts[0], out int start) && int.TryParse(parts[1], out int end))
        {
            return (start, end);
        }
        throw new ArgumentException("行号范围格式无效，请使用 'start-end' 格式");
    }

    private int GetLineNumber(int index)
    {
        int line = 1;
        for (int i = 0; i < index && i < _code.Length; i++)
        {
            if (_code[i] == '\n') line++;
        }
        return line;
    }

    private string GetIndentation(string line)
    {
        var match = Regex.Match(line, @"^(\s*)");
        return match.Success ? match.Groups[1].Value : "";
    }

    private string GenerateMethod(string methodName, string body)
    {
        return $@"private void {methodName}()
{{
{body}
}}";
    }
}

/// <summary>
/// 代码格式化器
/// </summary>
public class CodeFormatter
{
    private readonly string _language;
    private readonly int _indentSize;

    public CodeFormatter(string language, int indentSize)
    {
        _language = language.ToLowerInvariant();
        _indentSize = indentSize;
    }

    public FormatResult Format(string code)
    {
        var result = new FormatResult();
        var formattedCode = code;

        formattedCode = FormatByLanguage(formattedCode);
        formattedCode = FormatCommon(formattedCode);

        if (!formattedCode.EndsWith("\n"))
        {
            formattedCode += "\n";
        }

        result.FormattedCode = formattedCode;
        result.ChangesMade = code != formattedCode;

        return result;
    }

    private string FormatByLanguage(string code)
    {
        return _language switch
        {
            "csharp" or "java" or "cpp" or "c" => FormatCStyle(code),
            "javascript" or "typescript" => FormatJavaScript(code),
            "python" => FormatPython(code),
            _ => code
        };
    }

    private string FormatCStyle(string code)
    {
        var lines = code.Split('\n');
        var result = new List<string>();
        var currentIndent = 0;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            
            if (trimmed.StartsWith("}") || trimmed.StartsWith("]"))
            {
                currentIndent = Math.Max(0, currentIndent - 1);
            }

            if (!string.IsNullOrWhiteSpace(trimmed))
            {
                result.Add(new string(' ', currentIndent * _indentSize) + trimmed);
            }
            else
            {
                result.Add("");
            }

            if (trimmed.EndsWith("{") || trimmed.EndsWith("["))
            {
                currentIndent++;
            }
        }

        return string.Join("\n", result);
    }

    private string FormatJavaScript(string code)
    {
        return FormatCStyle(code);
    }

    private string FormatPython(string code)
    {
        var lines = code.Split('\n');
        var result = new List<string>();
        var currentIndent = 0;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            
            if (!string.IsNullOrWhiteSpace(trimmed))
            {
                if (trimmed.EndsWith(":"))
                {
                    result.Add(new string(' ', currentIndent * _indentSize) + trimmed);
                    currentIndent++;
                }
                else
                {
                    if (trimmed.StartsWith("return") || trimmed.StartsWith("break") || trimmed.StartsWith("continue"))
                    {
                        currentIndent = Math.Max(0, currentIndent - 1);
                    }
                    result.Add(new string(' ', currentIndent * _indentSize) + trimmed);
                }
            }
            else
            {
                result.Add("");
            }
        }

        return string.Join("\n", result);
    }

    private string FormatCommon(string code)
    {
        code = Regex.Replace(code, @"[ \t]+$", "", RegexOptions.Multiline);
        code = code.Replace("\r\n", "\n").Replace("\r", "\n");
        code = Regex.Replace(code, @"  +", " ");
        
        return code;
    }
}

/// <summary>
/// 代码生成器
/// </summary>
public class CodeGenerator
{
    private readonly string _language;
    private readonly string _template;
    private readonly JsonElement _variables;

    public CodeGenerator(string language, string template, JsonElement variables)
    {
        _language = language.ToLowerInvariant();
        _template = template;
        _variables = variables;
    }

    public GenerationResult Generate()
    {
        var result = new GenerationResult();
        var generatedCode = ReplaceTemplateVariables(_template);

        if (_template.ToLower().Contains("class"))
        {
            generatedCode = GenerateClassTemplate(generatedCode);
            result.Explanation = "生成了一个类定义，包含基本的属性和方法结构";
        }
        else if (_template.ToLower().Contains("function") || _template.ToLower().Contains("method"))
        {
            generatedCode = GenerateFunctionTemplate(generatedCode);
            result.Explanation = "生成了一个函数/方法定义，包含参数和返回值";
        }
        else
        {
            result.Explanation = "根据模板生成了代码";
        }

        result.GeneratedCode = generatedCode;
        return result;
    }

    private string ReplaceTemplateVariables(string template)
    {
        var result = template;
        
        foreach (var prop in _variables.EnumerateObject())
        {
            var placeholder = $"{{{prop.Name}}}";
            var value = prop.Value.ToString();
            result = result.Replace(placeholder, value);
        }
        
        return result;
    }

    private string GenerateClassTemplate(string template)
    {
        return _language switch
        {
            "csharp" => $@"public class {template}
{{
    public string Name {{ get; set; }}
    
    public {template}()
    {{
    }}
    
    public void Execute()
    {{
        // TODO: 实现逻辑
    }}
}}",
            "javascript" or "typescript" => $@"class {template} {{
    constructor() {{
        // 初始化
    }}
    
    execute() {{
        // TODO: 实现逻辑
    }}
}}",
            "python" => $@"class {template}:
    def __init__(self):
        # 初始化
        pass
    
    def execute(self):
        # TODO: 实现逻辑
        pass",
            _ => template
        };
    }

    private string GenerateFunctionTemplate(string template)
    {
        return _language switch
        {
            "csharp" => $@"public void {template}()
{{
    // TODO: 实现逻辑
}}",
            "javascript" or "typescript" => $@"function {template}() {{
    // TODO: 实现逻辑
}}",
            "python" => $@"def {template}():
    # TODO: 实现逻辑
    pass",
            _ => template
        };
    }
}

/// <summary>
/// 代码转换器
/// </summary>
public class CodeTransformer
{
    private readonly string _sourceLanguage;
    private readonly string _targetLanguage;

    public CodeTransformer(string sourceLanguage, string targetLanguage)
    {
        _sourceLanguage = sourceLanguage.ToLowerInvariant();
        _targetLanguage = targetLanguage.ToLowerInvariant();
    }

    public TransformationResult Transform(string code)
    {
        var result = new TransformationResult();
        var transformedCode = code;
        var notes = new List<string>();
        var warnings = new List<string>();

        var conversionKey = $"{_sourceLanguage}_to_{_targetLanguage}";
        
        transformedCode = conversionKey switch
        {
            "javascript_to_typescript" => ConvertJavaScriptToTypeScript(code, notes, warnings),
            "java_to_csharp" => ConvertJavaToCSharp(code, notes, warnings),
            "csharp_to_java" => ConvertCSharpToJava(code, notes, warnings),
            _ => code
        };

        if (transformedCode == code && conversionKey != "unknown_to_unknown")
        {
            warnings.Add($"暂不支持从 {_sourceLanguage} 到 {_targetLanguage} 的转换");
        }

        result.TransformedCode = transformedCode;
        result.ConversionNotes = notes;
        result.Warnings = warnings;

        return result;
    }

    private string ConvertJavaScriptToTypeScript(string code, List<string> notes, List<string> warnings)
    {
        notes.Add("添加类型注解");
        code = Regex.Replace(code, @"function\s+(\w+)\s*\(([^)]*)\)", m =>
        {
            var funcName = m.Groups[1].Value;
            var parameters = m.Groups[2].Value;
            return $"function {funcName}({parameters}): any";
        });
        code = Regex.Replace(code, @"\bvar\b", "const");
        return code;
    }

    private string ConvertJavaToCSharp(string code, List<string> notes, List<string> warnings)
    {
        notes.Add("转换 Java 语法为 C# 语法");
        code = Regex.Replace(code, @"\bString\b", "string");
        code = Regex.Replace(code, @"System\.out\.println\s*\(([^)]+)\)", "Console.WriteLine($1);");
        code = Regex.Replace(code, @"\bvar\b", "Object");
        return code;
    }

    private string ConvertCSharpToJava(string code, List<string> notes, List<string> warnings)
    {
        notes.Add("转换 C# 语法为 Java 语法");
        code = Regex.Replace(code, @"\bstring\b", "String");
        code = Regex.Replace(code, @"Console\.WriteLine\s*\(([^)]+)\)", "System.out.println($1);");
        return code;
    }
}

/// <summary>
/// 代码编辑器
/// </summary>
public class CodeEditor
{
    private readonly string _code;

    public CodeEditor(string code)
    {
        _code = code;
    }

    public EditResult ApplyEdit(string search, string replace)
    {
        var result = new EditResult();
        
        var normalizedSearch = NormalizeWhitespace(search);
        var normalizedCode = NormalizeWhitespace(_code);
        
        var matches = Regex.Matches(normalizedCode, Regex.Escape(normalizedSearch));
        result.MatchCount = matches.Count;

        if (matches.Count == 0)
        {
            result.Success = false;
            result.EditedCode = _code;
            return result;
        }

        if (matches.Count > 1)
        {
            result.Success = false;
            result.EditedCode = _code;
            return result;
        }

        var editedCode = normalizedCode.Substring(0, matches[0].Index) + replace + normalizedCode.Substring(matches[0].Index + matches[0].Length);
        
        result.Success = true;
        result.EditedCode = editedCode;

        return result;
    }

    private string NormalizeWhitespace(string text)
    {
        var lines = text.Split('\n');
        var normalizedLines = lines.Select(l => l.TrimEnd()).Where(l => !string.IsNullOrWhiteSpace(l));
        return string.Join("\n", normalizedLines);
    }
}

/// <summary>
/// 结果类
/// </summary>
public class RefactorResult
{
    public string OriginalCode { get; set; } = string.Empty;
    public string RefactoredCode { get; set; } = string.Empty;
    public List<ChangeInfo> Changes { get; set; } = new();
    public List<int> AffectedLines { get; set; } = new();
}

public class ChangeInfo
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int LineStart { get; set; }
    public int LineEnd { get; set; }
}

public class FormatResult
{
    public string FormattedCode { get; set; } = string.Empty;
    public bool ChangesMade { get; set; }
}

public class GenerationResult
{
    public string GeneratedCode { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
}

public class TransformationResult
{
    public string TransformedCode { get; set; } = string.Empty;
    public List<string> ConversionNotes { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

public class EditResult
{
    public string EditedCode { get; set; } = string.Empty;
    public bool Success { get; set; }
    public int MatchCount { get; set; }
}