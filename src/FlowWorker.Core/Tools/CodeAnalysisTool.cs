using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using FlowWorker.Core.Interfaces;

namespace FlowWorker.Core.Tools;

/// <summary>
/// 代码分析工具处理器
/// 提供代码解析、依赖分析、复杂度分析等功能
/// </summary>
public class CodeAnalysisTool : IToolHandler
{
    public string Name => "CodeAnalysis";

    public async Task<ToolResponse> ExecuteAsync(string action, JsonElement parameters)
    {
        return action.ToLowerInvariant() switch
        {
            "parse_code" => await ParseCodeAsync(parameters),
            "find_function" => await FindFunctionAsync(parameters),
            "find_class" => await FindClassAsync(parameters),
            "get_dependencies" => await GetDependenciesAsync(parameters),
            "analyze_complexity" => await AnalyzeComplexityAsync(parameters),
            _ => ToolResponse.Error("UNKNOWN_ACTION", $"未知的操作：{action}")
        };
    }

    private Task<ToolResponse> ParseCodeAsync(JsonElement parameters)
    {
        try
        {
            var code = parameters.GetProperty("code").GetString();
            var language = parameters.GetProperty("language").GetString()?.ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(code))
            {
                return Task.FromResult(ToolResponse.Error("INVALID_PARAMETERS", "代码内容不能为空"));
            }

            if (string.IsNullOrWhiteSpace(language))
            {
                return Task.FromResult(ToolResponse.Error("INVALID_PARAMETERS", "编程语言不能为空"));
            }

            var parser = new CodeParser(language, code);
            var result = parser.Parse();

            return Task.FromResult(ToolResponse.Success(new
            {
                language = language,
                functions = result.Functions,
                classes = result.Classes,
                imports = result.Imports,
                variables = result.Variables,
                line_count = code.Split('\n').Length
            }));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResponse.Error("EXECUTION_FAILED", $"代码解析失败：{ex.Message}"));
        }
    }

    private Task<ToolResponse> FindFunctionAsync(JsonElement parameters)
    {
        try
        {
            var code = parameters.GetProperty("code").GetString();
            var functionName = parameters.GetProperty("function_name").GetString();
            var language = parameters.TryGetProperty("language", out var langProp) 
                ? langProp.GetString()?.ToLowerInvariant() 
                : null;

            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(functionName))
            {
                return Task.FromResult(ToolResponse.Error("INVALID_PARAMETERS", "代码内容和函数名称不能为空"));
            }

            var parser = new CodeParser(language ?? "csharp", code);
            var functions = parser.FindFunctions(functionName);

            return Task.FromResult(ToolResponse.Success(new
            {
                found = functions.Count > 0,
                functions = functions,
                count = functions.Count
            }));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResponse.Error("EXECUTION_FAILED", $"查找函数失败：{ex.Message}"));
        }
    }

    private Task<ToolResponse> FindClassAsync(JsonElement parameters)
    {
        try
        {
            var code = parameters.GetProperty("code").GetString();
            var className = parameters.GetProperty("class_name").GetString();
            var language = parameters.TryGetProperty("language", out var langProp) 
                ? langProp.GetString()?.ToLowerInvariant() 
                : null;

            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(className))
            {
                return Task.FromResult(ToolResponse.Error("INVALID_PARAMETERS", "代码内容和类名称不能为空"));
            }

            var parser = new CodeParser(language ?? "csharp", code);
            var classes = parser.FindClasses(className);

            return Task.FromResult(ToolResponse.Success(new
            {
                found = classes.Count > 0,
                classes = classes,
                count = classes.Count
            }));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResponse.Error("EXECUTION_FAILED", $"查找类失败：{ex.Message}"));
        }
    }

    private Task<ToolResponse> GetDependenciesAsync(JsonElement parameters)
    {
        try
        {
            var projectPath = parameters.GetProperty("project_path").GetString();
            var type = parameters.TryGetProperty("type", out var typeProp) 
                ? typeProp.GetString()?.ToLowerInvariant() 
                : "all";

            if (string.IsNullOrWhiteSpace(projectPath))
            {
                return Task.FromResult(ToolResponse.Error("INVALID_PARAMETERS", "项目路径不能为空"));
            }

            if (!Directory.Exists(projectPath))
            {
                return Task.FromResult(ToolResponse.Error("PROJECT_NOT_FOUND", $"项目路径不存在：{projectPath}"));
            }

            var dependencyAnalyzer = new DependencyAnalyzer();
            var result = dependencyAnalyzer.Analyze(projectPath, type);

            return Task.FromResult(ToolResponse.Success(new
            {
                project_path = projectPath,
                project_type = result.ProjectType,
                dependencies = result.Dependencies,
                dev_dependencies = result.DevDependencies,
                total_count = result.Dependencies.Count + result.DevDependencies.Count
            }));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResponse.Error("EXECUTION_FAILED", $"获取依赖失败：{ex.Message}"));
        }
    }

    private Task<ToolResponse> AnalyzeComplexityAsync(JsonElement parameters)
    {
        try
        {
            var code = parameters.GetProperty("code").GetString();
            var language = parameters.TryGetProperty("language", out var langProp) 
                ? langProp.GetString()?.ToLowerInvariant() 
                : null;

            if (string.IsNullOrWhiteSpace(code))
            {
                return Task.FromResult(ToolResponse.Error("INVALID_PARAMETERS", "代码内容不能为空"));
            }

            var analyzer = new ComplexityAnalyzer(language ?? "csharp", code);
            var result = analyzer.Analyze();

            return Task.FromResult(ToolResponse.Success(new
            {
                overall_score = result.OverallScore,
                cyclomatic_complexity = result.CyclomaticComplexity,
                cognitive_complexity = result.CognitiveComplexity,
                lines_of_code = result.LinesOfCode,
                max_nesting_depth = result.MaxNestingDepth,
                recommendations = result.Recommendations
            }));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResponse.Error("EXECUTION_FAILED", $"复杂度分析失败：{ex.Message}"));
        }
    }
}

/// <summary>
/// 代码解析器
/// </summary>
public class CodeParser
{
    private readonly string _language;
    private readonly string _code;
    private readonly string[] _lines;

    public CodeParser(string language, string code)
    {
        _language = language.ToLowerInvariant();
        _code = code;
        _lines = code.Split('\n');
    }

    public ParseResult Parse()
    {
        return new ParseResult
        {
            Functions = ExtractFunctions(),
            Classes = ExtractClasses(),
            Imports = ExtractImports(),
            Variables = ExtractVariables()
        };
    }

    public List<FunctionInfo> FindFunctions(string pattern)
    {
        var functions = ExtractFunctions();
        var regex = new Regex(pattern, RegexOptions.IgnoreCase);
        return functions.Where(f => regex.IsMatch(f.Name)).ToList();
    }

    public List<ClassInfo> FindClasses(string pattern)
    {
        var classes = ExtractClasses();
        var regex = new Regex(pattern, RegexOptions.IgnoreCase);
        return classes.Where(c => regex.IsMatch(c.Name)).ToList();
    }

    private List<FunctionInfo> ExtractFunctions()
    {
        var functions = new List<FunctionInfo>();
        var patterns = GetFunctionPatterns();

        foreach (var pattern in patterns)
        {
            var matches = Regex.Matches(_code, pattern, RegexOptions.Multiline);
            foreach (Match match in matches)
            {
                var lineNumber = GetLineNumber(match.Index);
                functions.Add(new FunctionInfo
                {
                    Name = match.Groups[1].Value,
                    LineStart = lineNumber,
                    LineEnd = FindBlockEnd(lineNumber),
                    Signature = match.Value.Trim()
                });
            }
        }

        return functions;
    }

    private List<ClassInfo> ExtractClasses()
    {
        var classes = new List<ClassInfo>();
        var pattern = GetClassPattern();

        var matches = Regex.Matches(_code, pattern, RegexOptions.Multiline);
        foreach (Match match in matches)
        {
            var lineNumber = GetLineNumber(match.Index);
            classes.Add(new ClassInfo
            {
                Name = match.Groups[1].Value,
                LineStart = lineNumber,
                LineEnd = FindBlockEnd(lineNumber),
                Definition = match.Value.Trim(),
                BaseClass = match.Groups.Count > 2 ? match.Groups[2].Value : null
            });
        }

        return classes;
    }

    private List<string> ExtractImports()
    {
        var imports = new List<string>();
        var patterns = GetImportPatterns();

        foreach (var pattern in patterns)
        {
            var matches = Regex.Matches(_code, pattern, RegexOptions.Multiline);
            foreach (Match match in matches)
            {
                imports.Add(match.Groups[1].Value.Trim());
            }
        }

        return imports.Distinct().ToList();
    }

    private List<VariableInfo> ExtractVariables()
    {
        var variables = new List<VariableInfo>();
        var pattern = GetVariablePattern();

        var matches = Regex.Matches(_code, pattern, RegexOptions.Multiline);
        foreach (Match match in matches)
        {
            var lineNumber = GetLineNumber(match.Index);
            variables.Add(new VariableInfo
            {
                Name = match.Groups[2].Value,
                Type = match.Groups[1].Value,
                Line = lineNumber
            });
        }

        return variables;
    }

    private string[] GetFunctionPatterns()
    {
        return _language switch
        {
            "csharp" => new[]
            {
                @"(?:public|private|protected|internal|static|async|virtual|override|abstract|\s)+\s+(?:[\w<>,\s]+)\s+([\w]+)\s*\([^)]*\)\s*{",
                @"(?:public|private|protected|internal|static|async|virtual|override|abstract|\s)+\s+(?:[\w<>,\s]+)\s+([\w]+)\s*\([^)]*\)\s*=>"
            },
            "javascript" or "typescript" => new[]
            {
                @"(?:async\s+)?function\s+([\w]+)\s*\([^)]*\)",
                @"(?:const|let|var)\s+([\w]+)\s*=\s*(?:async\s+)?\([^)]*\)\s*=>"
            },
            "python" => new[] { @"def\s+([\w]+)\s*\([^)]*\):" },
            "java" => new[]
            {
                @"(?:public|private|protected|static|final|abstract|\s)+\s+(?:[\w<>,\s]+)\s+([\w]+)\s*\([^)]*\)\s*{"
            },
            _ => new[] { @"(?:function|def|fn|func)\s+([\w]+)\s*\(" }
        };
    }

    private string GetClassPattern()
    {
        return _language switch
        {
            "csharp" => @"(?:public|private|protected|internal|static|abstract|sealed|partial|\s)+\s*class\s+([\w]+)",
            "java" => @"(?:public|private|protected|static|abstract|final|\s)+\s*class\s+([\w]+)",
            "python" => @"class\s+([\w]+)",
            "typescript" => @"(?:export\s+)?(?:abstract\s+)?class\s+([\w]+)",
            _ => @"class\s+([\w]+)"
        };
    }

    private string[] GetImportPatterns()
    {
        return _language switch
        {
            "csharp" => new[] { @"using\s+([\w.]+);" },
            "java" => new[] { @"import\s+([\w.]+);" },
            "python" => new[] { @"(?:import|from)\s+([\w.]+)" },
            "javascript" or "typescript" => new[] { @"import\s+.*?\s+from\s+['""]([^'""]+)['""]" },
            "go" => new[] { @"import\s+['""]([^'""]+)['""]" },
            "rust" => new[] { @"use\s+([\w:]+);" },
            _ => new[] { @"(?:import|include|using|require)\s+['""]?([^;'""]+)['""]?" }
        };
    }

    private string GetVariablePattern()
    {
        return _language switch
        {
            "csharp" or "java" => @"(?:[\w<>,\s]+)\s+([\w]+)\s*(?:=|;)",
            "javascript" or "typescript" => @"(?:const|let|var)\s+([\w]+)",
            "python" => @"([\w]+)\s*=",
            _ => @"(?:var|let|const)?\s*([\w]+)\s*(?:=|:)"
        };
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

    private int FindBlockEnd(int startLine)
    {
        int braceCount = 0;
        bool inBlock = false;
        
        for (int i = startLine - 1; i < _lines.Length; i++)
        {
            var line = _lines[i];
            foreach (var c in line)
            {
                if (c == '{')
                {
                    braceCount++;
                    inBlock = true;
                }
                else if (c == '}')
                {
                    braceCount--;
                    if (inBlock && braceCount == 0)
                    {
                        return i + 1;
                    }
                }
            }
        }
        
        return _lines.Length;
    }
}

/// <summary>
/// 依赖分析器
/// </summary>
public class DependencyAnalyzer
{
    public DependencyResult Analyze(string projectPath, string type)
    {
        var result = new DependencyResult();
        result.ProjectType = DetectProjectType(projectPath);
        
        if (type == "all" || type == result.ProjectType)
        {
            switch (result.ProjectType)
            {
                case "nuget":
                    AnalyzeNuGet(projectPath, result);
                    break;
                case "npm":
                    AnalyzeNpm(projectPath, result);
                    break;
                case "pip":
                    AnalyzePip(projectPath, result);
                    break;
            }
        }
        
        return result;
    }

    private string DetectProjectType(string projectPath)
    {
        if (File.Exists(Path.Combine(projectPath, "package.json"))) return "npm";
        if (File.Exists(Path.Combine(projectPath, "requirements.txt"))) return "pip";
        if (Directory.GetFiles(projectPath, "*.csproj").Length > 0) return "nuget";
        if (Directory.GetFiles(projectPath, "*.sln").Length > 0) return "nuget";
        return "unknown";
    }

    private void AnalyzeNuGet(string projectPath, DependencyResult result)
    {
        var csprojFiles = Directory.GetFiles(projectPath, "*.csproj", SearchOption.AllDirectories);
        
        foreach (var csproj in csprojFiles)
        {
            var content = File.ReadAllText(csproj);
            var matches = Regex.Matches(content, @"<PackageReference\s+Include=[""']([^'""]+)[""']\s+Version=[""']([^'""]+)[""']");
            
            foreach (Match match in matches)
            {
                result.Dependencies.Add(new DependencyInfo
                {
                    Name = match.Groups[1].Value,
                    Version = match.Groups[2].Value,
                    Type = "nuget"
                });
            }
        }
    }

    private void AnalyzeNpm(string projectPath, DependencyResult result)
    {
        var packageJsonPath = Path.Combine(projectPath, "package.json");
        if (!File.Exists(packageJsonPath)) return;

        var content = File.ReadAllText(packageJsonPath);
        var doc = JsonDocument.Parse(content);

        if (doc.RootElement.TryGetProperty("dependencies", out var deps))
        {
            foreach (var prop in deps.EnumerateObject())
            {
                result.Dependencies.Add(new DependencyInfo
                {
                    Name = prop.Name,
                    Version = prop.Value.GetString(),
                    Type = "npm"
                });
            }
        }

        if (doc.RootElement.TryGetProperty("devDependencies", out var devDeps))
        {
            foreach (var prop in devDeps.EnumerateObject())
            {
                result.DevDependencies.Add(new DependencyInfo
                {
                    Name = prop.Name,
                    Version = prop.Value.GetString(),
                    Type = "npm-dev"
                });
            }
        }
    }

    private void AnalyzePip(string projectPath, DependencyResult result)
    {
        var requirementsPath = Path.Combine(projectPath, "requirements.txt");
        if (!File.Exists(requirementsPath)) return;

        var lines = File.ReadAllLines(requirementsPath);
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#")) continue;

            var match = Regex.Match(trimmed, @"^([\w-]+)(?:[=<>!~]+(.+))?$");
            if (match.Success)
            {
                result.Dependencies.Add(new DependencyInfo
                {
                    Name = match.Groups[1].Value,
                    Version = match.Groups[2].Success ? match.Groups[2].Value : "latest",
                    Type = "pip"
                });
            }
        }
    }
}

/// <summary>
/// 复杂度分析器
/// </summary>
public class ComplexityAnalyzer
{
    private readonly string _language;
    private readonly string _code;
    private readonly string[] _lines;

    public ComplexityAnalyzer(string language, string code)
    {
        _language = language.ToLowerInvariant();
        _code = code;
        _lines = code.Split('\n');
    }

    public ComplexityResult Analyze()
    {
        var result = new ComplexityResult
        {
            LinesOfCode = CalculateLinesOfCode(),
            CyclomaticComplexity = CalculateCyclomaticComplexity(),
            CognitiveComplexity = CalculateCognitiveComplexity(),
            MaxNestingDepth = CalculateMaxNestingDepth(),
            Recommendations = GenerateRecommendations()
        };

        result.OverallScore = CalculateOverallScore(result);
        return result;
    }

    private int CalculateLinesOfCode()
    {
        return _lines.Count(line => !string.IsNullOrWhiteSpace(line) && !line.Trim().StartsWith("//") && !line.Trim().StartsWith("#"));
    }

    private int CalculateCyclomaticComplexity()
    {
        var complexity = 1;
        var patterns = GetDecisionPatterns();
        
        foreach (var pattern in patterns)
        {
            complexity += Regex.Matches(_code, pattern, RegexOptions.Multiline).Count;
        }
        
        return complexity;
    }

    private int CalculateCognitiveComplexity()
    {
        return CalculateCyclomaticComplexity(); // 简化处理
    }

    private int CalculateMaxNestingDepth()
    {
        int maxDepth = 0;
        int currentDepth = 0;

        foreach (var line in _lines)
        {
            foreach (var c in line)
            {
                if (c == '{')
                {
                    currentDepth++;
                    maxDepth = Math.Max(maxDepth, currentDepth);
                }
                else if (c == '}')
                {
                    currentDepth = Math.Max(0, currentDepth - 1);
                }
            }
        }

        return maxDepth;
    }

    private List<string> GenerateRecommendations()
    {
        var recommendations = new List<string>();
        var cyclomatic = CalculateCyclomaticComplexity();
        var nesting = CalculateMaxNestingDepth();
        var lines = CalculateLinesOfCode();

        if (cyclomatic > 10)
        {
            recommendations.Add($"圈复杂度过高 ({cyclomatic})，建议将复杂函数拆分为多个小函数");
        }
        if (nesting > 4)
        {
            recommendations.Add($"嵌套深度过大 ({nesting})，建议使用卫语句或提前返回减少嵌套");
        }
        if (lines > 300)
        {
            recommendations.Add($"代码文件过长 ({lines} 行)，建议按功能拆分为多个文件");
        }
        if (recommendations.Count == 0)
        {
            recommendations.Add("代码复杂度良好，继续保持");
        }

        return recommendations;
    }

    private double CalculateOverallScore(ComplexityResult result)
    {
        double score = 100;
        
        if (result.CyclomaticComplexity > 10)
        {
            score -= (result.CyclomaticComplexity - 10) * 2;
        }
        
        if (result.MaxNestingDepth > 4)
        {
            score -= (result.MaxNestingDepth - 4) * 5;
        }
        
        if (result.LinesOfCode > 300)
        {
            score -= (result.LinesOfCode - 300) / 10;
        }
        
        return Math.Max(0, Math.Min(100, score));
    }

    private string[] GetDecisionPatterns()
    {
        return _language switch
        {
            "csharp" or "java" or "cpp" or "c" => new[]
            {
                @"\bif\s*\(", @"\belse\s+if\s*\(", @"\bwhile\s*\(", @"\bfor\s*\(",
                @"\bforeach\s*\(", @"\bcase\s+", @"\bcatch\s*\(", @"\?\s*", @"\|\|", @"&&"
            },
            "python" => new[]
            {
                @"\bif\s+", @"\belif\s+", @"\bwhile\s+", @"\bfor\s+", @"\band\s+", @"\bor\s+"
            },
            _ => new[] { @"\bif\s*", @"\bwhile\s*", @"\bfor\s*", @"\bcase\s*" }
        };
    }
}

/// <summary>
/// 解析结果类
/// </summary>
public class ParseResult
{
    public List<FunctionInfo> Functions { get; set; } = new();
    public List<ClassInfo> Classes { get; set; } = new();
    public List<string> Imports { get; set; } = new();
    public List<VariableInfo> Variables { get; set; } = new();
}

public class FunctionInfo
{
    public string Name { get; set; } = string.Empty;
    public int LineStart { get; set; }
    public int LineEnd { get; set; }
    public string Signature { get; set; } = string.Empty;
}

public class ClassInfo
{
    public string Name { get; set; } = string.Empty;
    public int LineStart { get; set; }
    public int LineEnd { get; set; }
    public string Definition { get; set; } = string.Empty;
    public string? BaseClass { get; set; }
}

public class VariableInfo
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int Line { get; set; }
}

public class DependencyResult
{
    public string ProjectType { get; set; } = string.Empty;
    public List<DependencyInfo> Dependencies { get; set; } = new();
    public List<DependencyInfo> DevDependencies { get; set; } = new();
}

public class DependencyInfo
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}

public class ComplexityResult
{
    public double OverallScore { get; set; }
    public int CyclomaticComplexity { get; set; }
    public int CognitiveComplexity { get; set; }
    public int LinesOfCode { get; set; }
    public int MaxNestingDepth { get; set; }
    public List<string> Recommendations { get; set; } = new();
}