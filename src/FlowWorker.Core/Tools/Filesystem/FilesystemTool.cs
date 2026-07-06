using System.Text.Json;
using FlowWorker.Core.Interfaces;

namespace FlowWorker.Core.Tools.Filesystem;

/// <summary>
/// 文件系统工具处理器
/// 提供文件读取、写入、删除、目录操作等功能
/// </summary>
public partial class FilesystemTool : IToolHandler
{
    public string Name => "Filesystem";

    public IReadOnlyList<string> SupportedActions => new[]
    {
        "read_file",
        "write_file",
        "write_to_file",
        "list_files",
        "file_exists",
        "delete_file",
        "create_directory"
    };

    public async Task<ToolResponse> ExecuteAsync(string action, JsonElement parameters)
    {
        return action.ToLowerInvariant() switch
        {
            "read_file" => await ReadFileAsync(parameters),
            "write_file" => await WriteFileAsync(parameters),
            "write_to_file" => await WriteFileAsync(parameters),
            "list_files" => await ListFilesAsync(parameters),
            "file_exists" => await FileExistsAsync(parameters),
            "delete_file" => await DeleteFileAsync(parameters),
            "create_directory" => await CreateDirectoryAsync(parameters),
            _ => ToolResponse.Error("UNKNOWN_ACTION", $"未知的操作：{action}")
        };
    }
}