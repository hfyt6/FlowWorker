using System.Text.Json;

namespace FlowWorker.Core.Interfaces;

/// <summary>
/// 工具处理器接口
/// </summary>
public interface IToolHandler
{
    /// <summary>
    /// 工具名称
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// 执行工具
    /// </summary>
    /// <param name="action">操作名称</param>
    /// <param name="parameters">参数</param>
    /// <returns>工具响应</returns>
    Task<ToolResponse> ExecuteAsync(string action, JsonElement parameters);
}

/// <summary>
/// 工具响应类
/// </summary>
public class ToolResponse
{
    public string Status { get; set; } = string.Empty;
    public object? Data { get; set; }
    public ToolError? ErrorInfo { get; set; }
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

/// <summary>
/// 工具错误类
/// </summary>
public class ToolError
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public object? Data { get; set; }
}