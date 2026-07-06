using System.IO;
using System.Text.Json;
using FlowWorker.Core.Interfaces;

namespace FlowWorker.Core.Tools.Filesystem;

/// <summary>
/// 文件系统工具处理器 - 删除文件
/// </summary>
public partial class FilesystemTool
{
    /// <summary>
    /// 删除文件或目录
    /// </summary>
    private Task<ToolResponse> DeleteFileAsync(JsonElement parameters)
    {
        try
        {
            var filePath = parameters.GetProperty("file_path").GetString();
            var workingDirectory = parameters.TryGetProperty("working_directory", out var wdProp) ? wdProp.GetString() : null;
            
            filePath = ResolveFilePath(filePath, workingDirectory);
            
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return Task.FromResult(ToolResponse.Error("INVALID_PARAMETERS", "文件路径不能为空"));
            }

            var recursive = parameters.TryGetProperty("recursive", out var recProp) ? recProp.GetBoolean() : false;

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                return Task.FromResult(ToolResponse.Success(new { success = true, message = $"文件已删除: {filePath}" }));
            }
            else if (Directory.Exists(filePath))
            {
                Directory.Delete(filePath, recursive);
                return Task.FromResult(ToolResponse.Success(new { success = true, message = $"目录已删除: {filePath}" }));
            }
            else
            {
                return Task.FromResult(ToolResponse.Error("FILE_NOT_FOUND", $"文件或目录不存在: {filePath}"));
            }
        }
        catch (UnauthorizedAccessException)
        {
            return Task.FromResult(ToolResponse.Error("PERMISSION_DENIED", "没有权限删除文件"));
        }
        catch (IOException ex)
        {
            return Task.FromResult(ToolResponse.Error("EXECUTION_FAILED", $"文件正在使用中：{ex.Message}"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResponse.Error("EXECUTION_FAILED", ex.Message));
        }
    }
}