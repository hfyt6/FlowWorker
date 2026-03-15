using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FlowWorker.Plugins.CodeAnalysis
{
    /// <summary>
    /// 代码分析工具处理器
    /// </summary>
    public class CodeAnalysisHandler
    {
        /// <summary>
        /// 解析代码，提取代码结构信息
        /// </summary>
        public Task<ToolResponse> ParseCodeAsync(JsonElement parameters)
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
                return Task.FromResult(ToolResponse.Error("EXECUTION_FAILED", $"代码解析失败: {ex.Message}"));
            }
        }

        /// <summary>
        /// 在代码中查找函数定义
        /// </summary>
        public Task<ToolResponse> FindFunctionAsync(JsonElement parameters)
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
                return Task.FromResult(ToolResponse.Error("EXECUTION_FAILED", $"查找函数失败: {ex.Message}"));
            }
        }

        /// <summary>
        /// 在代码中查找类定义
        /// </summary>
        public Task<ToolResponse> FindClassAsync(JsonElement parameters)
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
                return Task.FromResult(ToolResponse.Error("EXECUTION_FAILED", $"查找类失败: {ex.Message}"));
            }
        }

        /// <summary>
        /// 获取项目的依赖列表
        /// </summary>
        public Task<ToolResponse> GetDependenciesAsync(JsonElement parameters)
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
                    return Task.FromResult(ToolResponse.Error("PROJECT_NOT_FOUND", $"项目路径不存在: {projectPath}"));
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
                return Task.FromResult(ToolResponse.Error("EXECUTION_FAILED", $"获取依赖失败: {ex.Message}"));
            }
        }

        /// <summary>
        /// 分析代码复杂度
        /// </summary>
        public Task<ToolResponse> AnalyzeComplexityAsync(JsonElement parameters)
        {
            try
            {
                var code = parameters.GetProperty("code").GetString();
                var language = parameters.TryGetProperty("language", out var langProp) 
                    ? langProp.GetString()?.ToLowerInvariant() 
                    : null;
                var metrics = parameters.TryGetProperty("metrics", out var metricsProp)
                    ? metricsProp.EnumerateArray().Select(m => m.GetString()).ToList()
                    : new List<string> { "cyclomatic", "cognitive", "lines", "nesting" };

                if (string.IsNullOrWhiteSpace(code))
                {
                    return Task.FromResult(ToolResponse.Error("INVALID_PARAMETERS", "代码内容不能为空"));
                }

                var analyzer = new ComplexityAnalyzer(language ?? "csharp", code, metrics);
                var result = analyzer.Analyze();

                return Task.FromResult(ToolResponse.Success(new
                {
                    overall_score = result.OverallScore,
                    cyclomatic_complexity = result.CyclomaticComplexity,
                    cognitive_complexity = result.CognitiveComplexity,
                    lines_of_code = result.LinesOfCode,
                    max_nesting_depth = result.MaxNestingDepth,
                    function_complexities = result.FunctionComplexities,
                    recommendations = result.Recommendations
                }));
            }
            catch (Exception ex)
            {
                return Task.FromResult(ToolResponse.Error("EXECUTION_FAILED", $"复杂度分析失败: {ex.Message}"));
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
                        Signature = match.Value.Trim(),
                        Body = ExtractBlockBody(lineNumber)
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
                    Body = ExtractBlockBody(lineNumber),
                    BaseClass = match.Groups.Count > 2 ? match.Groups[2].Value : null,
                    Interfaces = match.Groups.Count > 3 ? match.Groups[3].Value.Split(',').Select(s => s.Trim()).ToList() : new List<string>()
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
                    @"(?:const|let|var)\s+([\w]+)\s*=\s*(?:async\s+)?\([^)]*\)\s*=>",
                    @"([\w]+)\s*\([^)]*\)\s*{",
                    @"([\w]+)\s*:\s*(?:async\s+)?\([^)]*\)\s*=>"
                },
                "python" => new[]
                {
                    @"def\s+([\w]+)\s*\([^)]*\):"
                },
                "java" => new[]
                {
                    @"(?:public|private|protected|static|final|abstract|\s)+\s+(?:[\w<>,\s]+)\s+([\w]+)\s*\([^)]*\)\s*{",
                    @"(?:public|private|protected|static|final|abstract|\s)+\s+(?:[\w<>,\s]+)\s+([\w]+)\s*\([^)]*\)\s*throws[^}]*{"
                },
                "cpp" or "c" => new[]
                {
                    @"(?:[\w\*\s]+)\s+([\w]+)\s*\([^)]*\)\s*{",
                    @"(?:[\w\*\s]+)\s+([\w]+)\s*\([^)]*\)\s*;"
                },
                "go" => new[]
                {
                    @"func\s+(?:\([^)]*\)\s+)?([\w]+)\s*\([^)]*\)"
                },
                "rust" => new[]
                {
                    @"fn\s+([\w]+)\s*\([^)]*\)"
                },
                _ => new[] { @"(?:function|def|fn|func)\s+([\w]+)\s*\(" }
            };
        }

        private string GetClassPattern()
        {
            return _language switch
            {
                "csharp" => @"(?:public|private|protected|internal|static|abstract|sealed|partial|\s)+\s*class\s+([\w]+)(?:\s*:\s*([\w]+))?(?:\s*,\s*([\w,\s]+))?",
                "java" => @"(?:public|private|protected|static|abstract|final|\s)+\s*class\s+([\w]+)(?:\s+extends\s+([\w]+))?(?:\s+implements\s+([\w,\s]+))?",
                "cpp" => @"(?:class|struct)\s+([\w]+)(?:\s*:\s*(?:public|private|protected)\s+([\w]+))?",
                "python" => @"class\s+([\w]+)(?:\s*\(\s*([\w]+)\s*\))?",
                "typescript" => @"(?:export\s+)?(?:abstract\s+)?class\s+([\w]+)(?:\s+extends\s+([\w]+))?(?:\s+implements\s+([\w,\s]+))?",
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
                "javascript" or "typescript" => new[] { @"import\s+.*?\s+from\s+['""]([^'""]+)['""]", @"require\s*\(\s*['""]([^'""]+)['""]\s*\)" },
                "go" => new[] { @"import\s+['""]([^'""]+)['""]" },
                "rust" => new[] { @"use\s+([\w:]+);", @"extern\s+crate\s+([\w]+);" },
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
                "cpp" or "c" => @"(?:[\w\*\s]+)\s+([\w]+)\s*(?:=|;)",
                "go" => @"(?:var)?\s*([\w]+)\s+(?:[\w\*\[\]]+)",
                "rust" => @"(?:let\s+mut|let)\s+([\w]+)",
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
                
                // Python uses indentation
                if (_language == "python" && inBlock && string.IsNullOrWhiteSpace(line))
                {
                    return i;
                }
            }
            
            return _lines.Length;
        }

        private string ExtractBlockBody(int startLine)
        {
            var endLine = FindBlockEnd(startLine);
            var bodyLines = new List<string>();
            
            for (int i = startLine - 1; i < endLine && i < _lines.Length; i++)
            {
                bodyLines.Add(_lines[i]);
            }
            
            return string.Join("\n", bodyLines);
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
            
            // 检测项目类型
            result.ProjectType = DetectProjectType(projectPath);
            
            // 根据项目类型和请求类型分析依赖
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
                    case "maven":
                        AnalyzeMaven(projectPath, result);
                        break;
                    case "cargo":
                        AnalyzeCargo(projectPath, result);
                        break;
                }
            }
            
            return result;
        }

        private string DetectProjectType(string projectPath)
        {
            if (File.Exists(Path.Combine(projectPath, "package.json"))) return "npm";
            if (File.Exists(Path.Combine(projectPath, "requirements.txt"))) return "pip";
            if (File.Exists(Path.Combine(projectPath, "Cargo.toml"))) return "cargo";
            if (File.Exists(Path.Combine(projectPath, "pom.xml"))) return "maven";
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

        private void AnalyzeMaven(string projectPath, DependencyResult result)
        {
            var pomPath = Path.Combine(projectPath, "pom.xml");
            if (!File.Exists(pomPath)) return;

            var content = File.ReadAllText(pomPath);
            var matches = Regex.Matches(content, @"<dependency>.*?<groupId>([^<]+)</groupId>.*?<artifactId>([^<]+)</artifactId>.*?<version>([^<]+)</version>.*?</dependency>", RegexOptions.Singleline);

            foreach (Match match in matches)
            {
                result.Dependencies.Add(new DependencyInfo
                {
                    Name = $"{match.Groups[1].Value}:{match.Groups[2].Value}",
                    Version = match.Groups[3].Value,
                    Type = "maven"
                });
            }
        }

        private void AnalyzeCargo(string projectPath, DependencyResult result)
        {
            var cargoPath = Path.Combine(projectPath, "Cargo.toml");
            if (!File.Exists(cargoPath)) return;

            var content = File.ReadAllText(cargoPath);
            var depsMatch = Regex.Match(content, @"\[dependencies\](.*?)(?:\[|$)", RegexOptions.Singleline);
            
            if (depsMatch.Success)
            {
                var depsContent = depsMatch.Groups[1].Value;
                var depMatches = Regex.Matches(depsContent, @"([\w-]+)\s*=\s*[\"']([^\"']+)[\"']");
                
                foreach (Match match in depMatches)
                {
                    result.Dependencies.Add(new DependencyInfo
                    {
                        Name = match.Groups[1].Value,
                        Version = match.Groups[2].Value,
                        Type = "cargo"
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
        private readonly List<string> _metrics;
        private readonly string[] _lines;

        public ComplexityAnalyzer(string language, string code, List<string> metrics)
        {
            _language = language.ToLowerInvariant();
            _code = code;
            _metrics = metrics;
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
                FunctionComplexities = AnalyzeFunctionComplexities(),
                Recommendations = GenerateRecommendations()
            };

            // 计算整体评分 (0-100，越高越好)
            result.OverallScore = CalculateOverallScore(result);

            return result;
        }

        private int CalculateLinesOfCode()
        {
            return _lines.Count(line => !string.IsNullOrWhiteSpace(line) && !line.Trim().StartsWith("//") && !line.Trim().StartsWith("#") && !line.Trim().StartsWith("/*") && !line.Trim().StartsWith("*"));
        }

        private int CalculateCyclomaticComplexity()
        {
            var complexity = 1; // 基础复杂度
            var patterns = GetDecisionPatterns();
            
            foreach (var pattern in patterns)
            {
                complexity += Regex.Matches(_code, pattern, RegexOptions.Multiline).Count;
            }
            
            return complexity;
        }

        private int CalculateCognitiveComplexity()
        {
            var complexity = 0;
            var patterns = GetCognitivePatterns();
            
            foreach (var pattern in patterns)
            {
                var matches = Regex.Matches(_code, pattern, RegexOptions.Multiline);
                foreach (Match match in matches)
                {
                    complexity += CalculateMatchComplexity(match);
                }
            }
            
            return complexity;
        }

        private int CalculateMaxNestingDepth()
        {
            int maxDepth = 0;
            int currentDepth = 0;
            bool inString = false;
            char stringChar = '\0';

            foreach (var line in _lines)
            {
                foreach (var c in line)
                {
                    if (!inString && (c == '"' || c == '\'' || c == '`'))
                    {
                        inString = true;
                        stringChar = c;
                    }
                    else if (inString && c == stringChar)
                    {
                        inString = false;
                    }
                    else if (!inString)
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
            }

            return maxDepth;
        }

        private List<FunctionComplexity> AnalyzeFunctionComplexities()
        {
            var complexities = new List<FunctionComplexity>();
            var parser = new CodeParser(_language, _code);
            var functions = parser.ExtractFunctions();

            foreach (var function in functions)
            {
                var functionCode = ExtractFunctionCode(function.LineStart, function.LineEnd);
                var functionAnalyzer = new ComplexityAnalyzer(_language, functionCode, _metrics);
                var functionResult = functionAnalyzer.Analyze();

                complexities.Add(new FunctionComplexity
                {
                    Name = function.Name,
                    LineStart = function.LineStart,
                    Cyclomatic = functionResult.CyclomaticComplexity,
                    Cognitive = functionResult.CognitiveComplexity,
                    Lines = functionResult.LinesOfCode,
                    NestingDepth = functionResult.MaxNestingDepth
                });
            }

            return complexities;
        }

        private List<string> GenerateRecommendations()
        {
            var recommendations = new List<string>();
            var cyclomatic = CalculateCyclomaticComplexity();
            var cognitive = CalculateCognitiveComplexity();
            var nesting = CalculateMaxNestingDepth();
            var lines = CalculateLinesOfCode();

            if (cyclomatic > 10)
            {
                recommendations.Add($"圈复杂度过高 ({cyclomatic})，建议将复杂函数拆分为多个小函数");
            }
            if (cognitive > 15)
            {
                recommendations.Add($"认知复杂度过高 ({cognitive})，建议简化逻辑结构，减少嵌套");
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
            
            // 圈复杂度扣分
            if (result.CyclomaticComplexity > 10)
            {
                score -= (result.CyclomaticComplexity - 10) * 2;
            }
            
            // 认知复杂度扣分
            if (result.CognitiveComplexity > 15)
            {
                score -= (result.CognitiveComplexity - 15) * 1.5;
            }
            
            // 嵌套深度扣分
            if (result.MaxNestingDepth > 4)
            {
                score -= (result.MaxNestingDepth - 4) * 5;
            }
            
            // 代码行数扣分
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
                    @"\bif\s+", @"\belif\s+", @"\bwhile\s+", @"\bfor\s+",
                    @"\bexcept\s*", @"\band\s+", @"\bor\s+"
                },
                "javascript" or "typescript" => new[]
                {
                    @"\bif\s*\(", @"\belse\s+if\s*\(", @"\bwhile\s*\(", @"\bfor\s*\(",
                    @"\bforeach\s*\(", @"\bswitch\s*\(", @"\bcase\s+", @"\bcatch\s*\(",
                    @"\?\s*", @"\|\|", @"&&"
                },
                _ => new[] { @"\bif\s*", @"\bwhile\s*", @"\bfor\s*", @"\bcase\s*" }
            };
        }

        private string[] GetCognitivePatterns()
        {
            return GetDecisionPatterns();
        }

        private int CalculateMatchComplexity(Match match)
        {
            // 根据嵌套深度增加复杂度
            var lineNumber = GetLineNumber(match.Index);
            var nestingAtLine = CalculateNestingAtLine(lineNumber);
            return 1 + nestingAtLine;
        }

        private int CalculateNestingAtLine(int lineNumber)
        {
            int depth = 0;
            for (int i = 0; i < lineNumber - 1 && i < _lines.Length; i++)
            {
                depth += _lines[i].Count(c => c == '{');
                depth -= _lines[i].Count(c => c == '}');
                depth = Math.Max(0, depth);
            }
            return depth;
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

        private string ExtractFunctionCode(int startLine, int endLine)
        {
            var lines = new List<string>();
            for (int i = startLine - 1; i < endLine && i < _lines.Length; i++)
            {
                lines.Add(_lines[i]);
            }
            return string.Join("\n", lines);
        }
    }

    // 数据模型类
    public class ParseResult
    {
        public List<FunctionInfo> Functions { get; set; } = new();
        public List<ClassInfo> Classes { get; set; } = new();
        public List<string> Imports { get; set; } = new();
        public List<VariableInfo> Variables { get; set; } = new();
    }

    public class FunctionInfo
    {
        public string Name { get; set; }
        public int LineStart { get; set; }
        public int LineEnd { get; set; }
        public string Signature { get; set; }
        public string Body { get; set; }
        public List<ParameterInfo> Parameters { get; set; } = new();
        public string ReturnType { get; set; }
        public List<string> Modifiers { get; set; } = new();
    }

    public class ParameterInfo
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public object DefaultValue { get; set; }
    }

    public class ClassInfo
    {
        public string Name { get; set; }
        public int LineStart { get; set; }
        public int LineEnd { get; set; }
        public string Definition { get; set; }
        public string Body { get; set; }
        public string BaseClass { get; set; }
        public List<string> Interfaces { get; set; } = new();
        public List<FunctionInfo> Methods { get; set; } = new();
        public List<PropertyInfo> Properties { get; set; } = new();
    }

    public class PropertyInfo
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public List<string> Modifiers { get; set; } = new();
    }

    public class VariableInfo
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public int Line { get; set; }
    }

    public class DependencyResult
    {
        public string ProjectType { get; set; }
        public List<DependencyInfo> Dependencies { get; set; } = new();
        public List<DependencyInfo> DevDependencies { get; set; } = new();
    }

    public class DependencyInfo
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public string Type { get; set; }
    }

    public class ComplexityResult
    {
        public double OverallScore { get; set; }
        public int CyclomaticComplexity { get; set; }
        public int CognitiveComplexity { get; set; }
        public int LinesOfCode { get; set; }
        public int MaxNestingDepth { get; set; }
        public List<FunctionComplexity> FunctionComplexities { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
    }

    public class FunctionComplexity
    {
        public string Name { get; set; }
        public int LineStart { get; set; }
        public int Cyclomatic { get; set; }
        public int Cognitive { get; set; }
        public int Lines { get; set; }
        public int NestingDepth { get; set; }
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
