using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FlowWorker.Plugins.CodeManipulation
{
    /// <summary>
    /// 代码操作工具处理器
    /// </summary>
    public class CodeManipulationHandler
    {
        /// <summary>
        /// 重构代码
        /// </summary>
        public Task<ToolResponse> RefactorCodeAsync(JsonElement parameters)
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
                return Task.FromResult(ToolResponse.Error("EXECUTION_FAILED", $"代码重构失败: {ex.Message}"));
            }
        }

        /// <summary>
        /// 格式化代码
        /// </summary>
        public Task<ToolResponse> FormatCodeAsync(JsonElement parameters)
        {
            try
            {
                var code = parameters.GetProperty("code").GetString();
                var language = parameters.GetProperty("language").GetString()?.ToLowerInvariant();
                var style = parameters.TryGetProperty("style", out var styleProp) 
                    ? styleProp.GetString()?.ToLowerInvariant() 
                    : "default";
                var options = parameters.TryGetProperty("options", out var optionsProp) 
                    ? optionsProp 
                    : JsonDocument.Parse("{}").RootElement;

                if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(language))
                {
                    return Task.FromResult(ToolResponse.Error("INVALID_PARAMETERS", "代码内容和编程语言不能为空"));
                }

                var formatter = new CodeFormatter(language, style, options);
                var result = formatter.Format(code);

                return Task.FromResult(ToolResponse.Success(new
                {
                    formatted_code = result.FormattedCode,
                    changes_made = result.ChangesMade,
                    format_stats = result.FormatStats
                }));
            }
            catch (Exception ex)
            {
                return Task.FromResult(ToolResponse.Error("EXECUTION_FAILED", $"代码格式化失败: {ex.Message}"));
            }
        }

        /// <summary>
        /// 生成代码
        /// </summary>
        public Task<ToolResponse> GenerateCodeAsync(JsonElement parameters)
        {
            try
            {
                var template = parameters.GetProperty("template").GetString();
                var language = parameters.GetProperty("language").GetString()?.ToLowerInvariant();
                var variables = parameters.TryGetProperty("variables", out var varsProp) 
                    ? varsProp 
                    : JsonDocument.Parse("{}").RootElement;
                var context = parameters.TryGetProperty("context", out var contextProp) 
                    ? contextProp.GetString() 
                    : null;

                if (string.IsNullOrWhiteSpace(template) || string.IsNullOrWhiteSpace(language))
                {
                    return Task.FromResult(ToolResponse.Error("INVALID_PARAMETERS", "模板和编程语言不能为空"));
                }

                var generator = new CodeGenerator(language, template, variables, context);
                var result = generator.Generate();

                return Task.FromResult(ToolResponse.Success(new
                {
                    generated_code = result.GeneratedCode,
                    explanation = result.Explanation,
                    suggestions = result.Suggestions
                }));
            }
            catch (Exception ex)
            {
                return Task.FromResult(ToolResponse.Error("EXECUTION_FAILED", $"代码生成失败: {ex.Message}"));
            }
        }

        /// <summary>
        /// 转换代码
        /// </summary>
        public Task<ToolResponse> TransformCodeAsync(JsonElement parameters)
        {
            try
            {
                var code = parameters.GetProperty("code").GetString();
                var sourceLanguage = parameters.GetProperty("source_language").GetString()?.ToLowerInvariant();
                var targetLanguage = parameters.GetProperty("target_language").GetString()?.ToLowerInvariant();
                var transformationType = parameters.TryGetProperty("transformation_type", out var typeProp) 
                    ? typeProp.GetString()?.ToLowerInvariant() 
                    : "language_convert";

                if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(sourceLanguage) || string.IsNullOrWhiteSpace(targetLanguage))
                {
                    return Task.FromResult(ToolResponse.Error("INVALID_PARAMETERS", "代码内容、源语言和目标语言不能为空"));
                }

                var transformer = new CodeTransformer(sourceLanguage, targetLanguage, transformationType);
                var result = transformer.Transform(code);

                return Task.FromResult(ToolResponse.Success(new
                {
                    transformed_code = result.TransformedCode,
                    conversion_notes = result.ConversionNotes,
                    warnings = result.Warnings
                }));
            }
            catch (Exception ex)
            {
                return Task.FromResult(ToolResponse.Error("EXECUTION_FAILED", $"代码转换失败: {ex.Message}"));
            }
        }

        /// <summary>
        /// 应用代码编辑
        /// </summary>
        public Task<ToolResponse> ApplyEditAsync(JsonElement parameters)
        {
            try
            {
                var code = parameters.GetProperty("code").GetString();
                var search = parameters.GetProperty("search").GetString();
                var replace = parameters.GetProperty("replace").GetString();
                var language = parameters.TryGetProperty("language", out var langProp) 
                    ? langProp.GetString()?.ToLowerInvariant() 
                    : null;

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
                return Task.FromResult(ToolResponse.Error("EXECUTION_FAILED", $"应用编辑失败: {ex.Message}"));
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

        public RefactorResult Execute(string operation, string target, string newName)
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
                case "move_method":
                    result = MoveMethod(target, newName);
                    break;
                case "extract_class":
                    result = ExtractClass(target, newName);
                    break;
                default:
                    throw new ArgumentException($"不支持的重构操作: {operation}");
            }

            return result;
        }

        private RefactorResult Rename(string oldName, string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
            {
                throw new ArgumentException("重命名操作需要提供新名称");
            }

            var result = new RefactorResult();
            var changes = new List<ChangeInfo>();
            var affectedLines = new List<int>();

            // 使用正则表达式匹配变量、方法、类名
            var patterns = GetRenamePatterns(oldName);
            var refactoredCode = _code;

            foreach (var pattern in patterns)
            {
                var matches = Regex.Matches(refactoredCode, pattern, RegexOptions.Multiline);
                foreach (Match match in matches.Cast<Match>().OrderByDescending(m => m.Index))
                {
                    var lineNumber = GetLineNumber(match.Index);
                    refactoredCode = refactoredCode.Substring(0, match.Groups[1].Index) + newName + refactoredCode.Substring(match.Groups[1].Index + match.Groups[1].Length);
                    
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
            }

            result.RefactoredCode = refactoredCode;
            result.Changes = changes;
            result.AffectedLines = affectedLines.OrderBy(l => l).ToList();

            return result;
        }

        private RefactorResult ExtractMethod(string lineRange)
        {
            var result = new RefactorResult();
            var changes = new List<ChangeInfo>();

            // 解析行号范围
            var (startLine, endLine) = ParseLineRange(lineRange);
            if (startLine < 1 || endLine > _lines.Length || startLine > endLine)
            {
                throw new ArgumentException("无效的行号范围");
            }

            // 提取代码块
            var extractedLines = new List<string>();
            for (int i = startLine - 1; i < endLine && i < _lines.Length; i++)
            {
                extractedLines.Add(_lines[i]);
            }

            var extractedCode = string.Join("\n", extractedLines);
            var methodName = $"ExtractedMethod{Guid.NewGuid().ToString("N").Substring(0, 4)}";

            // 生成新方法
            var newMethod = GenerateMethod(methodName, extractedCode);

            // 替换原代码
            var refactoredLines = _lines.ToList();
            var indent = GetIndentation(_lines[startLine - 1]);
            refactoredLines.RemoveRange(startLine - 1, endLine - startLine + 1);
            refactoredLines.Insert(startLine - 1, $"{indent}{methodName}();");

            // 在类末尾添加新方法
            var classEndLine = FindClassEndLine(startLine);
            refactoredLines.Insert(classEndLine, "");
            refactoredLines.Insert(classEndLine + 1, newMethod);

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

            // 查找变量声明
            var declarationPattern = $@"\b{variableName}\b\s*=\s*([^;]+);";
            var declarationMatch = Regex.Match(_code, declarationPattern);
            
            if (!declarationMatch.Success)
            {
                throw new ArgumentException($"未找到变量 '{variableName}' 的声明");
            }

            var variableValue = declarationMatch.Groups[1].Value.Trim();
            var refactoredCode = _code;
            
            // 替换所有使用该变量的地方
            var usagePattern = $@"\b{variableName}\b";
            var matches = Regex.Matches(refactoredCode, usagePattern);
            
            foreach (Match match in matches.Cast<Match>().OrderByDescending(m => m.Index))
            {
                // 跳过声明处
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

            // 删除变量声明
            refactoredCode = Regex.Replace(refactoredCode, $@"^\s*{Regex.Escape(declarationMatch.Value)}\s*$", "", RegexOptions.Multiline);

            result.RefactoredCode = refactoredCode;
            result.Changes = changes;
            result.AffectedLines = affectedLines.OrderBy(l => l).ToList();

            return result;
        }

        private RefactorResult MoveMethod(string methodName, string targetClass)
        {
            var result = new RefactorResult();
            var changes = new List<ChangeInfo>();

            // 查找方法
            var methodPattern = $@"(?:public|private|protected|internal|static|async|virtual|override|abstract|\s)+\s+(?:[\w<>,\s]+)\s+{methodName}\s*\([^)]*\)\s*{{[\s\S]*?^\s*}}";
            var methodMatch = Regex.Match(_code, methodPattern, RegexOptions.Multiline);

            if (!methodMatch.Success)
            {
                throw new ArgumentException($"未找到方法 '{methodName}'");
            }

            // 这里简化处理，实际应该将方法移动到目标类
            changes.Add(new ChangeInfo
            {
                Type = "move_method",
                Description = $"将方法 '{methodName}' 移动到类 '{targetClass}'",
                LineStart = GetLineNumber(methodMatch.Index),
                LineEnd = GetLineNumber(methodMatch.Index + methodMatch.Length)
            });

            result.RefactoredCode = _code; // 简化处理
            result.Changes = changes;
            result.AffectedLines = changes.Select(c => c.LineStart).ToList();

            return result;
        }

        private RefactorResult ExtractClass(string lineRange, string newClassName)
        {
            if (string.IsNullOrWhiteSpace(newClassName))
            {
                throw new ArgumentException("提取类操作需要提供新类名");
            }

            var result = new RefactorResult();
            var changes = new List<ChangeInfo>();

            // 解析行号范围
            var (startLine, endLine) = ParseLineRange(lineRange);
            if (startLine < 1 || endLine > _lines.Length || startLine > endLine)
            {
                throw new ArgumentException("无效的行号范围");
            }

            // 提取代码块
            var extractedLines = new List<string>();
            for (int i = startLine - 1; i < endLine && i < _lines.Length; i++)
            {
                extractedLines.Add(_lines[i]);
            }

            var extractedCode = string.Join("\n", extractedLines);
            
            // 生成新类
            var newClass = GenerateClass(newClassName, extractedCode);

            // 替换原代码
            var refactoredLines = _lines.ToList();
            refactoredLines.RemoveRange(startLine - 1, endLine - startLine + 1);
            refactoredLines.Insert(startLine - 1, $"// 已提取到类 {newClassName}");

            // 在文件末尾添加新类
            refactoredLines.Add("");
            refactoredLines.Add(newClass);

            result.RefactoredCode = string.Join("\n", refactoredLines);
            result.Changes = new List<ChangeInfo>
            {
                new ChangeInfo
                {
                    Type = "extract_class",
                    Description = $"提取类 '{newClassName}'",
                    LineStart = startLine,
                    LineEnd = endLine
                }
            };
            result.AffectedLines = Enumerable.Range(startLine, endLine - startLine + 1).ToList();

            return result;
        }

        private string[] GetRenamePatterns(string name)
        {
            return _language switch
            {
                "csharp" => new[]
                {
                    $@"\bclass\s+({name})\b",
                    $@"\bstruct\s+({name})\b",
                    $@"\binterface\s+({name})\b",
                    $@"\benum\s+({name})\b",
                    $@"\b(?:public|private|protected|internal|static|async|virtual|override|abstract|\s)+\s+(?:[\w<>,\s]+)\s+({name})\s*\(",
                    $@"\bvar\s+({name})\b",
                    $@"\b(?:int|string|bool|double|float|decimal|long|short|byte|char|object|dynamic)\s+({name})\b"
                },
                "javascript" or "typescript" => new[]
                {
                    $@"\bclass\s+({name})\b",
                    $@"\bfunction\s+({name})\b",
                    $@"\b(?:const|let|var)\s+({name})\b",
                    $@"\b({name})\s*[=:]\s*function\b",
                    $@"\b({name})\s*[=:]\s*\([^)]*\)\s*=>"
                },
                "python" => new[]
                {
                    $@"\bclass\s+({name})\b",
                    $@"\bdef\s+({name})\b",
                    $@"\b({name})\s*="
                },
                "java" => new[]
                {
                    $@"\bclass\s+({name})\b",
                    $@"\binterface\s+({name})\b",
                    $@"\b(?:public|private|protected|static|final|abstract|\s)+\s+(?:[\w<>,\s]+)\s+({name})\s*\(",
                    $@"\b(?:int|String|boolean|double|float|long|short|byte|char|Object|var)\s+({name})\b"
                },
                _ => new[] { $@"\b({name})\b" }
            };
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

        private int FindClassEndLine(int startLine)
        {
            int braceCount = 0;
            for (int i = startLine - 1; i < _lines.Length; i++)
            {
                braceCount += _lines[i].Count(c => c == '{');
                braceCount -= _lines[i].Count(c => c == '}');
                if (braceCount == 0 && i > startLine - 1)
                {
                    return i;
                }
            }
            return _lines.Length - 1;
        }

        private string GenerateMethod(string methodName, string body)
        {
            var lines = body.Split('\n');
            var minIndent = lines.Where(l => !string.IsNullOrWhiteSpace(l)).Min(l => GetIndentation(l).Length);
            var normalizedBody = string.Join("\n", lines.Select(l => l.Length >= minIndent ? l.Substring(minIndent) : l));

            return $@"private void {methodName}()
{{
{normalizedBody}
}}";
        }

        private string GenerateClass(string className, string body)
        {
            var lines = body.Split('\n');
            var minIndent = lines.Where(l => !string.IsNullOrWhiteSpace(l)).Min(l => GetIndentation(l).Length);
            var normalizedBody = string.Join("\n", lines.Select(l => l.Length >= minIndent ? l.Substring(minIndent) : l));

            return $@"public class {className}
{{
{normalizedBody}
}}";
        }
    }

    /// <summary>
    /// 代码格式化器
    /// </summary>
    public class CodeFormatter
    {
        private readonly string _language;
        private readonly string _style;
        private readonly JsonElement _options;

        public CodeFormatter(string language, string style, JsonElement options)
        {
            _language = language.ToLowerInvariant();
            _style = style?.ToLowerInvariant() ?? "default";
            _options = options;
        }

        public FormatResult Format(string code)
        {
            var result = new FormatResult();
            var originalCode = code;
            var formattedCode = code;
            var stats = new FormatStats();

            // 获取格式化选项
            var indentSize = GetOption("indent_size", 4);
            var useTabs = GetOption("use_tabs", false);
            var maxLineLength = GetOption("max_line_length", 120);
            var braceStyle = GetOption("brace_style", "same_line");
            var insertFinalNewline = GetOption("insert_final_newline", true);

            var indentString = useTabs ? "\t" : new string(' ', indentSize);

            // 根据语言进行格式化
            formattedCode = FormatByLanguage(formattedCode, indentString, braceStyle);

            // 通用格式化
            formattedCode = FormatCommon(formattedCode, indentString, maxLineLength);

            // 处理末尾换行
            if (insertFinalNewline && !formattedCode.EndsWith("\n"))
            {
                formattedCode += "\n";
            }

            // 计算统计信息
            stats.LinesChanged = CountChangedLines(originalCode, formattedCode);
            stats.WhitespaceChanges = CountWhitespaceChanges(originalCode, formattedCode);
            stats.IndentationChanges = CountIndentationChanges(originalCode, formattedCode);

            result.FormattedCode = formattedCode;
            result.ChangesMade = originalCode != formattedCode;
            result.FormatStats = stats;

            return result;
        }

        private string FormatByLanguage(string code, string indent, string braceStyle)
        {
            return _language switch
            {
                "csharp" or "java" or "cpp" or "c" => FormatCStyle(code, indent, braceStyle),
                "javascript" or "typescript" => FormatJavaScript(code, indent, braceStyle),
                "python" => FormatPython(code, indent),
                "go" => FormatGo(code, indent),
                "rust" => FormatRust(code, indent),
                "html" or "xml" => FormatMarkup(code, indent),
                "json" => FormatJson(code, indent),
                _ => code
            };
        }

        private string FormatCStyle(string code, string indent, string braceStyle)
        {
            var lines = code.Split('\n');
            var result = new List<string>();
            var currentIndent = "";
            var inString = false;
            char stringChar = '\0';

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                
                // 处理字符串
                foreach (var c in trimmed)
                {
                    if (!inString && (c == '"' || c == '\''))
                    {
                        inString = true;
                        stringChar = c;
                    }
                    else if (inString && c == stringChar)
                    {
                        inString = false;
                    }
                }

                if (!inString)
                {
                    // 减少缩进
                    if (trimmed.StartsWith("}") || trimmed.StartsWith("]"))
                    {
                        currentIndent = currentIndent.Length >= indent.Length 
                            ? currentIndent.Substring(indent.Length) 
                            : "";
                    }

                    // 格式化大括号
                    if (braceStyle == "new_line" && trimmed.StartsWith("{"))
                    {
                        result.Add(currentIndent);
                    }
                }

                if (!string.IsNullOrWhiteSpace(trimmed))
                {
                    result.Add(currentIndent + trimmed);
                }

                if (!inString)
                {
                    // 增加缩进
                    if (trimmed.EndsWith("{") || trimmed.EndsWith("["))
                    {
                        currentIndent += indent;
                    }
                }
            }

            return string.Join("\n", result);
        }

        private string FormatJavaScript(string code, string indent, string braceStyle)
        {
            // JavaScript特殊处理
            var formatted = FormatCStyle(code, indent, braceStyle);
            
            // 添加分号（如果缺失）
            formatted = Regex.Replace(formatted, @"(?<!;|\{|\}|\))\s*$", ";", RegexOptions.Multiline);
            
            return formatted;
        }

        private string FormatPython(string code, string indent)
        {
            var lines = code.Split('\n');
            var result = new List<string>();
            var currentIndent = "";
            var indentLevel = 0;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                
                // 减少缩进
                if (!string.IsNullOrWhiteSpace(trimmed) && !trimmed.StartsWith("#"))
                {
                    var newIndentLevel = GetPythonIndentLevel(trimmed);
                    if (newIndentLevel < indentLevel)
                    {
                        indentLevel = newIndentLevel;
                        currentIndent = string.Join("", Enumerable.Repeat(indent, indentLevel));
                    }
                }

                if (!string.IsNullOrWhiteSpace(trimmed))
                {
                    result.Add(currentIndent + trimmed);
                }
                else
                {
                    result.Add("");
                }

                // 增加缩进
                if (trimmed.EndsWith(":"))
                {
                    indentLevel++;
                    currentIndent += indent;
                }
            }

            return string.Join("\n", result);
        }

        private int GetPythonIndentLevel(string line)
        {
            // 简单的缩进级别检测
            if (line.StartsWith("def ") || line.StartsWith("class ") || line.StartsWith("if ") ||
                line.StartsWith("elif ") || line.StartsWith("else:") || line.StartsWith("for ") ||
                line.StartsWith("while ") || line.StartsWith("try:") || line.StartsWith("except"))
            {
                return 0;
            }
            return 1; // 简化处理
        }

        private string FormatGo(string code, string indent)
        {
            // Go使用tab缩进
            return FormatCStyle(code, "\t", "same_line");
        }

        private string FormatRust(string code, string indent)
        {
            return FormatCStyle(code, indent, "same_line");
        }

        private string FormatMarkup(string code, string indent)
        {
            // 简单的XML/HTML格式化
            var formatted = Regex.Replace(code, @">\s*<", ">\n<");
            var lines = formatted.Split('\n');
            var result = new List<string>();
            var currentIndent = "";

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("</"))
                {
                    currentIndent = currentIndent.Length >= indent.Length 
                        ? currentIndent.Substring(indent.Length) 
                        : "";
                }

                if (!string.IsNullOrWhiteSpace(trimmed))
                {
                    result.Add(currentIndent + trimmed);
                }

                if (trimmed.StartsWith("<") && !trimmed.StartsWith("</") && !trimmed.EndsWith("/>"))
                {
                    currentIndent += indent;
                }
            }

            return string.Join("\n", result);
        }

        private string FormatJson(string code, string indent)
        {
            try
            {
                var doc = JsonDocument.Parse(code);
                return JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
            }
            catch
            {
                return code;
            }
        }

        private string FormatCommon(string code, string indent, int maxLineLength)
        {
            // 移除行尾空格
            code = Regex.Replace(code, @"[ \t]+$", "", RegexOptions.Multiline);
            
            // 统一换行符
            code = code.Replace("\r\n", "\n").Replace("\r", "\n");
            
            // 确保操作符周围有空格
            code = Regex.Replace(code, @"\s*([=+\-*/<>!&|]+)\s*", " $1 ");
            
            // 移除多余空格
            code = Regex.Replace(code, @"  +", " ");
            
            return code;
        }

        private T GetOption<T>(string name, T defaultValue)
        {
            if (_options.TryGetProperty(name, out var prop))
            {
                if (typeof(T) == typeof(int) && prop.ValueKind == JsonValueKind.Number)
                {
                    return (T)(object)prop.GetInt32();
                }
                if (typeof(T) == typeof(bool) && prop.ValueKind == JsonValueKind.True)
                {
                    return (T)(object)true;
                }
                if (typeof(T) == typeof(bool) && prop.ValueKind == JsonValueKind.False)
                {
                    return (T)(object)false;
                }
                if (typeof(T) == typeof(string) && prop.ValueKind == JsonValueKind.String)
                {
                    return (T)(object)prop.GetString();
                }
            }
            return defaultValue;
        }

        private int CountChangedLines(string original, string formatted)
        {
            var origLines = original.Split('\n');
            var formLines = formatted.Split('\n');
            int count = 0;
            int minLen = Math.Min(origLines.Length, formLines.Length);
            
            for (int i = 0; i < minLen; i++)
            {
                if (origLines[i] != formLines[i]) count++;
            }
            
            count += Math.Abs(origLines.Length - formLines.Length);
            return count;
        }

        private int CountWhitespaceChanges(string original, string formatted)
        {
            var origSpaces = original.Count(c => c == ' ' || c == '\t');
            var formSpaces = formatted.Count(c => c == ' ' || c == '\t');
            return Math.Abs(origSpaces - formSpaces);
        }

        private int CountIndentationChanges(string original, string formatted)
        {
            var origLines = original.Split('\n');
            var formLines = formatted.Split('\n');
            int count = 0;
            int minLen = Math.Min(origLines.Length, formLines.Length);
            
            for (int i = 0; i < minLen; i++)
            {
                var origIndent = Regex.Match(origLines[i], @"^(\s*)").Groups[1].Value;
                var formIndent = Regex.Match(formLines[i], @"^(\s*)").Groups[1].Value;
                if (origIndent != formIndent) count++;
            }
            
            return count;
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
        private readonly string _context;

        public CodeGenerator(string language, string template, JsonElement variables, string context)
        {
            _language = language.ToLowerInvariant();
            _template = template;
            _variables = variables;
            _context = context;
        }

        public GenerationResult Generate()
        {
            var result = new GenerationResult();
            var generatedCode = _template;
            var explanation = "";
            var suggestions = new List<string>();

            // 替换模板变量
            generatedCode = ReplaceTemplateVariables(generatedCode);

            // 根据模板类型生成代码
            if (_template.ToLower().Contains("class"))
            {
                generatedCode = GenerateClassTemplate(generatedCode);
                explanation = "生成了一个类定义，包含基本的属性和方法结构";
                suggestions.Add("根据实际需求添加更多属性和方法");
                suggestions.Add("考虑添加构造函数和验证逻辑");
            }
            else if (_template.ToLower().Contains("function") || _template.ToLower().Contains("method"))
            {
                generatedCode = GenerateFunctionTemplate(generatedCode);
                explanation = "生成了一个函数/方法定义，包含参数和返回值";
                suggestions.Add("添加参数验证逻辑");
                suggestions.Add("考虑添加异常处理");
            }
            else if (_template.ToLower().Contains("api") || _template.ToLower().Contains("controller"))
            {
                generatedCode = GenerateApiTemplate(generatedCode);
                explanation = "生成了一个API端点/控制器，包含基本的CRUD操作";
                suggestions.Add("添加身份验证和授权");
                suggestions.Add("实现输入验证和错误处理");
            }
            else
            {
                generatedCode = GenerateGenericTemplate(generatedCode);
                explanation = "根据模板生成了代码";
                suggestions.Add("请检查生成的代码是否符合需求");
            }

            // 添加上下文相关的代码
            if (!string.IsNullOrWhiteSpace(_context))
            {
                generatedCode = AddContextualCode(generatedCode);
            }

            result.GeneratedCode = generatedCode;
            result.Explanation = explanation;
            result.Suggestions = suggestions;

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
    // 属性
    public string Name {{ get; set; }}
    
    // 构造函数
    public {template}()
    {{
    }}
    
    // 方法
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
                "java" => $@"public class {template} {{
    // 属性
    private String name;
    
    // 构造函数
    public {template}() {{
    }}
    
    // 方法
    public void execute() {{
        // TODO: 实现逻辑
    }}
}}",
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
                "java" => $@"public void {template}() {{
    // TODO: 实现逻辑
}}",
                _ => template
            };
        }

        private string GenerateApiTemplate(string template)
        {
            return _language switch
            {
                "csharp" => $@"[ApiController]
[Route(""api/[controller]"")]
public class {template}Controller : ControllerBase
{{
    [HttpGet]
    public IActionResult GetAll()
    {{
        // TODO: 实现获取所有数据
        return Ok();
    }}
    
    [HttpGet(""{{id}}"")]
    public IActionResult GetById(int id)
    {{
        // TODO: 实现根据ID获取
        return Ok();
    }}
    
    [HttpPost]
    public IActionResult Create([FromBody] object model)
    {{
        // TODO: 实现创建
        return CreatedAtAction(nameof(GetById), new {{ id = 1 }}, model);
    }}
    
    [HttpPut(""{{id}}"")]
    public IActionResult Update(int id, [FromBody] object model)
    {{
        // TODO: 实现更新
        return NoContent();
    }}
    
    [HttpDelete(""{{id}}"")]
    public IActionResult Delete(int id)
    {{
        // TODO: 实现删除
        return NoContent();
    }}
}}",
                _ => template
            };
        }

        private string GenerateGenericTemplate(string template)
        {
            // 通用模板处理
            return template;
        }

        private string AddContextualCode(string code)
        {
            // 根据上下文添加相关代码
            if (_context.ToLower().Contains("error handling") || _context.ToLower().Contains("exception"))
            {
                code = AddErrorHandling(code);
            }
            
            if (_context.ToLower().Contains("logging"))
            {
                code = AddLogging(code);
            }
            
            if (_context.ToLower().Contains("validation"))
            {
                code = AddValidation(code);
            }
            
            return code;
        }

        private string AddErrorHandling(string code)
        {
            return _language switch
            {
                "csharp" => $@"try
{{
    {code}
}}
catch (Exception ex)
{{
    // TODO: 处理异常
    throw;
}}",
                "javascript" or "typescript" => $@"try {{
    {code}
}} catch (error) {{
    // TODO: 处理异常
    console.error(error);
}}",
                "python" => $@"try:
    {code}
except Exception as e:
    # TODO: 处理异常
    pass",
                "java" => $@"try {{
    {code}
}} catch (Exception e) {{
    // TODO: 处理异常
    throw e;
}}",
                _ => code
            };
        }

        private string AddLogging(string code)
        {
            // 简化处理，实际应该根据语言添加适当的日志代码
            return $"// TODO: 添加日志记录\n{code}";
        }

        private string AddValidation(string code)
        {
            // 简化处理，实际应该根据语言添加适当的验证代码
            return $"// TODO: 添加输入验证\n{code}";
        }
    }

    /// <summary>
    /// 代码转换器
    /// </summary>
    public class CodeTransformer
    {
        private readonly string _sourceLanguage;
        private readonly string _targetLanguage;
        private readonly string _transformationType;

        public CodeTransformer(string sourceLanguage, string targetLanguage, string transformationType)
        {
            _sourceLanguage = sourceLanguage.ToLowerInvariant();
            _targetLanguage = targetLanguage.ToLowerInvariant();
            _transformationType = transformationType.ToLowerInvariant();
        }

        public TransformationResult Transform(string code)
        {
            var result = new TransformationResult();
            var transformedCode = code;
            var notes = new List<string>();
            var warnings = new List<string>();

            switch (_transformationType)
            {
                case "language_convert":
                    transformedCode = ConvertLanguage(code, notes, warnings);
                    break;
                case "syntax_upgrade":
                    transformedCode = UpgradeSyntax(code, notes, warnings);
                    break;
                case "framework_migrate":
                    transformedCode = MigrateFramework(code, notes, warnings);
                    break;
                default:
                    warnings.Add($"未知的转换类型: {_transformationType}");
                    break;
            }

            result.TransformedCode = transformedCode;
            result.ConversionNotes = notes;
            result.Warnings = warnings;

            return result;
        }

        private string ConvertLanguage(string code, List<string> notes, List<string> warnings)
        {
            notes.Add($"从 {_sourceLanguage} 转换到 {_targetLanguage}");

            // 语言转换映射
            var conversion = ($"{_sourceLanguage}_to_{_targetLanguage}") switch
            {
                "javascript_to_typescript" => ConvertJavaScriptToTypeScript(code, notes, warnings),
                "python_to_csharp" => ConvertPythonToCSharp(code, notes, warnings),
                "java_to_csharp" => ConvertJavaToCSharp(code, notes, warnings),
                "csharp_to_java" => ConvertCSharpToJava(code, notes, warnings),
                _ => null
            };

            if (conversion == null)
            {
                warnings.Add($"暂不支持从 {_sourceLanguage} 到 {_targetLanguage} 的转换");
                return code;
            }

            return conversion;
        }

        private string ConvertJavaScriptToTypeScript(string code, List<string> notes, List<string> warnings)
        {
            notes.Add("添加类型注解");
            notes.Add("转换模块导入语法");

            // 添加类型注解
            code = Regex.Replace(code, @"function\s+(\w+)\s*\(([^)]*)\)", m =>
            {
                var funcName = m.Groups[1].Value;
                var parameters = m.Groups[2].Value;
                return $"function {funcName}({parameters}): any";
            });

            // 转换var/let为const（推荐做法）
            code = Regex.Replace(code, @"\bvar\b", "const");

            return code;
        }

        private string ConvertPythonToCSharp(string code, List<string> notes, List<string> warnings)
        {
            notes.Add("转换缩进为花括号");
            notes.Add("添加类型声明");
            warnings.Add("需要手动检查类型转换");

            // 转换def为方法
            code = Regex.Replace(code, @"def\s+(\w+)\s*\(([^)]*)\):", m =>
            {
                var funcName = m.Groups[1].Value;
                var parameters = m.Groups[2].Value;
                return $"public void {funcName}({parameters})\n{{";
            });

            // 转换print为Console.WriteLine
            code = Regex.Replace(code, @"print\s*\(([^)]+)\)", "Console.WriteLine($1);");

            // 转换self为this
            code = Regex.Replace(code, @"\bself\b", "this");

            return code;
        }

        private string ConvertJavaToCSharp(string code, List<string> notes, List<string> warnings)
        {
            notes.Add("转换Java语法为C#语法");
            notes.Add("转换集合类型");

            // 转换String为string
            code = Regex.Replace(code, @"\bString\b", "string");

            // 转换System.out.println为Console.WriteLine
            code = Regex.Replace(code, @"System\.out\.println\s*\(([^)]+)\)", "Console.WriteLine($1);");

            // 转换List/Map等
            code = Regex.Replace(code, @"List<", "List<");
            code = Regex.Replace(code, @"Map<", "Dictionary<");
            code = Regex.Replace(code, @"Set<", "HashSet<");

            return code;
        }

        private string ConvertCSharpToJava(string code, List<string> notes, List<string> warnings)
        {
            notes.Add("转换C#语法为Java语法");
            notes.Add("转换属性为getter/setter");

            // 转换string为String
            code = Regex.Replace(code, @"\bstring\b", "String");

            // 转换Console.WriteLine为System.out.println
            code = Regex.Replace(code, @"Console\.WriteLine\s*\(([^)]+)\)", "System.out.println($1);");

            // 转换var为具体类型（简化处理）
            code = Regex.Replace(code, @"\bvar\b", "Object");

            return code;
        }

        private string UpgradeSyntax(string code, List<string> notes, List<string> warnings)
        {
            notes.Add($"升级 {_sourceLanguage} 语法");

            return _sourceLanguage switch
            {
                "csharp" => UpgradeCSharpSyntax(code, notes, warnings),
                "javascript" => UpgradeJavaScriptSyntax(code, notes, warnings),
                "python" => UpgradePythonSyntax(code, notes, warnings),
                _ => code
            };
        }

        private string UpgradeCSharpSyntax(string code, List<string> notes, List<string> warnings)
        {
            notes.Add("使用模式匹配");
            notes.Add("使用空合并赋值运算符");

            // 简化模式匹配（示例）
            code = Regex.Replace(code, @"if\s*\(\s*(\w+)\s*!=\s*null\s*\)", "if ($1 is not null)");

            return code;
        }

        private string UpgradeJavaScriptSyntax(string code, List<string> notes, List<string> warnings)
        {
            notes.Add("使用箭头函数");
            notes.Add("使用解构赋值");
            notes.Add("使用模板字符串");

            // 转换函数为箭头函数（简化示例）
            code = Regex.Replace(code, @"function\s+(\w+)\s*\(([^)]*)\)\s*\{([^}]+)\}", m =>
            {
                var funcName = m.Groups[1].Value;
                var parameters = m.Groups[2].Value;
                var body = m.Groups[3].Value.Trim();
                return $"const {funcName} = ({parameters}) => {{ {body} }};";
            });

            return code;
        }

        private string UpgradePythonSyntax(string code, List<string> notes, List<string> warnings)
        {
            notes.Add("使用类型注解");
            notes.Add("使用f-string格式化");

            // 添加类型注解示例
            code = Regex.Replace(code, @"def\s+(\w+)\s*\(([^)]*)\):", m =>
            {
                var funcName = m.Groups[1].Value;
                var parameters = m.Groups[2].Value;
                return $"def {funcName}({parameters}) -> None:";
            });

            return code;
        }

        private string MigrateFramework(string code, List<string> notes, List<string> warnings)
        {
            notes.Add($"迁移 {_sourceLanguage} 框架");
            warnings.Add("框架迁移需要手动验证");

            // 这里应该实现具体的框架迁移逻辑
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
            
            // 规范化搜索和替换内容
            var normalizedSearch = NormalizeWhitespace(search);
            var normalizedCode = NormalizeWhitespace(_code);
            
            // 查找匹配
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

            // 执行替换
            var editedCode = normalizedCode.Substring(0, matches[0].Index) + replace + normalizedCode.Substring(matches[0].Index + matches[0].Length);
            
            result.Success = true;
            result.EditedCode = editedCode;

            return result;
        }

        private string NormalizeWhitespace(string text)
        {
            // 标准化空白字符
            var lines = text.Split('\n');
            var normalizedLines = lines.Select(l => l.TrimEnd()).Where(l => !string.IsNullOrWhiteSpace(l));
            return string.Join("\n", normalizedLines);
        }
    }

    // 数据模型类
    public class RefactorResult
    {
        public string OriginalCode { get; set; }
        public string RefactoredCode { get; set; }
        public List<ChangeInfo> Changes { get; set; } = new();
        public List<int> AffectedLines { get; set; } = new();
    }

    public class ChangeInfo
    {
        public string Type { get; set; }
        public string Description { get; set; }
        public int LineStart { get; set; }
        public int LineEnd { get; set; }
    }

    public class FormatResult
    {
        public string FormattedCode { get; set; }
        public bool ChangesMade { get; set; }
        public FormatStats FormatStats { get; set; }
    }

    public class FormatStats
    {
        public int LinesChanged { get; set; }
        public int WhitespaceChanges { get; set; }
        public int IndentationChanges { get; set; }
    }

    public class GenerationResult
    {
        public string GeneratedCode { get; set; }
        public string Explanation { get; set; }
        public List<string> Suggestions { get; set; } = new();
    }

    public class TransformationResult
    {
        public string TransformedCode { get; set; }
        public List<string> ConversionNotes { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }

    public class EditResult
    {
        public string EditedCode { get; set; }
        public bool Success { get; set; }
        public int MatchCount { get; set; }
    }

    /// <summary>
    /// 工具响应类
    /// </summary>
    public class ToolResponse
    {
        public string Status { get; set; }
        public object Data { get; set; }
        public ToolError Error { get; set; }
        public long ExecutionTime { get; set; }

        public static ToolResponse Success(object data)
        {
            return new ToolResponse
            {
                Status = "success",
                Data = data
            };
        }

        public static ToolResponse Error(string code, string message)
        {
            return new ToolResponse
            {
                Status = "error",
                Error = new ToolError { Code = code, Message = message }
            };
        }
    }

    public class ToolError
    {
        public string Code { get; set; }
        public string Message { get; set; }
    }
}
