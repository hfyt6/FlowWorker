using System.Diagnostics;
using System.IO;
using System.Text.Json;
using FlowWorker.Core.Interfaces;

namespace FlowWorker.Core.Tools.ProcessTools;

/// <summary>
/// 进程管理工具处理器 - 执行命令
/// </summary>
public partial class ProcessTool
{
    /// <summary>
    /// 执行系统命令
    /// </summary>
    private async Task<ToolResponse> ExecuteCommandAsync(JsonElement parameters)
    {
        try
        {
            var command = parameters.GetProperty("command").GetString();
            if (string.IsNullOrWhiteSpace(command))
            {
                return ToolResponse.Error("INVALID_PARAMETERS", "命令不能为空");
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

            var timeout = parameters.TryGetProperty("timeout", out var timeoutProp)
                ? timeoutProp.GetInt32()
                : 30000;

            string shellPath;
            string shellArgs;
            
            if (IsWindows)
            {
                shellPath = "cmd.exe";
                shellArgs = $"/c \"{EscapeShellCommand(command)} {string.Join(" ", args.Select(EscapeArgument))}\"";
            }
            else if (IsMacOS)
            {
                shellPath = File.Exists("/bin/zsh") ? "/bin/zsh" : "/bin/bash";
                shellArgs = $"-c \"{EscapeShellCommand(command)} {string.Join(" ", args.Select(EscapeArgument))}\"";
            }
            else
            {
                shellPath = "/bin/bash";
                shellArgs = $"-c \"{EscapeShellCommand(command)} {string.Join(" ", args.Select(EscapeArgument))}\"";
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = shellPath,
                Arguments = shellArgs,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            if (!string.IsNullOrWhiteSpace(workingDir) && Directory.Exists(workingDir))
            {
                startInfo.WorkingDirectory = workingDir;
            }

            var stopwatch = Stopwatch.StartNew();
            using var process = Process.Start(startInfo);
            if (process == null)
            {
                return ToolResponse.Error("EXECUTION_FAILED", "无法启动进程");
            }

            var cts = new CancellationTokenSource(timeout);
            try
            {
                await Task.WhenAll(
                    process.WaitForExitAsync(cts.Token),
                    Task.Delay(timeout, cts.Token)
                );
            }
            catch (OperationCanceledException)
            {
                try { process.Kill(); } catch { }
                return ToolResponse.Error("TIMEOUT", $"命令执行超时（{timeout}ms）");
            }

            stopwatch.Stop();

            var stdout = await process.StandardOutput.ReadToEndAsync();
            var stderr = await process.StandardError.ReadToEndAsync();

            return ToolResponse.Success(new
            {
                exit_code = process.ExitCode,
                stdout = stdout,
                stderr = stderr,
                execution_time = (int)stopwatch.ElapsedMilliseconds
            });
        }
        catch (Exception ex)
        {
            return ToolResponse.Error("EXECUTION_FAILED", ex.Message);
        }
    }

    private string EscapeShellCommand(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            return "";

        if (IsWindows)
        {
            return command.Replace("\"", "\\\"");
        }
        else
        {
            return command.Replace("\"", "\\\"").Replace("$", "\\$");
        }
    }

    private string EscapeArgument(string arg)
    {
        if (string.IsNullOrWhiteSpace(arg))
            return "";

        if (IsWindows)
        {
            if (arg.Contains(" ") || arg.Contains("\"") || arg.Contains("\t"))
            {
                return "\"" + arg.Replace("\"", "\\\"") + "\"";
            }
            return arg;
        }
        else
        {
            return "'" + arg.Replace("'", "'\\''") + "'";
        }
    }
}