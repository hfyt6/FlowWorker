using System.Runtime.InteropServices;
using System.Text.Json;
using FlowWorker.Core.Interfaces;

namespace FlowWorker.Core.Tools.ProcessTools;

/// <summary>
/// 进程管理工具处理器
/// 提供命令执行、进程管理等功能
/// </summary>
public partial class ProcessTool : IToolHandler
{
    private static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    private static readonly bool IsLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    private static readonly bool IsMacOS = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

    public string Name => "Process";

    public IReadOnlyList<string> SupportedActions => new[]
    {
        "execute_command",
        "run_process",
        "kill_process",
        "list_processes",
        "get_process_info"
    };

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
}