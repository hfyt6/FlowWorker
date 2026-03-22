using System.IO;
using System.Text;
using System.Text.Json;
using FlowWorker.Core.Interfaces;

namespace FlowWorker.Core.Tools;

/// <summary>
/// 文件系统工具处理器
/// 提供文件读取、写入、删除、目录操作等功能
/// </summary>
public class FilesystemTool : IToolHandler
{
    public string Name => "Filesystem";

    public async Task<ToolResponse> ExecuteAsync(string action, JsonElement parameters)
    {
        return action.ToLowerInvariant() switch
        {
            "read_file" => await ReadFileAsync(parameters),
            "write_file" => await WriteFileAsync(parameters),
            "list_files" => await ListFilesAsync(parameters),
            "file_exists" => await FileExistsAsync(parameters),
            "delete_file" => await DeleteFileAsync(parameters),
            "create_directory" => await CreateDirectoryAsync(parameters),
            _ => ToolResponse.Error("UNKNOWN_ACTION", $"未知的操作：{action}")
        };
    }

    /// <summary>
    /// 读取文件内容
    /// </summary>
    private async Task<ToolResponse> ReadFileAsync(JsonElement parameters)
    {
        try
        {
            var filePath = parameters.GetProperty("file_path").GetString();
            
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

    /// <summary>
    /// 写入文件内容
    /// </summary>
    private async Task<ToolResponse> WriteFileAsync(JsonElement parameters)
    {
        try
        {
            var filePath = parameters.GetProperty("file_path").GetString();
            var content = parameters.GetProperty("content").GetString();
            
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

            // 确保目录存在
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

    /// <summary>
    /// 列出目录文件
    /// </summary>
    private Task<ToolResponse> ListFilesAsync(JsonElement parameters)
    {
        try
        {
            var directoryPath = parameters.GetProperty("directory_path").GetString();
            
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

            // 列出文件
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

            // 列出目录（仅在递归模式下）
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

    /// <summary>
    /// 检查文件是否存在
    /// </summary>
    private Task<ToolResponse> FileExistsAsync(JsonElement parameters)
    {
        try
        {
            var filePath = parameters.GetProperty("file_path").GetString();
            
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

    /// <summary>
    /// 删除文件
    /// </summary>
    private Task<ToolResponse> DeleteFileAsync(JsonElement parameters)
    {
        try
        {
            var filePath = parameters.GetProperty("file_path").GetString();
            
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return Task.FromResult(ToolResponse.Error("INVALID_PARAMETERS", "文件路径不能为空"));
            }

            var recursive = parameters.TryGetProperty("recursive", out var recProp) 
                ? recProp.GetBoolean() 
                : false;

            int deletedCount = 0;

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                deletedCount = 1;
            }
            else if (Directory.Exists(filePath))
            {
                if (recursive)
                {
                    deletedCount = CountItems(filePath);
                    Directory.Delete(filePath, true);
                }
                else
                {
                    Directory.Delete(filePath, false);
                    deletedCount = 1;
                }
            }
            else
            {
                return Task.FromResult(ToolResponse.Error("FILE_NOT_FOUND", $"文件或目录不存在：{filePath}"));
            }

            return Task.FromResult(ToolResponse.Success(new
            {
                success = true,
                deleted_items = deletedCount
            }));
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

    /// <summary>
    /// 创建目录
    /// </summary>
    private Task<ToolResponse> CreateDirectoryAsync(JsonElement parameters)
    {
        try
        {
            var directoryPath = parameters.GetProperty("directory_path").GetString();
            
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

    private int CountItems(string path)
    {
        try
        {
            var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
            var dirs = Directory.GetDirectories(path, "*", SearchOption.AllDirectories);
            return files.Length + dirs.Length + 1; // +1 for the root directory
        }
        catch
        {
            return 1;
        }
    }
}