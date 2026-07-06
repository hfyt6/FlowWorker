using System.Text.Json;
using FlowWorker.Core.Interfaces;
using FlowWorker.Core.Services;

namespace FlowWorker.Core.Tools.BuiltIn;

/// <summary>
/// 内置AI命令工具处理器
/// 处理 ask_followup_question、attempt_completion、plan_mode_respond 等基础命令
/// </summary>
public class BuiltInTool : IToolHandler
{
    private readonly BuiltInToolHandlers _handlers;

    public BuiltInTool()
    {
        _handlers = new BuiltInToolHandlers();
    }

    public string Name => "BuiltIn";

    public IReadOnlyList<string> SupportedActions => new[]
    {
        "ask_followup_question",
        "attempt_completion",
        "plan_mode_respond"
    };

    public async Task<ToolResponse> ExecuteAsync(string action, JsonElement parameters)
    {
        return action.ToLowerInvariant() switch
        {
            "ask_followup_question" => await AskFollowupQuestionAsync(parameters),
            "attempt_completion" => await AttemptCompletionAsync(parameters),
            "plan_mode_respond" => await PlanModeRespondAsync(parameters),
            _ => ToolResponse.Error("UNKNOWN_ACTION", $"未知的操作：{action}")
        };
    }

    /// <summary>
    /// 处理 ask_followup_question 工具调用
    /// </summary>
    private async Task<ToolResponse> AskFollowupQuestionAsync(JsonElement parameters)
    {
        var result = await _handlers.AskFollowupQuestionAsync(parameters);
        return ConvertToToolResponse(result);
    }

    /// <summary>
    /// 处理 attempt_completion 工具调用
    /// </summary>
    private async Task<ToolResponse> AttemptCompletionAsync(JsonElement parameters)
    {
        var result = await _handlers.AttemptCompletionAsync(parameters);
        return ConvertToToolResponse(result);
    }

    /// <summary>
    /// 处理 plan_mode_respond 工具调用
    /// </summary>
    private async Task<ToolResponse> PlanModeRespondAsync(JsonElement parameters)
    {
        var result = await _handlers.PlanModeRespondAsync(parameters);
        return ConvertToToolResponse(result);
    }

    /// <summary>
    /// 将 BuiltInToolResponse 转换为 ToolResponse
    /// </summary>
    private static ToolResponse ConvertToToolResponse(BuiltInToolResponse response)
    {
        if (response.Status == "success")
        {
            return ToolResponse.Success(response.Data ?? new object());
        }
        else
        {
            return ToolResponse.Error(
                response.ErrorInfo?.Code ?? "UNKNOWN_ERROR",
                response.ErrorInfo?.Message ?? "Unknown error");
        }
    }
}