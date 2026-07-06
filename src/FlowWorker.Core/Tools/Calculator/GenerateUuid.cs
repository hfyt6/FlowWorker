using System.Text.Json;
using FlowWorker.Core.Interfaces;

namespace FlowWorker.Core.Tools.Calculator;

/// <summary>
/// 计算器工具处理器 - 生成UUID
/// </summary>
public partial class CalculatorTool
{
    /// <summary>
    /// 生成UUID
    /// </summary>
    private Task<ToolResponse> GenerateUuidAsync(JsonElement parameters)
    {
        try
        {
            var count = parameters.TryGetProperty("count", out var countProp) ? countProp.GetInt32() : 1;
            var format = parameters.TryGetProperty("format", out var formatProp) ? formatProp.GetString() : "D";

            if (count < 1 || count > 100)
            {
                return Task.FromResult(ToolResponse.Error("INVALID_PARAMETERS", "生成数量必须在1-100之间"));
            }

            var uuids = new List<string>();
            for (int i = 0; i < count; i++)
            {
                var uuid = Guid.NewGuid();
                uuids.Add(FormatUuid(uuid, format));
            }

            return Task.FromResult(ToolResponse.Success(new
            {
                uuids = uuids,
                count = count,
                format = format
            }));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResponse.Error("EXECUTION_FAILED", ex.Message));
        }
    }

    /// <summary>
    /// 格式化UUID
    /// </summary>
    private string FormatUuid(Guid uuid, string format)
    {
        return format?.ToLowerInvariant() switch
        {
            "n" or "nodashes" => uuid.ToString("N"),
            "d" or "standard" => uuid.ToString("D"),
            "b" or "braces" => uuid.ToString("B"),
            "p" or "parentheses" => uuid.ToString("P"),
            "x" or "hex" => uuid.ToString("X"),
            _ => uuid.ToString("D")
        };
    }
}