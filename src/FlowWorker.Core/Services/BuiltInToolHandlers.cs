using System.Text.Json;

namespace FlowWorker.Core.Services;

/// <summary>
/// 内置AI命令工具处理器
/// 处理 ask_followup_question、attempt_completion、plan_mode_respond 等基础命令
/// </summary>
public class BuiltInToolHandlers
{
    /// <summary>
    /// 处理 ask_followup_question 工具调用
    /// 向用户询问问题以获取额外信息
    /// </summary>
    public Task<BuiltInToolResponse> AskFollowupQuestionAsync(JsonElement parameters)
    {
        try
        {
            // 获取必需参数 question
            if (!parameters.TryGetProperty("question", out var questionElement) ||
                string.IsNullOrWhiteSpace(questionElement.GetString()))
            {
                return Task.FromResult(BuiltInToolResponse.Error("INVALID_PARAMETERS", "缺少必需参数: question"));
            }

            var question = questionElement.GetString()!;

            // 获取可选参数 options
            List<string>? options = null;
            if (parameters.TryGetProperty("options", out var optionsElement) &&
                optionsElement.ValueKind == JsonValueKind.Array)
            {
                options = new List<string>();
                foreach (var option in optionsElement.EnumerateArray())
                {
                    var optionValue = option.GetString();
                    if (!string.IsNullOrWhiteSpace(optionValue))
                    {
                        options.Add(optionValue);
                    }
                }
            }

            // 获取可选参数 task_progress
            string? taskProgress = null;
            if (parameters.TryGetProperty("task_progress", out var taskProgressElement))
            {
                taskProgress = taskProgressElement.GetString();
            }

            // 构建响应数据
            var responseData = new Dictionary<string, object>
            {
                ["question"] = question,
                ["type"] = "ask_followup_question"
            };

            if (options != null && options.Count > 0)
            {
                responseData["options"] = options;
            }

            if (!string.IsNullOrWhiteSpace(taskProgress))
            {
                responseData["task_progress"] = taskProgress;
            }

            return Task.FromResult(BuiltInToolResponse.Success(responseData));
        }
        catch (Exception ex)
        {
            return Task.FromResult(BuiltInToolResponse.Error("EXECUTION_FAILED", ex.Message));
        }
    }

    /// <summary>
    /// 处理 attempt_completion 工具调用
    /// 完成任务并呈现结果给用户
    /// </summary>
    public Task<BuiltInToolResponse> AttemptCompletionAsync(JsonElement parameters)
    {
        try
        {
            // 获取必需参数 result
            if (!parameters.TryGetProperty("result", out var resultElement) ||
                string.IsNullOrWhiteSpace(resultElement.GetString()))
            {
                return Task.FromResult(BuiltInToolResponse.Error("INVALID_PARAMETERS", "缺少必需参数: result"));
            }

            var result = resultElement.GetString()!;

            // 获取可选参数 command
            string? command = null;
            if (parameters.TryGetProperty("command", out var commandElement))
            {
                command = commandElement.GetString();
            }

            // 获取可选参数 task_progress
            string? taskProgress = null;
            if (parameters.TryGetProperty("task_progress", out var taskProgressElement))
            {
                taskProgress = taskProgressElement.GetString();
            }

            // 构建响应数据
            var responseData = new Dictionary<string, object>
            {
                ["result"] = result,
                ["type"] = "attempt_completion"
            };

            if (!string.IsNullOrWhiteSpace(command))
            {
                responseData["command"] = command;
            }

            if (!string.IsNullOrWhiteSpace(taskProgress))
            {
                responseData["task_progress"] = taskProgress;
            }

            return Task.FromResult(BuiltInToolResponse.Success(responseData));
        }
        catch (Exception ex)
        {
            return Task.FromResult(BuiltInToolResponse.Error("EXECUTION_FAILED", ex.Message));
        }
    }

    /// <summary>
    /// 处理 plan_mode_respond 工具调用
    /// 在PLAN MODE下响应用户的询问
    /// </summary>
    public Task<BuiltInToolResponse> PlanModeRespondAsync(JsonElement parameters)
    {
        try
        {
            // 获取必需参数 response
            if (!parameters.TryGetProperty("response", out var responseElement) ||
                string.IsNullOrWhiteSpace(responseElement.GetString()))
            {
                return Task.FromResult(BuiltInToolResponse.Error("INVALID_PARAMETERS", "缺少必需参数: response"));
            }

            var response = responseElement.GetString()!;

            // 获取可选参数 needs_more_exploration
            bool needsMoreExploration = false;
            if (parameters.TryGetProperty("needs_more_exploration", out var explorationElement))
            {
                needsMoreExploration = explorationElement.GetBoolean();
            }

            // 获取可选参数 task_progress
            string? taskProgress = null;
            if (parameters.TryGetProperty("task_progress", out var taskProgressElement))
            {
                taskProgress = taskProgressElement.GetString();
            }

            // 构建响应数据
            var responseData = new Dictionary<string, object>
            {
                ["response"] = response,
                ["type"] = "plan_mode_respond",
                ["needs_more_exploration"] = needsMoreExploration
            };

            if (!string.IsNullOrWhiteSpace(taskProgress))
            {
                responseData["task_progress"] = taskProgress;
            }

            return Task.FromResult(BuiltInToolResponse.Success(responseData));
        }
        catch (Exception ex)
        {
            return Task.FromResult(BuiltInToolResponse.Error("EXECUTION_FAILED", ex.Message));
        }
    }
}

/// <summary>
/// 内置工具响应类
/// </summary>
public class BuiltInToolResponse
{
    public string Status { get; set; } = string.Empty;
    public object? Data { get; set; }
    public BuiltInToolError? ErrorInfo { get; set; }
    public long ExecutionTime { get; set; }

    public static BuiltInToolResponse Success(object data)
    {
        return new BuiltInToolResponse
        {
            Status = "success",
            Data = data
        };
    }

    public static BuiltInToolResponse Error(string code, string message)
    {
        return new BuiltInToolResponse
        {
            Status = "error",
            ErrorInfo = new BuiltInToolError { Code = code, Message = message }
        };
    }
}

/// <summary>
/// 内置工具错误类
/// </summary>
public class BuiltInToolError
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
