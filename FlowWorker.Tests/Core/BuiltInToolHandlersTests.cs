using System.Text.Json;
using FlowWorker.Core.Services;

namespace FlowWorker.Tests.Core;

/// <summary>
/// 内置AI命令工具处理器测试类
/// </summary>
public class BuiltInToolHandlersTests
{
    private readonly BuiltInToolHandlers _handlers;

    public BuiltInToolHandlersTests()
    {
        _handlers = new BuiltInToolHandlers();
    }

    #region AskFollowupQuestion Tests

    [Fact]
    public async Task AskFollowupQuestion_WithValidQuestion_ReturnsSuccess()
    {
        // Arrange
        var parameters = JsonDocument.Parse(@"{""question"": ""What is your name?""}").RootElement;

        // Act
        var result = await _handlers.AskFollowupQuestionAsync(parameters);

        // Assert
        Assert.Equal("success", result.Status);
        Assert.NotNull(result.Data);
        var data = result.Data as Dictionary<string, object>;
        Assert.NotNull(data);
        Assert.Equal("What is your name?", data["question"]);
        Assert.Equal("ask_followup_question", data["type"]);
    }

    [Fact]
    public async Task AskFollowupQuestion_WithoutQuestion_ReturnsError()
    {
        // Arrange
        var parameters = JsonDocument.Parse(@"{}").RootElement;

        // Act
        var result = await _handlers.AskFollowupQuestionAsync(parameters);

        // Assert
        Assert.Equal("error", result.Status);
        Assert.NotNull(result.ErrorInfo);
        Assert.Equal("INVALID_PARAMETERS", result.ErrorInfo!.Code);
    }

    [Fact]
    public async Task AskFollowupQuestion_WithEmptyQuestion_ReturnsError()
    {
        // Arrange
        var parameters = JsonDocument.Parse(@"{""question"": """"}").RootElement;

        // Act
        var result = await _handlers.AskFollowupQuestionAsync(parameters);

        // Assert
        Assert.Equal("error", result.Status);
        Assert.NotNull(result.ErrorInfo);
        Assert.Equal("INVALID_PARAMETERS", result.ErrorInfo!.Code);
    }

    [Fact]
    public async Task AskFollowupQuestion_WithOptions_ReturnsSuccessWithOptions()
    {
        // Arrange
        var parameters = JsonDocument.Parse(@"{""question"": ""Choose an option"", ""options"": [""Option 1"", ""Option 2"", ""Option 3""]}").RootElement;

        // Act
        var result = await _handlers.AskFollowupQuestionAsync(parameters);

        // Assert
        Assert.Equal("success", result.Status);
        var data = result.Data as Dictionary<string, object>;
        Assert.NotNull(data);
        Assert.Equal("Choose an option", data["question"]);
        Assert.True(data.ContainsKey("options"));
        var options = data["options"] as List<string>;
        Assert.NotNull(options);
        Assert.Equal(3, options.Count);
        Assert.Contains("Option 1", options);
        Assert.Contains("Option 2", options);
        Assert.Contains("Option 3", options);
    }

    [Fact]
    public async Task AskFollowupQuestion_WithTaskProgress_ReturnsSuccessWithTaskProgress()
    {
        // Arrange
        var parameters = JsonDocument.Parse(@"{""question"": ""What next?"", ""task_progress"": ""- [x] Step 1\n- [ ] Step 2""}").RootElement;

        // Act
        var result = await _handlers.AskFollowupQuestionAsync(parameters);

        // Assert
        Assert.Equal("success", result.Status);
        var data = result.Data as Dictionary<string, object>;
        Assert.NotNull(data);
        Assert.Equal("What next?", data["question"]);
        Assert.Equal("- [x] Step 1\n- [ ] Step 2", data["task_progress"]);
    }

    #endregion

    #region AttemptCompletion Tests

    [Fact]
    public async Task AttemptCompletion_WithValidResult_ReturnsSuccess()
    {
        // Arrange
        var parameters = JsonDocument.Parse(@"{""result"": ""Task completed successfully""}").RootElement;

        // Act
        var result = await _handlers.AttemptCompletionAsync(parameters);

        // Assert
        Assert.Equal("success", result.Status);
        Assert.NotNull(result.Data);
        var data = result.Data as Dictionary<string, object>;
        Assert.NotNull(data);
        Assert.Equal("Task completed successfully", data["result"]);
        Assert.Equal("attempt_completion", data["type"]);
    }

    [Fact]
    public async Task AttemptCompletion_WithoutResult_ReturnsError()
    {
        // Arrange
        var parameters = JsonDocument.Parse(@"{}").RootElement;

        // Act
        var result = await _handlers.AttemptCompletionAsync(parameters);

        // Assert
        Assert.Equal("error", result.Status);
        Assert.NotNull(result.ErrorInfo);
        Assert.Equal("INVALID_PARAMETERS", result.ErrorInfo!.Code);
    }

    [Fact]
    public async Task AttemptCompletion_WithCommand_ReturnsSuccessWithCommand()
    {
        // Arrange
        var parameters = JsonDocument.Parse(@"{""result"": ""Website created"", ""command"": ""open index.html""}").RootElement;

        // Act
        var result = await _handlers.AttemptCompletionAsync(parameters);

        // Assert
        Assert.Equal("success", result.Status);
        var data = result.Data as Dictionary<string, object>;
        Assert.NotNull(data);
        Assert.Equal("Website created", data["result"]);
        Assert.Equal("open index.html", data["command"]);
    }

    [Fact]
    public async Task AttemptCompletion_WithTaskProgress_ReturnsSuccessWithTaskProgress()
    {
        // Arrange
        var parameters = JsonDocument.Parse(@"{""result"": ""Done"", ""task_progress"": ""- [x] All tasks completed""}").RootElement;

        // Act
        var result = await _handlers.AttemptCompletionAsync(parameters);

        // Assert
        Assert.Equal("success", result.Status);
        var data = result.Data as Dictionary<string, object>;
        Assert.NotNull(data);
        Assert.Equal("Done", data["result"]);
        Assert.Equal("- [x] All tasks completed", data["task_progress"]);
    }

    #endregion

    #region PlanModeRespond Tests

    [Fact]
    public async Task PlanModeRespond_WithValidResponse_ReturnsSuccess()
    {
        // Arrange
        var parameters = JsonDocument.Parse(@"{""response"": ""Here is my plan...""}").RootElement;

        // Act
        var result = await _handlers.PlanModeRespondAsync(parameters);

        // Assert
        Assert.Equal("success", result.Status);
        Assert.NotNull(result.Data);
        var data = result.Data as Dictionary<string, object>;
        Assert.NotNull(data);
        Assert.Equal("Here is my plan...", data["response"]);
        Assert.Equal("plan_mode_respond", data["type"]);
        Assert.Equal(false, data["needs_more_exploration"]);
    }

    [Fact]
    public async Task PlanModeRespond_WithoutResponse_ReturnsError()
    {
        // Arrange
        var parameters = JsonDocument.Parse(@"{}").RootElement;

        // Act
        var result = await _handlers.PlanModeRespondAsync(parameters);

        // Assert
        Assert.Equal("error", result.Status);
        Assert.NotNull(result.ErrorInfo);
        Assert.Equal("INVALID_PARAMETERS", result.ErrorInfo!.Code);
    }

    [Fact]
    public async Task PlanModeRespond_WithNeedsMoreExploration_ReturnsSuccessWithFlag()
    {
        // Arrange
        var parameters = JsonDocument.Parse(@"{""response"": ""I need to explore more"", ""needs_more_exploration"": true}").RootElement;

        // Act
        var result = await _handlers.PlanModeRespondAsync(parameters);

        // Assert
        Assert.Equal("success", result.Status);
        var data = result.Data as Dictionary<string, object>;
        Assert.NotNull(data);
        Assert.Equal("I need to explore more", data["response"]);
        Assert.Equal(true, data["needs_more_exploration"]);
    }

    [Fact]
    public async Task PlanModeRespond_WithTaskProgress_ReturnsSuccessWithTaskProgress()
    {
        // Arrange
        var parameters = JsonDocument.Parse(@"{""response"": ""Plan created"", ""task_progress"": ""- [x] Analysis\n- [ ] Implementation""}").RootElement;

        // Act
        var result = await _handlers.PlanModeRespondAsync(parameters);

        // Assert
        Assert.Equal("success", result.Status);
        var data = result.Data as Dictionary<string, object>;
        Assert.NotNull(data);
        Assert.Equal("Plan created", data["response"]);
        Assert.Equal("- [x] Analysis\n- [ ] Implementation", data["task_progress"]);
    }

    #endregion

    #region BuiltInToolDefinitions Tests

    [Fact]
    public void GetAllDefinitions_ReturnsThreeTools()
    {
        // Act
        var definitions = BuiltInToolDefinitions.GetAllDefinitions();

        // Assert
        Assert.Equal(3, definitions.Count);
        Assert.Contains(definitions, d => d.Name == "ask_followup_question");
        Assert.Contains(definitions, d => d.Name == "attempt_completion");
        Assert.Contains(definitions, d => d.Name == "plan_mode_respond");
    }

    [Fact]
    public void AskFollowupQuestionDefinition_HasCorrectParameters()
    {
        // Act
        var definition = BuiltInToolDefinitions.AskFollowupQuestion;

        // Assert
        Assert.Equal("ask_followup_question", definition.Name);
        Assert.NotEmpty(definition.Description);
        Assert.Equal(3, definition.Parameters.Count);

        var questionParam = definition.Parameters.FirstOrDefault(p => p.Name == "question");
        Assert.NotNull(questionParam);
        Assert.True(questionParam.Required);
        Assert.Equal("string", questionParam.Type);

        var optionsParam = definition.Parameters.FirstOrDefault(p => p.Name == "options");
        Assert.NotNull(optionsParam);
        Assert.False(optionsParam.Required);
        Assert.Equal("array", optionsParam.Type);

        var taskProgressParam = definition.Parameters.FirstOrDefault(p => p.Name == "task_progress");
        Assert.NotNull(taskProgressParam);
        Assert.False(taskProgressParam.Required);
    }

    [Fact]
    public void AttemptCompletionDefinition_HasCorrectParameters()
    {
        // Act
        var definition = BuiltInToolDefinitions.AttemptCompletion;

        // Assert
        Assert.Equal("attempt_completion", definition.Name);
        Assert.NotEmpty(definition.Description);
        Assert.Equal(3, definition.Parameters.Count);

        var resultParam = definition.Parameters.FirstOrDefault(p => p.Name == "result");
        Assert.NotNull(resultParam);
        Assert.True(resultParam.Required);

        var commandParam = definition.Parameters.FirstOrDefault(p => p.Name == "command");
        Assert.NotNull(commandParam);
        Assert.False(commandParam.Required);

        var taskProgressParam = definition.Parameters.FirstOrDefault(p => p.Name == "task_progress");
        Assert.NotNull(taskProgressParam);
        Assert.False(taskProgressParam.Required);
    }

    [Fact]
    public void PlanModeRespondDefinition_HasCorrectParameters()
    {
        // Act
        var definition = BuiltInToolDefinitions.PlanModeRespond;

        // Assert
        Assert.Equal("plan_mode_respond", definition.Name);
        Assert.NotEmpty(definition.Description);
        Assert.Equal(3, definition.Parameters.Count);

        var responseParam = definition.Parameters.FirstOrDefault(p => p.Name == "response");
        Assert.NotNull(responseParam);
        Assert.True(responseParam.Required);

        var needsMoreExplorationParam = definition.Parameters.FirstOrDefault(p => p.Name == "needs_more_exploration");
        Assert.NotNull(needsMoreExplorationParam);
        Assert.False(needsMoreExplorationParam.Required);
        Assert.Equal("boolean", needsMoreExplorationParam.Type);

        var taskProgressParam = definition.Parameters.FirstOrDefault(p => p.Name == "task_progress");
        Assert.NotNull(taskProgressParam);
        Assert.False(taskProgressParam.Required);
    }

    [Fact]
    public void ToPromptDescription_ContainsToolNameAndDescription()
    {
        // Arrange
        var definition = BuiltInToolDefinitions.AskFollowupQuestion;

        // Act
        var description = definition.ToPromptDescription();

        // Assert
        Assert.Contains("## ask_followup_question", description);
        Assert.Contains("Description:", description);
        Assert.Contains("Parameters:", description);
        Assert.Contains("Usage:", description);
    }

    [Fact]
    public void GetAllToolsPromptDescription_ContainsAllTools()
    {
        // Act
        var description = BuiltInToolDefinitions.GetAllToolsPromptDescription();

        // Assert
        Assert.Contains("## ask_followup_question", description);
        Assert.Contains("## attempt_completion", description);
        Assert.Contains("## plan_mode_respond", description);
    }

    #endregion
}
