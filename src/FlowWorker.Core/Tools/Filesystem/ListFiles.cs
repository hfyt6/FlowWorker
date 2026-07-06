using System.IO;
using System.Text.Json;
using FlowWorker.Core.Interfaces;

namespace FlowWorker.Core.Tools.Filesystem;

/// <summary>
/// 文件系统工具处理器 - 列出文件
/// </summary>
public partial class FilesystemTool
{
    /// <summary>
    /// 列出目录文件
    /// </summary>
    private Task<ToolResponse> ListFilesAsync(JsonElement parameters)
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

            if (!Directory.Exists(directoryPath))
            {
                return Task.FromResult(ToolResponse.Error("DIRECTORY_NOT_FOUND", $"目录不存在：{directoryPath}"));
            }

            var recursive = parameters.TryGetProperty("recursive", out var recProp) 
                ? recProp.GetBoolean() 
                : false;

            var pattern = parameters.TryGetProperty("pattern", out var patProp) 
                ? patProp.GetString() 
                : "*";

            var files = new List<object>();
            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            var fileEntries = Directory.GetFiles(directoryPath, pattern ?? "*", searchOption);
            foreach (var file in fileEntries)
            {
                var fileInfo = new FileInfo(file);
                files.Add(new
                {
                    name = fileInfo.Name,
                    path = fileInfo.FullName,
                    type = "file",
                    size = fileInfo.Length,
                    created_at = fileInfo.CreationTimeUtc.ToString("O"),
                    modified_at = fileInfo.LastWriteTimeUtc.ToString("O")
                });
            }

            if (recursive)
            {
                var dirEntries = Directory.GetDirectories(directoryPath, "*", searchOption);
                foreach (var dir in dirEntries)
                {
                    var dirInfo = new DirectoryInfo(dir);
                    files.Add(new
                    {
                        name = dirInfo.Name,
                        path = dirInfo.FullName,
                        type = "directory",
                        size = 0L,
                        created_at = dirInfo.CreationTimeUtc.ToString("O"),
                        modified_at = dirInfo.LastWriteTimeUtc.ToString("O")
                    });
                }
            }

            return Task.FromResult(ToolResponse.Success(new
            {
                files = files.OrderBy(f => ((dynamic)f).type).ThenBy(f => ((dynamic)f).name).ToList(),
                total_count = files.Count
            }));
        }
        catch (UnauthorizedAccessException)
        {
            return Task.FromResult(ToolResponse.Error("PERMISSION_DENIED", "没有权限访问目录"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResponse.Error("EXECUTION_FAILED", ex.Message));
        }
    }
}