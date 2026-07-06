using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using FlowWorker.Core.Interfaces;

namespace FlowWorker.Core.Tools.ProcessTools;

/// <summary>
/// 进程管理工具处理器 - 列出进程
/// </summary>
public partial class ProcessTool
{
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