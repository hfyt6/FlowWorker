using System.Diagnostics;
using System.IO;
using System.Text.Json;
using FlowWorker.Core.Interfaces;

namespace FlowWorker.Core.Tools.ProcessTools;

/// <summary>
/// 进程管理工具处理器 - 运行进程
/// </summary>
public partial class ProcessTool
{
    /// <summary>
    /// 运行进程
    /// </summary>
    private Task<ToolResponse> RunProcessAsync(JsonElement parameters)
    {
        try
        {
            var command = parameters.GetProperty("command").GetString();
            if (string.IsNullOrWhiteSpace(command))
            {
                return Task.FromResult(ToolResponse.Error("INVALID_PARAMETERS", "命令不能为空"));
            }

            var args = new List<string>();
            if (parameters.TryGetProperty("args", out var argsProp) && argsProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var arg in argsProp.EnumerateArray())
                {
                    args.Add(arg.GetString() ?? string.Empty);
                }
            }

            var workingDir = parameters.TryGetProperty("working_directory", out var wdProp)
                ? wdProp.GetString()
                : null;

            var startInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = string.Join(" ", args.Select(EscapeArgument)),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            if (!string.IsNullOrWhiteSpace(workingDir) && Directory.Exists(workingDir))
            {
                startInfo.WorkingDirectory = workingDir;
            }

            var process = Process.Start(startInfo);
            if (process == null)
            {
                return Task.FromResult(ToolResponse.Error("EXECUTION_FAILED", "无法启动进程"));
            }

            return Task.FromResult(ToolResponse.Success(new
            {
                pid = process.Id,
                process_name = process.ProcessName,
                start_time = DateTime.UtcNow.ToString("O")
            }));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResponse.Error("EXECUTION_FAILED", ex.Message));
        }
    }
}