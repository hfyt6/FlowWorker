using System.Text.Json;
using FlowWorker.Core.Interfaces;

namespace FlowWorker.Core.Tools.Calculator;

/// <summary>
/// 计算器工具处理器 - 单位转换
/// </summary>
public partial class CalculatorTool
{
    /// <summary>
    /// 单位转换
    /// </summary>
    private Task<ToolResponse> ConvertUnitAsync(JsonElement parameters)
    {
        try
        {
            var value = parameters.GetProperty("value").GetDouble();
            var fromUnit = parameters.GetProperty("from_unit").GetString()?.ToLowerInvariant();
            var toUnit = parameters.GetProperty("to_unit").GetString()?.ToLowerInvariant();
            
            if (string.IsNullOrWhiteSpace(fromUnit) || string.IsNullOrWhiteSpace(toUnit))
            {
                return Task.FromResult(ToolResponse.Error("INVALID_PARAMETERS", "原单位和目标单位不能为空"));
            }

            var convertedValue = ConvertUnit(value, fromUnit, toUnit);
            
            return Task.FromResult(ToolResponse.Success(new
            {
                original_value = value,
                original_unit = fromUnit,
                converted_value = convertedValue,
                target_unit = toUnit,
                conversion_rate = convertedValue / value
            }));
        }
        catch (NotSupportedException ex)
        {
            return Task.FromResult(ToolResponse.Error("UNSUPPORTED_CONVERSION", ex.Message));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResponse.Error("EXECUTION_FAILED", ex.Message));
        }
    }

    private double ConvertUnit(double value, string fromUnit, string toUnit)
    {
        var lengthUnits = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
        {
            ["m"] = 1, ["meter"] = 1, ["meters"] = 1,
            ["km"] = 1000, ["kilometer"] = 1000,
            ["cm"] = 0.01, ["centimeter"] = 0.01,
            ["mm"] = 0.001, ["millimeter"] = 0.001,
            ["inch"] = 0.0254, ["inches"] = 0.0254,
            ["ft"] = 0.3048, ["foot"] = 0.3048, ["feet"] = 0.3048,
            ["yd"] = 0.9144, ["yard"] = 0.9144, ["yards"] = 0.9144,
            ["mi"] = 1609.344, ["mile"] = 1609.344, ["miles"] = 1609.344
        };

        var weightUnits = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
        {
            ["kg"] = 1, ["kilogram"] = 1, ["kilograms"] = 1,
            ["g"] = 0.001, ["gram"] = 0.001, ["grams"] = 0.001,
            ["mg"] = 0.000001, ["milligram"] = 0.000001,
            ["t"] = 1000, ["ton"] = 1000, ["tons"] = 1000,
            ["lb"] = 0.45359237, ["pound"] = 0.45359237,
            ["oz"] = 0.02834952, ["ounce"] = 0.02834952
        };

        if (IsTemperatureUnit(fromUnit) && IsTemperatureUnit(toUnit))
            return ConvertTemperature(value, fromUnit, toUnit);

        if (lengthUnits.ContainsKey(fromUnit) && lengthUnits.ContainsKey(toUnit))
            return value * lengthUnits[fromUnit] / lengthUnits[toUnit];

        if (weightUnits.ContainsKey(fromUnit) && weightUnits.ContainsKey(toUnit))
            return value * weightUnits[fromUnit] / weightUnits[toUnit];

        var dataUnits = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
        {
            ["b"] = 1, ["byte"] = 1,
            ["kb"] = 1024, ["kilobyte"] = 1024,
            ["mb"] = 1024 * 1024, ["megabyte"] = 1024 * 1024,
            ["gb"] = 1024L * 1024 * 1024, ["gigabyte"] = 1024L * 1024 * 1024,
            ["tb"] = 1024L * 1024 * 1024 * 1024, ["terabyte"] = 1024L * 1024 * 1024 * 1024
        };

        if (dataUnits.ContainsKey(fromUnit) && dataUnits.ContainsKey(toUnit))
            return value * dataUnits[fromUnit] / dataUnits[toUnit];

        var timeUnits = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
        {
            ["s"] = 1, ["sec"] = 1, ["second"] = 1,
            ["min"] = 60, ["minute"] = 60,
            ["h"] = 3600, ["hr"] = 3600, ["hour"] = 3600,
            ["d"] = 86400, ["day"] = 86400,
            ["w"] = 604800, ["week"] = 604800,
            ["mo"] = 2592000, ["month"] = 2592000,
            ["y"] = 31536000, ["year"] = 31536000
        };

        if (timeUnits.ContainsKey(fromUnit) && timeUnits.ContainsKey(toUnit))
            return value * timeUnits[fromUnit] / timeUnits[toUnit];

        throw new NotSupportedException($"不支持的单位转换：{fromUnit} 到 {toUnit}");
    }

    private bool IsTemperatureUnit(string unit)
    {
        var tempUnits = new[] { "c", "celsius", "f", "fahrenheit", "k", "kelvin" };
        return tempUnits.Contains(unit.ToLowerInvariant());
    }

    private double ConvertTemperature(double value, string fromUnit, string toUnit)
    {
        double celsius = fromUnit.ToLowerInvariant() switch
        {
            "c" or "celsius" => value,
            "f" or "fahrenheit" => (value - 32) * 5 / 9,
            "k" or "kelvin" => value - 273.15,
            _ => throw new NotSupportedException($"不支持的温度单位：{fromUnit}")
        };

        return toUnit.ToLowerInvariant() switch
        {
            "c" or "celsius" => celsius,
            "f" or "fahrenheit" => celsius * 9 / 5 + 32,
            "k" or "kelvin" => celsius + 273.15,
            _ => throw new NotSupportedException($"不支持的温度单位：{toUnit}")
        };
    }
}