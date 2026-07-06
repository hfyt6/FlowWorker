using System.IO;
using System.Text;
using System.Text.Json;
using FlowWorker.Core.Interfaces;

namespace FlowWorker.Core.Tools.Filesystem;

/// <summary>
/// 文件系统工具处理器 - 写入文件
/// </summary>
public partial class FilesystemTool
{
    /// <summary>
    /// 写入文件内容
    /// </summary>
    private async Task<ToolResponse> WriteFileAsync(JsonElement parameters)
    {
        try
        {
            var filePath = parameters.GetProperty("file_path").GetString();
            var content = parameters.GetProperty("content").GetString();
            var workingDirectory = parameters.TryGetProperty("working_directory", out var wdProp)
                ? wdProp.GetString()
                : null;
            
            filePath = ResolveFilePath(filePath, workingDirectory);
            
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return ToolResponse.Error("INVALID_PARAMETERS", "文件路径不能为空");
            }

            var encoding = parameters.TryGetProperty("encoding", out var encProp) 
                ? Encoding.GetEncoding(encProp.GetString() ?? "utf-8") 
                : Encoding.UTF8;

            var append = parameters.TryGetProperty("append", out var appendProp) 
                ? appendProp.GetBoolean() 
                : false;

            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (append)
            {
                await File.AppendAllTextAsync(filePath, content, encoding);
            }
            else
            {
                await File.WriteAllTextAsync(filePath, content, encoding);
            }

            var bytesWritten = encoding.GetByteCount(content ?? string.Empty);

            return ToolResponse.Success(new
            {
                success = true,
                bytes_written = bytesWritten,
                file_path = filePath
            });
        }
        catch (UnauthorizedAccessException)
        {
            return ToolResponse.Error("PERMISSION_DENIED", "没有权限写入文件");
        }
        catch (Exception ex)
        {
            return ToolResponse.Error("EXECUTION_FAILED", ex.Message);
        }
    }
}