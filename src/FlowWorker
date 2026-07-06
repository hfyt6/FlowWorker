using System.Diagnostics;
using System.Text.Json;
using FlowWorker.Core.Interfaces;

namespace FlowWorker.Core.Tools.ProcessTools;

/// <summary>
/// 进程管理工具处理器 - 获取进程信息
/// </summary>
public partial class ProcessTool
{
    /// <summary>
    /// 获取进程详细信息
    /// </summary>
    private Task<ToolResponse> GetProcessInfoAsync(JsonElement parameters)
    {
        try
        {
            var pid = parameters.GetProperty("pid").GetInt32();
            if (pid <= 0)
            {
                return Task.FromResult(ToolResponse.Error("INVALID_PARAMETERS", "无效的进程 ID"));
            }

            try
            {
                var process = Process.GetProcessById(pid);
                if (process.HasExited)
                    return Task.FromResult(ToolResponse.Error("PROCESS_NOT_FOUND", $"进程 {pid} 已退出"));

                return Task.FromResult(ToolResponse.Success(new
                {
                    pid = pid,
                    name = process.ProcessName,
                    command_line = process.MainModule?.FileName ?? process.ProcessName,
                    executable_path = process.MainModule?.FileName,
                    cpu_percent = 0,
                    memory_mb = Math.Round(process.WorkingSet64 / (1024.0 * 1024.0), 2),
                    memory_percent = 0,
                    status = process.Responding ? "running" : "unknown",
                    start_time = process.StartTime.ToUniversalTime().ToString("O"),
                    parent_pid = 0,
                    threads = process.Threads.Count,
                    user = Environment.UserName
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