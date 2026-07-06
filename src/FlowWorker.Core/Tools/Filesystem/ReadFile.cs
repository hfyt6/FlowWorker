using System.IO;
using System.Text;
using System.Text.Json;
using FlowWorker.Core.Interfaces;

namespace FlowWorker.Core.Tools.Filesystem;

/// <summary>
/// 文件系统工具处理器 - 读取文件
/// </summary>
public partial class FilesystemTool
{
    /// <summary>
    /// 解析文件路径，如果是相对路径则转换为绝对路径（相对于工作目录）
    /// </summary>
    private string ResolveFilePath(string filePath, string? workingDirectory = null)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return filePath;
        }

        if (Path.IsPathRooted(filePath))
        {
            return filePath;
        }

        if (!string.IsNullOrWhiteSpace(workingDirectory))
        {
            return Path.Combine(workingDirectory, filePath);
        }

        return Path.GetFullPath(filePath);
    }

    /// <summary>
    /// 读取文件内容
    /// </summary>
    private async Task<ToolResponse> ReadFileAsync(JsonElement parameters)
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
                return ToolResponse.Error("INVALID_PARAMETERS", "文件路径不能为空");
            }

            if (!File.Exists(filePath))
            {
                return ToolResponse.Error("FILE_NOT_FOUND", $"文件不存在：{filePath}");
            }

            var encoding = parameters.TryGetProperty("encoding", out var encProp) 
                ? Encoding.GetEncoding(encProp.GetString() ?? "utf-8") 
                : Encoding.UTF8;

            var content = await File.ReadAllTextAsync(filePath, encoding);
            var fileInfo = new FileInfo(filePath);

            return ToolResponse.Success(new
            {
                content = content,
                encoding = encoding.WebName,
                size = fileInfo.Length
            });
        }
        catch (Exception ex)
        {
            return ToolResponse.Error("EXECUTION_FAILED", ex.Message);
        }
    }
}