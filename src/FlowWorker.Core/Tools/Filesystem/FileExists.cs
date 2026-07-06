using System.IO;
using System.Text.Json;
using FlowWorker.Core.Interfaces;

namespace FlowWorker.Core.Tools.Filesystem;

/// <summary>
/// 文件系统工具处理器 - 检查文件是否存在
/// </summary>
public partial class FilesystemTool
{
    /// <summary>
    /// 检查文件是否存在
    /// </summary>
    private Task<ToolResponse> FileExistsAsync(JsonElement parameters)
    {
        try
        {
            var filePath = parameters.GetProperty("file_path").GetString();
            var workingDirectory = parameters.TryGetProperty("working_directory", out var wdProp)
                ? wdProp.GetString()
                : null;
            
            filePath = ResolveFilePath(filePath, workingDirectory);
            
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return Task.FromResult(ToolResponse.Error("INVALID_PARAMETERS", "文件路径不能为空"));
            }

            var exists = File.Exists(filePath);
            var isDirectory = Directory.Exists(filePath);

            return Task.FromResult(ToolResponse.Success(new
            {
                exists = exists || isDirectory,
                is_file = exists && !isDirectory,
                is_directory = isDirectory
            }));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResponse.Error("EXECUTION_FAILED", ex.Message));
        }
    }
}