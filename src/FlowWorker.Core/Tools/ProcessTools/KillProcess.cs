using System.Diagnostics;
using System.Text.Json;
using FlowWorker.Core.Interfaces;

namespace FlowWorker.Core.Tools.ProcessTools;

/// <summary>
/// 进程管理工具处理器 - 终止进程
/// </summary>
public partial class ProcessTool
{
    /// <summary>
    /// 终止进程
    /// </summary>
    private Task<ToolResponse> KillProcessAsync(JsonElement parameters)
    {
        try
        {
            var pid = parameters.GetProperty("pid").GetInt32();
            if (pid <= 0)
            {
                return Task.FromResult(ToolResponse.Error("INVALID_PARAMETERS", "无效的进程 ID"));
            }

            var force = parameters.TryGetProperty("force", out var forceProp) && forceProp.GetBoolean();

            try
            {
                var process = Process.GetProcessById(pid);
                if (process.HasExited)
                {
                    return Task.FromResult(ToolResponse.Error("PROCESS_NOT_FOUND", $"进程 {pid} 已退出"));
                }

                if (force)
                {
                    process.Kill(true);
                }
                else
                {
                    process.CloseMainWindow();
                    if (!process.WaitForExit(5000))
                    {
                        process.Kill();
                    }
                }

                return Task.FromResult(ToolResponse.Success(new
                {
                    success = true,
                    message = $"进程 {pid} 已终止"
                }));
            }
            catch (ArgumentException)
            {
                return Task.FromResult(ToolResponse.Error("PROCESS_NOT_FOUND", $"进程 {pid} 不存在"));
            }
            catch (InvalidOperationException)
            {
                return Task.FromResult(ToolResponse.Error("PROCESS_NOT_FOUND", $"进程 {pid} 已退出"));
            }
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResponse.Error("EXECUTION_FAILED", ex.Message));
        }
    }
}