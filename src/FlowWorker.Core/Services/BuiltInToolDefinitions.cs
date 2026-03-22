using System.Text.Json;

namespace FlowWorker.Core.Services;

/// <summary>
/// 内置AI命令工具定义
/// 定义 ask_followup_question、attempt_completion、plan_mode_respond 等基础命令的元数据
/// </summary>
public static class BuiltInToolDefinitions
{
    /// <summary>
    /// 获取所有内置工具定义
    /// </summary>
    public static List<BuiltInToolDefinition> GetAllDefinitions()
    {
        return new List<BuiltInToolDefinition>
        {
            AskFollowupQuestion,
            AttemptCompletion,
            PlanModeRespond
        };
    }

    /// <summary>
    /// ask_followup_question 工具定义
    /// 向用户询问问题以获取额外信息
    /// </summary>
    public static BuiltInToolDefinition AskFollowupQuestion => new()
    {
        Name = "ask_followup_question",
        Description = "Ask the user a question to gather additional information needed to complete the task. This tool should be used when you encounter ambiguities, need clarification, or require more details to proceed effectively. It allows for interactive problem-solving by enabling direct communication with the user. Use this tool judiciously to maintain a balance between gathering necessary information and avoiding excessive back-and-forth.",
        Parameters = new List<BuiltInToolParameter>
        {
            new()
            {
                Name = "question",
                Description = "The question to ask the user. This should be a clear, specific question that addresses the information you need.",
                Type = "string",
                Required = true
            },
            new()
            {
                Name = "options",
                Description = "An array of 2-5 options for the user to choose from. Each option should be a string describing a possible answer. You may not always need to provide options, but it may be helpful in many cases where it can save the user from having to type out a response manually. IMPORTANT: NEVER include an option to toggle to Act mode, as this would be something you need to direct the user to do manually themselves if needed.",
                Type = "array",
                Required = false
            },
            new()
            {
                Name = "task_progress",
                Description = "A checklist showing task progress after this tool use is completed. The task_progress parameter must be included as a separate parameter inside of the parent tool call, it must be separate from other parameters such as content, arguments, etc.",
                Type = "string",
                Required = false
            }
        },
        Usage = @"<ask_followup_question>
<question>Your question here</question>
<options>Array of options here (optional), e.g. [""Option 1"", ""Option 2"", ""Option 3""]</options>
<task_progress>Checklist here (optional)</task_progress>
</ask_followup_question>"
    };

    /// <summary>
    /// attempt_completion 工具定义
    /// 完成任务并呈现结果给用户
    /// </summary>
    public static BuiltInToolDefinition AttemptCompletion => new()
    {
        Name = "attempt_completion",
        Description = "After each tool use, the user will respond with the result of that tool use, i.e. if it succeeded or failed, along with any reasons for failure. Once you've received the results of tool uses and can confirm that the task is complete, use this tool to present the result of your work to the user. Optionally you may provide a CLI command to showcase the result of your work. The user may respond with feedback if they are not satisfied with the result, which you can use to make improvements and try again. IMPORTANT NOTE: This tool CANNOT be used until you've confirmed from the user that any previous tool uses were successful. Failure to do so will result in code corruption and system failure. Before using this tool, you must ask yourself in <thinking></thinking> tags if you've confirmed from the user that any previous tool uses were successful. If not, then DO NOT use this tool. If you were using task_progress to update the task progress, you must include the completed list in the result as well.",
        Parameters = new List<BuiltInToolParameter>
        {
            new()
            {
                Name = "result",
                Description = "The result of the tool use. This should be a clear, specific description of the result.",
                Type = "string",
                Required = true
            },
            new()
            {
                Name = "command",
                Description = "A CLI command to execute to show a live demo of the result to the user. For example, use `open index.html` to display a created html website, or `open localhost:3000` to display a locally running development server. But DO NOT use commands like `echo` or `cat` that merely print text. This command should be valid for the current operating system. Ensure the command is properly formatted and does not contain any harmful instructions",
                Type = "string",
                Required = false
            },
            new()
            {
                Name = "task_progress",
                Description = "A checklist showing task progress after this tool use is completed.",
                Type = "string",
                Required = false
            }
        },
        Usage = @"<attempt_completion>
<result>Your final result description here</result>
<command>Your command here (optional)</command>
<task_progress>Checklist here (required if you used task_progress in previous tool uses)</task_progress>
</attempt_completion>"
    };

    /// <summary>
    /// plan_mode_respond 工具定义
    /// 在PLAN MODE下响应用户的询问
    /// </summary>
    public static BuiltInToolDefinition PlanModeRespond => new()
    {
        Name = "plan_mode_respond",
        Description = "Respond to the user's inquiry in an effort to plan a solution to the user's task. This tool should ONLY be used when you have already explored the relevant files and are ready to present a concrete plan. DO NOT use this tool to announce what files you're going to read - just read them first. This tool is only available in PLAN MODE. The environment_details will specify the current mode; if it is not PLAN_MODE then you should not use this tool. However, if while writing your response you realize you actually need to do more exploration before providing a complete plan, you can add the optional needs_more_exploration parameter to indicate this. This allows you to acknowledge that you should have done more exploration first, and signals that your next message will use exploration tools instead.",
        Parameters = new List<BuiltInToolParameter>
        {
            new()
            {
                Name = "response",
                Description = "The response to provide to the user. Do not try to use tools in this parameter, this is simply a chat response. (You MUST use the response parameter, do not simply place the response text directly within <plan_mode_respond> tags.)",
                Type = "string",
                Required = true
            },
            new()
            {
                Name = "needs_more_exploration",
                Description = "Set to true if while formulating your response that you found you need to do more exploration with tools, for example reading files. (Remember, you can explore the project with tools like read_file in PLAN MODE without the user having to toggle to ACT MODE.) Defaults to false if not specified.",
                Type = "boolean",
                Required = false
            },
            new()
            {
                Name = "task_progress",
                Description = "A checklist showing task progress after this tool use is completed.",
                Type = "string",
                Required = false
            }
        },
        Usage = @"<plan_mode_respond>
<response>Your response here</response>
<needs_more_exploration>true or false (optional, but you MUST set to true if in <response> you need to read files or use other exploration tools)</needs_more_exploration>
<task_progress>Checklist here (If you have presented the user with concrete steps or requirements, you can optionally include a todo list outlining these steps.)</task_progress>
</plan_mode_respond>"
    };

    /// <summary>
    /// 将工具定义转换为系统提示词中的工具描述格式
    /// </summary>
    public static string ToPromptDescription(this BuiltInToolDefinition definition)
    {
        var lines = new List<string>
        {
            $"## {definition.Name}",
            $"Description: {definition.Description}",
            "Parameters:"
        };

        foreach (var param in definition.Parameters)
        {
            var requiredText = param.Required ? "(required)" : "(optional)";
            lines.Add($"- {param.Name}: {requiredText} {param.Description}");
        }

        lines.Add("Usage:");
        lines.Add(definition.Usage);

        return string.Join("\n", lines);
    }

    /// <summary>
    /// 获取所有工具的系统提示词描述
    /// </summary>
    public static string GetAllToolsPromptDescription()
    {
        var definitions = GetAllDefinitions();
        var descriptions = definitions.Select(d => d.ToPromptDescription());
        return string.Join("\n\n", descriptions);
    }
}

/// <summary>
/// 内置工具定义
/// </summary>
public class BuiltInToolDefinition
{
    /// <summary>
    /// 工具名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 工具描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 工具参数列表
    /// </summary>
    public List<BuiltInToolParameter> Parameters { get; set; } = new();

    /// <summary>
    /// 工具使用示例
    /// </summary>
    public string Usage { get; set; } = string.Empty;
}

/// <summary>
/// 内置工具参数定义
/// </summary>
public class BuiltInToolParameter
{
    /// <summary>
    /// 参数名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 参数描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 参数类型
    /// </summary>
    public string Type { get; set; } = "string";

    /// <summary>
    /// 是否必需
    /// </summary>
    public bool Required { get; set; } = false;
}
