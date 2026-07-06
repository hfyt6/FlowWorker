using System.IO;
using System.Text.Json;
using FlowWorker.Core.Interfaces;

namespace FlowWorker.Core.Tools.Filesystem;

/// <summary>
/// 文件系统工具处理器 - 创建目录
/// </summary>
public partial class FilesystemTool
{
    /// <summary>
    /// 创建目录
    /// </summary>
    private Task<ToolResponse> CreateDirectoryAsync(JsonElement parameters)
    {
        try
        {
            var directoryPath = parameters.GetProperty("directory_path").GetString();
            var workingDirectory = parameters.TryGetProperty("working_directory", out var wdProp)
                ? wdProp.GetString()
                : null;
            
            directoryPath = ResolveFilePath(directoryPath, workingDirectory);
            
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                return Task.FromResult(ToolResponse.Error("INVALID_PARAMETERS", "目录路径不能为空"));
            }

            if (Directory.Exists(directoryPath))
            {
                return Task.FromResult(ToolResponse.Error("ALREADY_EXISTS", $"目录已存在：{directoryPath}"));
            }

            var recursive = parameters.TryGetProperty("recursive", out var recProp) 
                ? recProp.GetBoolean() 
                : true;

            var createdDir = Directory.CreateDirectory(directoryPath);

            return Task.FromResult(ToolResponse.Success(new
            {
                success = true,
                created_path = createdDir.FullName
            }));
        }
        catch (UnauthorizedAccessException)
        {
            return Task.FromResult(ToolResponse.Error("PERMISSION_DENIED", "没有权限创建目录"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResponse.Error("EXECUTION_FAILED", ex.Message));
        }
    }
}