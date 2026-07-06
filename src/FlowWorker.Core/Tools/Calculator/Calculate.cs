using System.Data;
using System.Text.Json;
using System.Text.RegularExpressions;
using FlowWorker.Core.Interfaces;

namespace FlowWorker.Core.Tools.Calculator;

/// <summary>
/// 计算器工具处理器 - 数学计算
/// </summary>
public partial class CalculatorTool
{
    /// <summary>
    /// 数学计算
    /// </summary>
    private Task<ToolResponse> CalculateAsync(JsonElement parameters)
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
            return Task.FromResult(ToolResponse.Error("SYNTAX_ERROR", $"表达式语法错误：{ex.Message}"));
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
            return Task.FromResult(ToolResponse.Error("EXECUTION_FAILED", $"计算失败：{ex.Message}"));
        }
    }

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
}