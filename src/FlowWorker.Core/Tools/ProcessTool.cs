using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using FlowWorker.Core.Interfaces;

namespace FlowWorker.Core.Tools;

/// <summary>
/// 进程管理工具处理器
/// 提供命令执行、进程管理等功能
/// </summary>
public class ProcessTool : IToolHandler
{
    private static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    private static readonly bool IsLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    private static readonly bool IsMacOS = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

    public string Name => "Process";

    public async Task<ToolResponse> ExecuteAsync(string action, JsonElement parameters)
    {
        return action.ToLowerInvariant() switch
        {
            "execute_command" => await ExecuteCommandAsync(parameters),
            "run_process" => await RunProcessAsync(parameters),
            "kill_process" => await KillProcessAsync(parameters),
            "list_processes" => await ListProcessesAsync(parameters),
            "get_process_info" => await GetProcessInfoAsync(parameters),
            _ => ToolResponse.Error("UNKNOWN_ACTION", $"未知的操作：{action}")
        };
    }

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

    /// <summary>
    /// 列出进程
    /// </summary>
    private async Task<ToolResponse> ListProcessesAsync(JsonElement parameters)
    {
        try
        {
            var filter = parameters.TryGetProperty("filter", out var filterProp)
                ? filterProp.GetString()?.ToLowerInvariant()
                : null;

            var includeSystem = parameters.TryGetProperty("include_system", out var sysProp) && sysProp.GetBoolean();

            var processes = new List<object>();

            if (IsWindows)
            {
                processes = await ListWindowsProcessesAsync(filter, includeSystem);
            }
            else if (IsLinux)
            {
                processes = await ListLinuxProcessesAsync(filter, includeSystem);
            }
            else if (IsMacOS)
            {
                processes = await ListMacOSProcessesAsync(filter, includeSystem);
            }
            else
            {
                processes = ListGenericProcesses(filter, includeSystem);
            }

            return ToolResponse.Success(new
            {
                processes = processes,
                total_count = processes.Count
            });
        }
        catch (Exception ex)
        {
            return ToolResponse.Error("EXECUTION_FAILED", ex.Message);
        }
    }

    private async Task<List<object>> ListWindowsProcessesAsync(string? filter, bool includeSystem)
    {
        var processes = new List<object>();

        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = "-Command \"Get-Process | Select-Object Id, ProcessName, Path, CPU, WorkingSet, StartTime | ConvertTo-Json -Compress\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process != null)
        {
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            try
            {
                var json = JsonDocument.Parse(output);
                if (json.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var proc in json.RootElement.EnumerateArray())
                    {
                        var name = proc.TryGetProperty("ProcessName", out var nameProp) ? nameProp.GetString() : "Unknown";
                        var pid = proc.TryGetProperty("Id", out var idProp) ? idProp.GetInt32() : 0;
                        var path = proc.TryGetProperty("Path", out var pathProp) ? pathProp.GetString() : "";

                        if (!includeSystem && IsSystemProcess(name, path))
                            continue;

                        if (!string.IsNullOrWhiteSpace(filter) &&
                            !name.ToLowerInvariant().Contains(filter) &&
                            !(path?.ToLowerInvariant().Contains(filter) ?? false))
                            continue;

                        processes.Add(new
                        {
                            pid = pid,
                            name = name,
                            command_line = path ?? name,
                            cpu_percent = 0,
                            memory_mb = 0,
                            status = "running",
                            start_time = (string?)null
                        });
                    }
                }
            }
            catch { }
        }

        if (processes.Count == 0)
        {
            foreach (var proc in Process.GetProcesses())
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(filter) &&
                        !proc.ProcessName.ToLowerInvariant().Contains(filter))
                        continue;

                    processes.Add(new
                    {
                        pid = proc.Id,
                        name = proc.ProcessName,
                        command_line = proc.MainModule?.FileName ?? proc.ProcessName,
                        cpu_percent = 0,
                        memory_mb = Math.Round(proc.WorkingSet64 / (1024.0 * 1024.0), 2),
                        status = proc.Responding ? "running" : "unknown",
                        start_time = DateTime.UtcNow.ToString("O")
                    });
                }
                catch { }
            }
        }

        return processes;
    }

    private async Task<List<object>> ListLinuxProcessesAsync(string? filter, bool includeSystem)
    {
        var processes = new List<object>();

        var startInfo = new ProcessStartInfo
        {
            FileName = "/bin/sh",
            Arguments = "-c \"ps aux --no-headers\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process != null)
        {
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var parts = Regex.Split(line.Trim(), "\\s+");
                if (parts.Length >= 11)
                {
                    try
                    {
                        var user = parts[0];
                        var pid = int.Parse(parts[1]);
                        var cpu = double.Parse(parts[2]);
                        var mem = double.Parse(parts[3]);
                        var command = string.Join(" ", parts.Skip(10));

                        if (!includeSystem && (user == "root" || user == "sys" || user == "daemon"))
                            continue;

                        if (!string.IsNullOrWhiteSpace(filter) &&
                            !command.ToLowerInvariant().Contains(filter))
                            continue;

                        var status = "running";

                        processes.Add(new
                        {
                            pid = pid,
                            name = Path.GetFileName(command.Split(' ')[0]),
                            command_line = command,
                            cpu_percent = cpu,
                            memory_mb = Math.Round(mem * 10, 2),
                            status = status,
                            start_time = (string?)null
                        });
                    }
                    catch { }
                }
            }
        }

        return processes;
    }

    private async Task<List<object>> ListMacOSProcessesAsync(string? filter, bool includeSystem)
    {
        var processes = new List<object>();

        var startInfo = new ProcessStartInfo
        {
            FileName = "/bin/ps",
            Arguments = "-axo pid,ppid,user,pcpu,pmem,rss,vsz,stat,start_time,time,comm,command",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process != null)
        {
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var parts = Regex.Split(line, "\\s+");
                if (parts.Length >= 11)
                {
                    try
                    {
                        var pid = int.Parse(parts[0]);
                        var user = parts[2];
                        var cpu = double.Parse(parts[3]);
                        var mem = double.Parse(parts[4]);
                        var rss = long.Parse(parts[5]) * 1024;
                        var comm = parts[10];
                        var command = string.Join(" ", parts.Skip(11));

                        if (!includeSystem && (user == "root" || user == "_" + comm))
                            continue;

                        if (!string.IsNullOrWhiteSpace(filter) &&
                            !comm.ToLowerInvariant().Contains(filter) &&
                            !command.ToLowerInvariant().Contains(filter))
                            continue;

                        processes.Add(new
                        {
                            pid = pid,
                            name = comm,
                            command_line = string.IsNullOrWhiteSpace(command) ? comm : command,
                            cpu_percent = cpu,
                            memory_mb = Math.Round(rss / (1024.0 * 1024.0), 2),
                            status = "running",
                            start_time = (string?)null
                        });
                    }
                    catch { }
                }
            }
        }

        return processes;
    }

    private List<object> ListGenericProcesses(string? filter, bool includeSystem)
    {
        var processes = new List<object>();

        foreach (var proc in Process.GetProcesses())
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(filter) &&
                    !proc.ProcessName.ToLowerInvariant().Contains(filter))
                    continue;

                processes.Add(new
                {
                    pid = proc.Id,
                    name = proc.ProcessName,
                    command_line = proc.MainModule?.FileName ?? proc.ProcessName,
                    cpu_percent = 0,
                    memory_mb = Math.Round(proc.WorkingSet64 / (1024.0 * 1024.0), 2),
                    status = proc.Responding ? "running" : "unknown",
                    start_time = DateTime.UtcNow.ToString("O")
                });
            }
            catch { }
        }

        return processes;
    }

    /// <summary>
    /// 获取进程详细信息
    /// </summary>
    private async Task<ToolResponse> GetProcessInfoAsync(JsonElement parameters)
    {
        try
        {
            var pid = parameters.GetProperty("pid").GetInt32();
            if (pid <= 0)
            {
                return ToolResponse.Error("INVALID_PARAMETERS", "无效的进程 ID");
            }

            try
            {
                var process = Process.GetProcessById(pid);
                if (process.HasExited)
                    return ToolResponse.Error("PROCESS_NOT_FOUND", $"进程 {pid} 已退出");

                return ToolResponse.Success(new
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
                });
            }
            catch (ArgumentException)
            {
                return ToolResponse.Error("PROCESS_NOT_FOUND", $"进程 {pid} 不存在");
            }
            catch (InvalidOperationException)
            {
                return ToolResponse.Error("PROCESS_NOT_FOUND", $"进程 {pid} 已退出");
            }
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

    private bool IsSystemProcess(string? name, string? path)
    {
        var systemProcesses = new[]
        {
            "svchost", "csrss", "smss", "services", "lsass", "winlogon",
            "explorer", "System", "Registry", "Memory Compression",
            "init", "systemd", "kthreadd", "ksoftirqd", "kworker"
        };

        if (systemProcesses.Any(sp => name?.ToLowerInvariant().Contains(sp.ToLowerInvariant()) ?? false))
            return true;

        if (IsWindows && (path?.StartsWith("C:\\Windows\\System32", StringComparison.OrdinalIgnoreCase) ?? false))
            return true;

        return false;
    }
}