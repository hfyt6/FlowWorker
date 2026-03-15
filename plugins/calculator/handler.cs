using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FlowWorker.Plugins.Calculator
{
    /// <summary>
    /// 基础计算工具处理器
    /// </summary>
    public class CalculatorHandler
    {
        /// <summary>
        /// 数学计算
        /// </summary>
        public Task<ToolResponse> CalculateAsync(JsonElement parameters)
        {
            try
            {
                var expression = parameters.GetProperty("expression").GetString();
                
                if (string.IsNullOrWhiteSpace(expression))
                {
                    return Task.FromResult(ToolResponse.Error("INVALID_PARAMETERS", "数学表达式不能为空"));
                }

                // 清理表达式，移除潜在的危险字符
                var cleanedExpression = SanitizeExpression(expression);
                
                // 使用 DataTable.Compute 进行计算
                var result = EvaluateExpression(cleanedExpression);
                
                return Task.FromResult(ToolResponse.Success(new
                {
                    expression = expression,
                    result = result,
                    result_type = GetResultType(result)
                }));
            }
            catch (SyntaxErrorException ex)
            {
                return Task.FromResult(ToolResponse.Error("SYNTAX_ERROR", $"表达式语法错误: {ex.Message}"));
            }
            catch (DivideByZeroException)
            {
                return Task.FromResult(ToolResponse.Error("DIVIDE_BY_ZERO", "除数不能为零"));
            }
            catch (OverflowException)
            {
                return Task.FromResult(ToolResponse.Error("OVERFLOW", "计算结果溢出"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(ToolResponse.Error("EXECUTION_FAILED", $"计算失败: {ex.Message}"));
            }
        }

        /// <summary>
        /// 单位转换
        /// </summary>
        public Task<ToolResponse> ConvertUnitAsync(JsonElement parameters)
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

        /// <summary>
        /// 生成UUID
        /// </summary>
        public Task<ToolResponse> GenerateUuidAsync(JsonElement parameters)
        {
            try
            {
                var count = parameters.TryGetProperty("count", out var countProp) 
                    ? countProp.GetInt32() 
                    : 1;

                if (count < 1 || count > 100)
                {
                    return Task.FromResult(ToolResponse.Error("INVALID_PARAMETERS", "生成数量必须在1-100之间"));
                }

                var format = parameters.TryGetProperty("format", out var formatProp) 
                    ? formatProp.GetString()?.ToLowerInvariant() 
                    : "standard";

                var uuids = new List<string>();
                for (int i = 0; i < count; i++)
                {
                    var uuid = Guid.NewGuid();
                    uuids.Add(FormatUuid(uuid, format));
                }

                return Task.FromResult(ToolResponse.Success(new
                {
                    uuids = count == 1 ? (object)uuids[0] : uuids,
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
        /// 获取时间戳
        /// </summary>
        public Task<ToolResponse> GetTimestampAsync(JsonElement parameters)
        {
            try
            {
                var format = parameters.TryGetProperty("format", out var formatProp) 
                    ? formatProp.GetString()?.ToLowerInvariant() 
                    : "unix";

                var timezone = parameters.TryGetProperty("timezone", out var tzProp) 
                    ? tzProp.GetString() 
                    : "utc";

                var now = timezone?.ToLowerInvariant() == "local" 
                    ? DateTime.Now 
                    : DateTime.UtcNow;

                object result;
                switch (format)
                {
                    case "unix":
                    case "unix_seconds":
                        result = new DateTimeOffset(now).ToUnixTimeSeconds();
                        break;
                    case "unix_milliseconds":
                        result = new DateTimeOffset(now).ToUnixTimeMilliseconds();
                        break;
                    case "iso":
                    case "iso8601":
                        result = now.ToString("O");
                        break;
                    case "rfc":
                    case "rfc1123":
                        result = now.ToString("R");
                        break;
                    case "datetime":
                        result = now.ToString("yyyy-MM-dd HH:mm:ss");
                        break;
                    case "date":
                        result = now.ToString("yyyy-MM-dd");
                        break;
                    case "time":
                        result = now.ToString("HH:mm:ss");
                        break;
                    default:
                        result = now.ToString(format);
                        break;
                }

                return Task.FromResult(ToolResponse.Success(new
                {
                    timestamp = result,
                    format = format,
                    timezone = timezone,
                    datetime = now.ToString("O")
                }));
            }
            catch (Exception ex)
            {
                return Task.FromResult(ToolResponse.Error("EXECUTION_FAILED", ex.Message));
            }
        }

        #region Private Methods

        /// <summary>
        /// 清理表达式，移除潜在的危险字符
        /// </summary>
        private string SanitizeExpression(string expression)
        {
            // 只允许数字、运算符、括号和数学函数
            var allowedPattern = @"[^0-9+\-*/().,\s\^%]";
            var cleaned = Regex.Replace(expression, allowedPattern, "");
            
            // 替换 ^ 为幂运算（使用 Math.Pow 语法）
            cleaned = Regex.Replace(cleaned, @"(\d+(?:\.\d+)?)\s*\^\s*(\d+(?:\.\d+)?)", "Math.Pow($1, $2)");
            
            return cleaned;
        }

        /// <summary>
        /// 计算表达式
        /// </summary>
        private object EvaluateExpression(string expression)
        {
            // 使用 DataTable.Compute 进行基本计算
            using (var table = new DataTable())
            {
                var result = table.Compute(expression, null);
                return Convert.ToDouble(result);
            }
        }

        /// <summary>
        /// 获取结果类型
        /// </summary>
        private string GetResultType(object result)
        {
            return result switch
            {
                int => "integer",
                long => "integer",
                float => "float",
                double => "double",
                decimal => "decimal",
                _ => "number"
            };
        }

        /// <summary>
        /// 单位转换
        /// </summary>
        private double ConvertUnit(double value, string fromUnit, string toUnit)
        {
            // 长度单位转换（转换为米）
            var lengthUnits = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["m"] = 1,
                ["meter"] = 1,
                ["meters"] = 1,
                ["米"] = 1,
                ["km"] = 1000,
                ["kilometer"] = 1000,
                ["kilometers"] = 1000,
                ["千米"] = 1000,
                ["cm"] = 0.01,
                ["centimeter"] = 0.01,
                ["centimeters"] = 0.01,
                ["厘米"] = 0.01,
                ["mm"] = 0.001,
                ["millimeter"] = 0.001,
                ["millimeters"] = 0.001,
                ["毫米"] = 0.001,
                ["inch"] = 0.0254,
                ["inches"] = 0.0254,
                ["in"] = 0.0254,
                ["英寸"] = 0.0254,
                ["ft"] = 0.3048,
                ["foot"] = 0.3048,
                ["feet"] = 0.3048,
                ["英尺"] = 0.3048,
                ["yd"] = 0.9144,
                ["yard"] = 0.9144,
                ["yards"] = 0.9144,
                ["码"] = 0.9144,
                ["mi"] = 1609.344,
                ["mile"] = 1609.344,
                ["miles"] = 1609.344,
                ["英里"] = 1609.344
            };

            // 重量单位转换（转换为千克）
            var weightUnits = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["kg"] = 1,
                ["kilogram"] = 1,
                ["kilograms"] = 1,
                ["千克"] = 1,
                ["g"] = 0.001,
                ["gram"] = 0.001,
                ["grams"] = 0.001,
                ["克"] = 0.001,
                ["mg"] = 0.000001,
                ["milligram"] = 0.000001,
                ["milligrams"] = 0.000001,
                ["毫克"] = 0.000001,
                ["t"] = 1000,
                ["ton"] = 1000,
                ["tons"] = 1000,
                ["tonne"] = 1000,
                ["tonnes"] = 1000,
                ["吨"] = 1000,
                ["lb"] = 0.45359237,
                ["pound"] = 0.45359237,
                ["pounds"] = 0.45359237,
                ["磅"] = 0.45359237,
                ["oz"] = 0.02834952,
                ["ounce"] = 0.02834952,
                ["ounces"] = 0.02834952,
                ["盎司"] = 0.02834952
            };

            // 温度单位转换
            if (IsTemperatureUnit(fromUnit) && IsTemperatureUnit(toUnit))
            {
                return ConvertTemperature(value, fromUnit, toUnit);
            }

            // 长度单位转换
            if (lengthUnits.ContainsKey(fromUnit) && lengthUnits.ContainsKey(toUnit))
            {
                var baseValue = value * lengthUnits[fromUnit];
                return baseValue / lengthUnits[toUnit];
            }

            // 重量单位转换
            if (weightUnits.ContainsKey(fromUnit) && weightUnits.ContainsKey(toUnit))
            {
                var baseValue = value * weightUnits[fromUnit];
                return baseValue / weightUnits[toUnit];
            }

            // 数据存储单位转换（转换为字节）
            var dataUnits = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["b"] = 1,
                ["byte"] = 1,
                ["bytes"] = 1,
                ["字节"] = 1,
                ["kb"] = 1024,
                ["kilobyte"] = 1024,
                ["kilobytes"] = 1024,
                ["千字节"] = 1024,
                ["mb"] = 1024 * 1024,
                ["megabyte"] = 1024 * 1024,
                ["megabytes"] = 1024 * 1024,
                ["兆字节"] = 1024 * 1024,
                ["gb"] = 1024 * 1024 * 1024,
                ["gigabyte"] = 1024 * 1024 * 1024,
                ["gigabytes"] = 1024 * 1024 * 1024,
                ["吉字节"] = 1024 * 1024 * 1024,
                ["tb"] = 1024L * 1024 * 1024 * 1024,
                ["terabyte"] = 1024L * 1024 * 1024 * 1024,
                ["terabytes"] = 1024L * 1024 * 1024 * 1024,
                ["太字节"] = 1024L * 1024 * 1024 * 1024
            };

            if (dataUnits.ContainsKey(fromUnit) && dataUnits.ContainsKey(toUnit))
            {
                var baseValue = value * dataUnits[fromUnit];
                return baseValue / dataUnits[toUnit];
            }

            // 时间单位转换（转换为秒）
            var timeUnits = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["s"] = 1,
                ["sec"] = 1,
                ["second"] = 1,
                ["seconds"] = 1,
                ["秒"] = 1,
                ["min"] = 60,
                ["minute"] = 60,
                ["minutes"] = 60,
                ["分钟"] = 60,
                ["h"] = 3600,
                ["hr"] = 3600,
                ["hour"] = 3600,
                ["hours"] = 3600,
                ["小时"] = 3600,
                ["d"] = 86400,
                ["day"] = 86400,
                ["days"] = 86400,
                ["天"] = 86400,
                ["w"] = 604800,
                ["week"] = 604800,
                ["weeks"] = 604800,
                ["周"] = 604800,
                ["mo"] = 2592000,
                ["month"] = 2592000,
                ["months"] = 2592000,
                ["月"] = 2592000,
                ["y"] = 31536000,
                ["year"] = 31536000,
                ["years"] = 31536000,
                ["年"] = 31536000
            };

            if (timeUnits.ContainsKey(fromUnit) && timeUnits.ContainsKey(toUnit))
            {
                var baseValue = value * timeUnits[fromUnit];
                return baseValue / timeUnits[toUnit];
            }

            throw new NotSupportedException($"不支持的单位转换: {fromUnit} 到 {toUnit}");
        }

        /// <summary>
        /// 判断是否为温度单位
        /// </summary>
        private bool IsTemperatureUnit(string unit)
        {
            var tempUnits = new[] { "c", "celsius", "f", "fahrenheit", "k", "kelvin", "°c", "°f", "摄氏度", "华氏度", "开尔文" };
            return tempUnits.Contains(unit.ToLowerInvariant());
        }

        /// <summary>
        /// 温度单位转换
        /// </summary>
        private double ConvertTemperature(double value, string fromUnit, string toUnit)
        {
            // 统一转换为摄氏度
            double celsius = fromUnit.ToLowerInvariant() switch
            {
                "c" or "celsius" or "°c" or "摄氏度" => value,
                "f" or "fahrenheit" or "°f" or "华氏度" => (value - 32) * 5 / 9,
                "k" or "kelvin" or "开尔文" => value - 273.15,
                _ => throw new NotSupportedException($"不支持的温度单位: {fromUnit}")
            };

            // 从摄氏度转换到目标单位
            return toUnit.ToLowerInvariant() switch
            {
                "c" or "celsius" or "°c" or "摄氏度" => celsius,
                "f" or "fahrenheit" or "°f" or "华氏度" => celsius * 9 / 5 + 32,
                "k" or "kelvin" or "开尔文" => celsius + 273.15,
                _ => throw new NotSupportedException($"不支持的温度单位: {toUnit}")
            };
        }

        /// <summary>
        /// 格式化UUID
        /// </summary>
        private string FormatUuid(Guid uuid, string format)
        {
            return format?.ToLowerInvariant() switch
            {
                "n" or "nodashes" or "no_dashes" => uuid.ToString("N"),
                "d" or "standard" => uuid.ToString("D"),
                "b" or "braces" => uuid.ToString("B"),
                "p" or "parentheses" => uuid.ToString("P"),
                "x" or "hex" => uuid.ToString("X"),
                "upper" or "uppercase" => uuid.ToString("D").ToUpperInvariant(),
                _ => uuid.ToString("D")
            };
        }

        #endregion
    }

    /// <summary>
    /// 工具响应类
    /// </summary>
    public class ToolResponse
    {
        public string Status { get; set; }
        public object Data { get; set; }
        public ToolError ErrorInfo { get; set; }
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
                ErrorInfo = new ToolError { Code = code, Message = message }
            };
        }
    }

    public class ToolError
    {
        public string Code { get; set; }
        public string Message { get; set; }
    }
}
