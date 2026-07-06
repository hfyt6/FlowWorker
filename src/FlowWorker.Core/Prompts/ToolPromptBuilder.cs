using System.Text;
using FlowWorker.Core.Interfaces;
using FlowWorker.Core.Services;

namespace FlowWorker.Core.Prompts;

/// <summary>
/// 工具提示词构建器
/// 根据角色允许的工具列表动态生成工具使用说明
/// </summary>
public static class ToolPromptBuilder
{
    /// <summary>
    /// 工具使用格式说明模板（通用）
    /// </summary>
    public const string ToolUseFormatIntro = @"
TOOL USE

You have access to a set of tools that are executed upon the user's approval. You can use one tool per message, and will receive the result of that tool use in the user's response. You use tools step-by-step to accomplish a given task, with each tool use informed by the result of the previous tool use.

# Tool Use Formatting

Tool use is formatted using XML-style tags. The tool name is enclosed in opening and closing tags, and each parameter is similarly enclosed within its own set of tags. Here's the structure:

<tool_name>
<parameter1_name>value1</parameter1_name>
<parameter2_name>value2</parameter2_name>
...
</tool_name>

Always adhere to this format for the tool use to ensure proper parsing and execution.

# Tools
";

    /// <summary>
    /// 工具使用指南模板（通用）
    /// </summary>
    public const string ToolUseGuidelines = @"
# Tool Use Guidelines

1. In <thinking> tags, assess what information you already have and what information you need to proceed with the task.
2. Choose the most appropriate tool based on the task and the tool descriptions provided.
3. If multiple actions are needed, use one tool at a time per message to accomplish the task iteratively.
4. Formulate your tool use using the XML format specified for each tool.
5. After each tool use, the user will respond with the result of that tool use.
6. ALWAYS wait for user confirmation after each tool use before proceeding.

# Tool Use Examples

## Example 1: Using a tool

<tool_name>
<param1>value1</param1>
<param2>value2</param2>
</tool_name>

## Example 2: Completing a task

<attempt_completion>
<result>Your final result description here</result>
</attempt_completion>
";

    /// <summary>
    /// 获取所有内置工具的描述模板
    /// </summary>
    public static Dictionary<string, string> GetToolDescriptions()
    {
        return new Dictionary<string, string>
        {
            ["Filesystem"] = GetFilesystemToolDescription(),
            ["Text"] = GetTextToolDescription(),
            ["Calculator"] = GetCalculatorToolDescription(),
            ["Network"] = GetNetworkToolDescription(),
            ["Process"] = GetProcessToolDescription(),
            ["CodeAnalysis"] = GetCodeAnalysisToolDescription(),
            ["CodeManipulation"] = GetCodeManipulationToolDescription(),
            ["VersionControl"] = GetVersionControlToolDescription()
        };
    }

    /// <summary>
    /// 根据允许的工具列表生成工具提示词
    /// </summary>
    /// <param name="allowedTools">允许使用的工具名称列表</param>
    /// <returns>完整的工具使用说明提示词</returns>
    public static string BuildToolPrompt(List<string>? allowedTools)
    {
        if (allowedTools == null || allowedTools.Count == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        sb.AppendLine(ToolUseFormatIntro);

        var allDescriptions = GetToolDescriptions();

        foreach (var toolName in allowedTools)
        {
            if (allDescriptions.TryGetValue(toolName, out var description))
            {
                sb.AppendLine(description);
            }
        }

        sb.AppendLine(ToolUseGuidelines);

        return sb.ToString();
    }

    /// <summary>
    /// 获取文件系统工具描述
    /// </summary>
    private static string GetFilesystemToolDescription()
    {
        return @"## Filesystem

Description: File operations including reading, writing, listing files, and directory management.

### Actions

#### read_file
Read the contents of a file at the specified path.
- Parameters:
  - `path` (required): The path of the file to read
  - `working_directory` (optional): The working directory for relative paths

#### write_file
Write content to a file at the specified path. Creates directories if needed.
- Parameters:
  - `path` (required): The path of the file to write
  - `content` (required): The content to write
  - `working_directory` (optional): The working directory for relative paths

#### list_files
List files and directories within the specified directory.
- Parameters:
  - `path` (required): The directory path to list
  - `recursive` (optional): Whether to list recursively (true/false)
  - `working_directory` (optional): The working directory for relative paths

#### file_exists
Check if a file exists at the specified path.
- Parameters:
  - `path` (required): The file path to check

#### delete_file
Delete a file at the specified path.
- Parameters:
  - `path` (required): The file path to delete

#### create_directory
Create a directory at the specified path.
- Parameters:
  - `path` (required): The directory path to create
";
    }

    /// <summary>
    /// 获取文本处理工具描述
    /// </summary>
    private static string GetTextToolDescription()
    {
        return @"## Text

Description: Text processing tools including search, replace, line counting, and JSON formatting.

### Actions

#### search_text
Search text using regular expressions.
- Parameters:
  - `text` (required): The text to search in
  - `pattern` (required): The regex pattern to search for
  - `case_sensitive` (optional): Whether search is case sensitive (default: true)
  - `max_results` (optional): Maximum number of results to return (default: 100)

#### replace_text
Replace text using regex or literal string replacement.
- Parameters:
  - `text` (required): The text to perform replacement on
  - `pattern` (required): The pattern to match
  - `replacement` (required): The replacement string
  - `is_regex` (optional): Whether pattern is regex (default: false)
  - `replace_all` (optional): Whether to replace all occurrences (default: true)

#### count_lines
Count lines, words, and characters in text.
- Parameters:
  - `text` (required): The text to analyze

#### format_json
Format and beautify JSON string.
- Parameters:
  - `json_string` (required): The JSON string to format
  - `indent_size` (optional): Indentation size (default: 2)
  - `sort_keys` (optional): Whether to sort keys (default: false)

#### validate_json
Validate if a string is valid JSON.
- Parameters:
  - `json_string` (required): The JSON string to validate
";
    }

    /// <summary>
    /// 获取计算器工具描述
    /// </summary>
    private static string GetCalculatorToolDescription()
    {
        return @"## Calculator

Description: Mathematical calculation and unit conversion tools.

### Actions

#### calculate
Evaluate a mathematical expression.
- Parameters:
  - `expression` (required): The mathematical expression to evaluate

#### convert_unit
Convert between different units.
- Parameters:
  - `value` (required): The value to convert
  - `from_unit` (required): The source unit
  - `to_unit` (required): The target unit

#### generate_uuid
Generate a UUID.
- Parameters:
  - `format` (optional): The UUID format (default, short, etc.)

#### get_timestamp
Get the current timestamp.
- Parameters: (none)
";
    }

    /// <summary>
    /// 获取网络工具描述
    /// </summary>
    private static string GetNetworkToolDescription()
    {
        return @"## Network

Description: Network utilities including HTTP requests, DNS resolution, and port checking.

### Actions

#### http_request
Send an HTTP request.
- Parameters:
  - `url` (required): The URL to request
  - `method` (optional): HTTP method (GET, POST, etc.)
  - `headers` (optional): Request headers
  - `body` (optional): Request body

#### download_file
Download a file from a URL.
- Parameters:
  - `url` (required): The URL to download from
  - `output_path` (required): The local path to save the file

#### ping_host
Ping a host to check connectivity.
- Parameters:
  - `host` (required): The host to ping

#### resolve_dns
Resolve DNS for a domain.
- Parameters:
  - `domain` (required): The domain to resolve

#### check_port
Check if a port is open on a host.
- Parameters:
  - `host` (required): The host to check
  - `port` (required): The port number to check
";
    }

    /// <summary>
    /// 获取进程工具描述
    /// </summary>
    private static string GetProcessToolDescription()
    {
        return @"## Process

Description: Process management and command execution tools.

### Actions

#### execute_command
Execute a shell command.
- Parameters:
  - `command` (required): The command to execute
  - `working_directory` (optional): The working directory
  - `timeout_ms` (optional): Timeout in milliseconds

#### run_process
Start a new process.
- Parameters:
  - `command` (required): The command to run
  - `arguments` (optional): Command arguments

#### kill_process
Kill a running process.
- Parameters:
  - `pid` (required): The process ID to kill

#### list_processes
List all running processes.
- Parameters:
  - `filter` (optional): Filter by process name
  - `include_system` (optional): Include system processes (default: false)

#### get_process_info
Get detailed information about a process.
- Parameters:
  - `pid` (required): The process ID
";
    }

    /// <summary>
    /// 获取代码分析工具描述
    /// </summary>
    private static string GetCodeAnalysisToolDescription()
    {
        return @"## CodeAnalysis

Description: Code analysis tools including parsing, function/class finding, and dependency analysis.

### Actions

#### parse_code
Parse code and extract structure information.
- Parameters:
  - `code` (required): The code to parse
  - `language` (required): The programming language

#### find_function
Find functions in code by name or pattern.
- Parameters:
  - `code` (required): The code to search
  - `pattern` (required): The function name or pattern

#### find_class
Find classes in code by name or pattern.
- Parameters:
  - `code` (required): The code to search
  - `pattern` (required): The class name or pattern

#### get_dependencies
Analyze code dependencies.
- Parameters:
  - `project_path` (required): The project path
  - `type` (optional): Dependency type (nuget, npm, pip)

#### analyze_complexity
Analyze code complexity.
- Parameters:
  - `code` (required): The code to analyze
";
    }

    /// <summary>
    /// 获取代码操作工具描述
    /// </summary>
    private static string GetCodeManipulationToolDescription()
    {
        return @"## CodeManipulation

Description: Code manipulation tools including refactoring, formatting, and code generation.

### Actions

#### refactor_code
Refactor code (rename, extract method, etc.).
- Parameters:
  - `code` (required): The code to refactor
  - `operation` (required): The operation (rename, extract, inline)
  - `target` (required): The target name or location

#### format_code
Format code according to language conventions.
- Parameters:
  - `code` (required): The code to format
  - `language` (optional): The programming language

#### generate_code
Generate code from template or description.
- Parameters:
  - `description` (required): Description of what to generate
  - `template_type` (required): Type of template (class, function)

#### transform_code
Transform code between languages.
- Parameters:
  - `code` (required): The code to transform
  - `from_language` (required): Source language
  - `to_language` (required): Target language
";
    }

    /// <summary>
    /// 获取版本控制工具描述
    /// </summary>
    private static string GetVersionControlToolDescription()
    {
        return @"## VersionControl

Description: Git version control operations.

### Actions

#### git_status
Get the current git status.
- Parameters: (none)

#### git_diff
Show differences between commits or working tree.
- Parameters:
  - `commit1` (optional): First commit reference
  - `commit2` (optional): Second commit reference
  - `file_path` (optional): Specific file to diff

#### git_log
View commit history.
- Parameters:
  - `limit` (optional): Number of commits to show (default: 10)
  - `branch` (optional): Branch name

#### git_branch
List, create, or switch branches.
- Parameters:
  - `action` (required): The action (list, create, switch, delete)
  - `branch_name` (optional): Branch name for create/switch/delete

#### git_commit
Create a new commit.
- Parameters:
  - `message` (required): Commit message
  - `files` (optional): Files to commit
  - `all` (optional): Commit all changes (default: false)
";
    }
}