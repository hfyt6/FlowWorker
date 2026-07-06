using System.Text.Json;
using FlowWorker.Core.Interfaces;

namespace FlowWorker.Core.Tools.Calculator;

/// <summary>
/// 计算器工具处理器 - 获取时间戳
/// </summary>
public partial class CalculatorTool
{
    /// <summary>
    /// 获取时间戳
    /// </summary>
    private Task<ToolResponse> GetTimestampAsync(JsonElement parameters)
    {
        try
        {
            var format = parameters.TryGetProperty("format", out var formatProp) ? formatProp.GetString() : "yyyy-MM-dd HH:mm:ss";
            var timezone = parameters.TryGetProperty("timezone", out var timezoneProp) ? timezoneProp.GetString() : "local";

            var now = timezone?.ToLowerInvariant() switch
            {
                "utc" => DateTime.UtcNow,
                _ => DateTime.Now
            };

            var timestamp = format?.ToLowerInvariant() switch
            {
                "unix" or "unix_seconds" => new DateTimeOffset(now).ToUnixTimeSeconds().ToString(),
                "unix_milliseconds" => new DateTimeOffset(now).ToUnixTimeMilliseconds().ToString(),
                "iso8601" => now.ToString("O"),
                "rfc1123" => now.ToUniversalTime().ToString("R"),
                _ => now.ToString(format)
            };

            return Task.FromResult(ToolResponse.Success(new
            {
                timestamp = timestamp,
                format = format,
                timezone = timezone
            }));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResponse.Error("EXECUTION_FAILED", ex.Message));
        }
    }
}