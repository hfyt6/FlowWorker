using System.Text.Json;
using FlowWorker.Core.Interfaces;

namespace FlowWorker.Core.Tools.Calculator;

/// <summary>
/// 计算器工具处理器
/// 提供数学计算、单位转换、UUID 生成、时间戳获取等功能
/// </summary>
public partial class CalculatorTool : IToolHandler
{
    public string Name => "Calculator";

    public IReadOnlyList<string> SupportedActions => new[]
    {
        "calculate",
        "convert_unit",
        "generate_uuid",
        "get_timestamp"
    };

    public async Task<ToolResponse> ExecuteAsync(string action, JsonElement parameters)
    {
        return action.ToLowerInvariant() switch
        {
            "calculate" => await CalculateAsync(parameters),
            "convert_unit" => await ConvertUnitAsync(parameters),
            "generate_uuid" => await GenerateUuidAsync(parameters),
            "get_timestamp" => await GetTimestampAsync(parameters),
            _ => ToolResponse.Error("UNKNOWN_ACTION", $"未知的操作：{action}")
        };
    }
}