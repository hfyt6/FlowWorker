using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using FlowWorker.Core.Interfaces;

namespace FlowWorker.Core.Tools;

/// <summary>
/// 版本控制工具处理器
/// 提供 Git 仓库状态、差异、提交历史、分支操作等功能
/// </summary>
public class VersionControlTool : IToolHandler
{
    public string Name => "VersionControl";

    public async Task<ToolResponse> ExecuteAsync(string action, JsonElement parameters)
    {
        return action.ToLowerInvariant() switch
        {
            "git_status" => await GitStatusAsync(parameters),
            "git_diff" => await GitDiffAsync(parameters),
            "git_log" => await GitLogAsync(parameters),
            "git_branch" => await GitBranchAsync(parameters),
            "git_commit" => await GitCommitAsync(parameters),
            _ => ToolResponse.Error("UNKNOWN_ACTION", $"未知的操作：{action}")
        };
    }

    private Task<ToolResponse> GitStatusAsync(JsonElement parameters)
    {
        try
        {
            var repositoryPath = parameters.GetProperty("repository_path").GetString();

            if (string.IsNullOrWhiteSpace(repositoryPath))
            {
                return Task.FromResult(ToolResponse.Error("INVALID_PARAMETERS", "仓库路径不能为空"));
            }

            if (!Directory.Exists(repositoryPath))
            {
                return Task.FromResult(ToolResponse.Error("INVALID_PARAMETERS", $"仓库路径不存在：{repositoryPath}"));
            }

            if (!IsGitRepository(repositoryPath))
            {
                return Task.FromResult(ToolResponse.Error("NOT_A_GIT_REPOSITORY", $"指定路径不是 Git 仓库：{repositoryPath}"));
            }

            var gitHelper = new GitHelper(repositoryPath);
            var status = gitHelper.GetStatus();

            return Task.FromResult(ToolResponse.Success(new
            {
                branch = status.Branch,
                is_clean = status.IsClean,
                modified = status.Modified,
                staged = status.Staged,
                untracked = status.Untracked,
                deleted = status.Deleted,
                renamed = status.Renamed,
                ahead = status.Ahead,
                behind = status.Behind
            }));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResponse.Error("EXECUTION_FAILED", $"获取 Git 状态失败：{ex.Message}"));
        }
    }

    private Task<ToolResponse> GitDiffAsync(JsonElement parameters)
    {
        try
        {
            var repositoryPath = parameters.GetProperty("repository_path").GetString();
            var commit1 = parameters.TryGetProperty("commit1", out var c1) ? c1.GetString() : null;
            var commit2 = parameters.TryGetProperty("commit2", out var c2) ? c2.GetString() : null;
            var filePath = parameters.TryGetProperty("file_path", out var fp) ? fp.GetString() : null;
            var cached = parameters.TryGetProperty("cached", out var cachedProp) && cachedProp.GetBoolean();

            if (string.IsNullOrWhiteSpace(repositoryPath))
            {
                return Task.FromResult(ToolResponse.Error("INVALID_PARAMETERS", "仓库路径不能为空"));
            }

            if (!IsGitRepository(repositoryPath))
            {
                return Task.FromResult(ToolResponse.Error("NOT_A_GIT_REPOSITORY", $"指定路径不是 Git 仓库：{repositoryPath}"));
            }

            var gitHelper = new GitHelper(repositoryPath);
            var diff = gitHelper.GetDiff(commit1, commit2, filePath, cached);

            return Task.FromResult(ToolResponse.Success(new
            {
                diff = diff.Diff,
                files_changed = diff.FilesChanged,
                insertions = diff.Insertions,
                deletions = diff.Deletions
            }));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResponse.Error("EXECUTION_FAILED", $"获取代码差异失败：{ex.Message}"));
        }
    }

    private Task<ToolResponse> GitLogAsync(JsonElement parameters)
    {
        try
        {
            var repositoryPath = parameters.GetProperty("repository_path").GetString();
            var limit = parameters.TryGetProperty("limit", out var limitProp) ? limitProp.GetInt32() : 20;
            var branch = parameters.TryGetProperty("branch", out var branchProp) ? branchProp.GetString() : null;
            var author = parameters.TryGetProperty("author", out var authorProp) ? authorProp.GetString() : null;
            var filePath = parameters.TryGetProperty("file_path", out var fileProp) ? fileProp.GetString() : null;

            if (string.IsNullOrWhiteSpace(repositoryPath))
            {
                return Task.FromResult(ToolResponse.Error("INVALID_PARAMETERS", "仓库路径不能为空"));
            }

            if (!IsGitRepository(repositoryPath))
            {
                return Task.FromResult(ToolResponse.Error("NOT_A_GIT_REPOSITORY", $"指定路径不是 Git 仓库：{repositoryPath}"));
            }

            var gitHelper = new GitHelper(repositoryPath);
            var log = gitHelper.GetLog(limit, branch, author, filePath);

            return Task.FromResult(ToolResponse.Success(new
            {
                commits = log.Commits,
                total_count = log.TotalCount
            }));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResponse.Error("EXECUTION_FAILED", $"获取提交历史失败：{ex.Message}"));
        }
    }

    private Task<ToolResponse> GitBranchAsync(JsonElement parameters)
    {
        try
        {
            var repositoryPath = parameters.GetProperty("repository_path").GetString();
            var action = parameters.GetProperty("action").GetString();
            var branchName = parameters.TryGetProperty("branch_name", out var bn) ? bn.GetString() : null;
            var baseBranch = parameters.TryGetProperty("base_branch", out var bb) ? bb.GetString() : null;
            var force = parameters.TryGetProperty("force", out var forceProp) && forceProp.GetBoolean();

            if (string.IsNullOrWhiteSpace(repositoryPath) || string.IsNullOrWhiteSpace(action))
            {
                return Task.FromResult(ToolResponse.Error("INVALID_PARAMETERS", "仓库路径和操作类型不能为空"));
            }

            if (!IsGitRepository(repositoryPath))
            {
                return Task.FromResult(ToolResponse.Error("NOT_A_GIT_REPOSITORY", $"指定路径不是 Git 仓库：{repositoryPath}"));
            }

            var gitHelper = new GitHelper(repositoryPath);
            BranchResult result;

            switch (action.ToLowerInvariant())
            {
                case "list":
                    result = gitHelper.ListBranches();
                    break;
                case "create":
                    if (string.IsNullOrWhiteSpace(branchName))
                    {
                        return Task.FromResult(ToolResponse.Error("INVALID_PARAMETERS", "创建分支时需要指定分支名称"));
                    }
                    result = gitHelper.CreateBranch(branchName, baseBranch);
                    break;
                case "switch":
                    if (string.IsNullOrWhiteSpace(branchName))
                    {
                        return Task.FromResult(ToolResponse.Error("INVALID_PARAMETERS", "切换分支时需要指定分支名称"));
                    }
                    result = gitHelper.SwitchBranch(branchName);
                    break;
                case "delete":
                    if (string.IsNullOrWhiteSpace(branchName))
                    {
                        return Task.FromResult(ToolResponse.Error("INVALID_PARAMETERS", "删除分支时需要指定分支名称"));
                    }
                    result = gitHelper.DeleteBranch(branchName, force);
                    break;
                default:
                    return Task.FromResult(ToolResponse.Error("INVALID_PARAMETERS", $"不支持的操作类型：{action}"));
            }

            return Task.FromResult(ToolResponse.Success(new
            {
                current_branch = result.CurrentBranch,
                branches = result.Branches,
                success = result.Success,
                message = result.Message
            }));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResponse.Error("EXECUTION_FAILED", $"分支操作失败：{ex.Message}"));
        }
    }

    private Task<ToolResponse> GitCommitAsync(JsonElement parameters)
    {
        try
        {
            var repositoryPath = parameters.GetProperty("repository_path").GetString();
            var message = parameters.GetProperty("message").GetString();
            var files = parameters.TryGetProperty("files", out var filesProp) 
                ? filesProp.EnumerateArray().Select(f => f.GetString()).ToList() 
                : null;
            var all = parameters.TryGetProperty("all", out var allProp) && allProp.GetBoolean();
            var amend = parameters.TryGetProperty("amend", out var amendProp) && amendProp.GetBoolean();

            if (string.IsNullOrWhiteSpace(repositoryPath) || string.IsNullOrWhiteSpace(message))
            {
                return Task.FromResult(ToolResponse.Error("INVALID_PARAMETERS", "仓库路径和提交信息不能为空"));
            }

            if (!IsGitRepository(repositoryPath))
            {
                return Task.FromResult(ToolResponse.Error("NOT_A_GIT_REPOSITORY", $"指定路径不是 Git 仓库：{repositoryPath}"));
            }

            var gitHelper = new GitHelper(repositoryPath);
            var result = gitHelper.Commit(message, files, all, amend);

            if (!result.Success)
            {
                return Task.FromResult(ToolResponse.Error("NOTHING_TO_COMMIT", result.Message));
            }

            return Task.FromResult(ToolResponse.Success(new
            {
                success = result.Success,
                commit_hash = result.CommitHash,
                message = result.Message
            }));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResponse.Error("EXECUTION_FAILED", $"提交代码失败：{ex.Message}"));
        }
    }

    private bool IsGitRepository(string path)
    {
        var gitDir = Path.Combine(path, ".git");
        return Directory.Exists(gitDir);
    }
}

/// <summary>
/// Git 辅助类
/// </summary>
public class GitHelper
{
    private readonly string _repositoryPath;

    public GitHelper(string repositoryPath)
    {
        _repositoryPath = repositoryPath;
    }

    public GitStatus GetStatus()
    {
        var status = new GitStatus();

        var branchOutput = ExecuteGitCommand("rev-parse --abbrev-ref HEAD");
        status.Branch = branchOutput.Trim();

        try
        {
            var aheadBehindOutput = ExecuteGitCommand($"rev-list --left-right --count {status.Branch}...origin/{status.Branch}");
            var parts = aheadBehindOutput.Trim().Split('\t');
            if (parts.Length == 2)
            {
                status.Behind = int.Parse(parts[0]);
                status.Ahead = int.Parse(parts[1]);
            }
        }
        catch { }

        var statusOutput = ExecuteGitCommand("status --porcelain");
        var lines = statusOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            if (line.Length < 3) continue;

            var indexStatus = line[0];
            var workTreeStatus = line[1];
            var filePath = line.Substring(3).Trim();

            if (filePath.Contains(" -> "))
            {
                var renameParts = filePath.Split(new[] { " -> " }, StringSplitOptions.None);
                status.Renamed.Add(new RenamedFile
                {
                    OldPath = renameParts[0],
                    NewPath = renameParts[1]
                });
                continue;
            }

            if (indexStatus != ' ' && indexStatus != '?')
            {
                status.Staged.Add(new FileStatus
                {
                    Path = filePath,
                    Status = GetStatusDescription(indexStatus)
                });
            }

            switch (workTreeStatus)
            {
                case 'M':
                    status.Modified.Add(new FileStatus { Path = filePath, Status = "modified" });
                    break;
                case 'D':
                    status.Deleted.Add(filePath);
                    break;
                case '?':
                    status.Untracked.Add(filePath);
                    break;
            }
        }

        status.IsClean = status.Modified.Count == 0 && 
                        status.Staged.Count == 0 && 
                        status.Untracked.Count == 0 && 
                        status.Deleted.Count == 0 &&
                        status.Renamed.Count == 0;

        return status;
    }

    public GitDiffResult GetDiff(string? commit1, string? commit2, string? filePath, bool cached)
    {
        var result = new GitDiffResult();
        var args = new StringBuilder("diff");

        if (cached) args.Append(" --cached");
        if (!string.IsNullOrEmpty(commit1)) args.Append($" {commit1}");
        if (!string.IsNullOrEmpty(commit2)) args.Append($" {commit2}");
        if (!string.IsNullOrEmpty(filePath)) args.Append($" -- \"{filePath}\"");

        result.Diff = ExecuteGitCommand(args.ToString());

        var statArgs = new StringBuilder("diff --stat");
        if (cached) statArgs.Append(" --cached");
        if (!string.IsNullOrEmpty(commit1)) statArgs.Append($" {commit1}");
        if (!string.IsNullOrEmpty(commit2)) statArgs.Append($" {commit2}");
        if (!string.IsNullOrEmpty(filePath)) statArgs.Append($" -- \"{filePath}\"");

        var statOutput = ExecuteGitCommand(statArgs.ToString());
        ParseDiffStat(statOutput, result);

        return result;
    }

    public GitLogResult GetLog(int limit, string? branch, string? author, string? filePath)
    {
        var result = new GitLogResult();
        var args = new StringBuilder($"log -{limit} --pretty=format:%H|%h|%s|%an|%ae|%ad|%ct");

        if (!string.IsNullOrEmpty(branch)) args.Append($" {branch}");
        if (!string.IsNullOrEmpty(author)) args.Append($" --author=\"{author}\"");
        if (!string.IsNullOrEmpty(filePath)) args.Append($" -- \"{filePath}\"");
        args.Append(" --date=short");

        var output = ExecuteGitCommand(args.ToString());
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var parts = line.Split('|');
            if (parts.Length >= 7)
            {
                var commit = new GitCommit
                {
                    Hash = parts[0],
                    ShortHash = parts[1],
                    Message = parts[2],
                    Author = parts[3],
                    Email = parts[4],
                    Date = parts[5],
                    Timestamp = long.Parse(parts[6])
                };

                result.Commits.Add(commit);
            }
        }

        result.TotalCount = result.Commits.Count;
        return result;
    }

    public BranchResult ListBranches()
    {
        var result = new BranchResult { Success = true };
        result.CurrentBranch = ExecuteGitCommand("rev-parse --abbrev-ref HEAD").Trim();

        var output = ExecuteGitCommand("branch -a --format=%(refname:short)|%(HEAD)");
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var parts = line.Split('|');
            if (parts.Length >= 2)
            {
                var branchName = parts[0].Trim();
                var isCurrent = parts[1].Trim() == "*";
                var isRemote = branchName.StartsWith("remotes/");

                result.Branches.Add(new BranchInfo
                {
                    Name = isRemote ? branchName.Replace("remotes/", "") : branchName,
                    IsCurrent = isCurrent,
                    IsRemote = isRemote
                });
            }
        }

        return result;
    }

    public BranchResult CreateBranch(string branchName, string? baseBranch)
    {
        var result = new BranchResult();

        try
        {
            var args = $"branch \"{branchName}\"";
            if (!string.IsNullOrEmpty(baseBranch)) args += $" \"{baseBranch}\"";

            ExecuteGitCommand(args);
            result.Success = true;
            result.Message = $"分支 '{branchName}' 创建成功";
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"创建分支失败：{ex.Message}";
        }

        var listResult = ListBranches();
        result.CurrentBranch = listResult.CurrentBranch;
        result.Branches = listResult.Branches;

        return result;
    }

    public BranchResult SwitchBranch(string branchName)
    {
        var result = new BranchResult();

        try
        {
            ExecuteGitCommand($"checkout \"{branchName}\"");
            result.Success = true;
            result.Message = $"已切换到分支 '{branchName}'";
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"切换分支失败：{ex.Message}";
        }

        var listResult = ListBranches();
        result.CurrentBranch = listResult.CurrentBranch;
        result.Branches = listResult.Branches;

        return result;
    }

    public BranchResult DeleteBranch(string branchName, bool force)
    {
        var result = new BranchResult();

        try
        {
            var flag = force ? "-D" : "-d";
            ExecuteGitCommand($"branch {flag} \"{branchName}\"");
            result.Success = true;
            result.Message = $"分支 '{branchName}' 已删除";
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"删除分支失败：{ex.Message}";
        }

        var listResult = ListBranches();
        result.CurrentBranch = listResult.CurrentBranch;
        result.Branches = listResult.Branches;

        return result;
    }

    public CommitResult Commit(string message, List<string>? files, bool all, bool amend)
    {
        var result = new CommitResult();

        try
        {
            if (all)
            {
                ExecuteGitCommand("add -A");
            }
            else if (files != null && files.Count > 0)
            {
                foreach (var file in files)
                {
                    ExecuteGitCommand($"add \"{file}\"");
                }
            }

            var args = new StringBuilder("commit");
            if (amend) args.Append(" --amend --no-edit");
            var escapedMessage = message.Replace("\"", "\\\"");
            args.Append($" -m \"{escapedMessage}\"");

            var output = ExecuteGitCommand(args.ToString());

            var match = Regex.Match(output, @"\[([\w\-]+)\s+([a-f0-9]+)\]");
            if (match.Success)
            {
                result.CommitHash = match.Groups[2].Value;
            }
            else
            {
                result.CommitHash = ExecuteGitCommand("rev-parse HEAD").Trim();
            }

            result.Success = true;
            result.Message = amend ? "已修改上一次提交" : "提交成功";
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = ex.Message;
        }

        return result;
    }

    private string ExecuteGitCommand(string arguments)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = arguments,
                WorkingDirectory = _repositoryPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            }
        };

        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0 && !string.IsNullOrEmpty(error))
        {
            throw new Exception(error.Trim());
        }

        return output;
    }

    private void ParseDiffStat(string statOutput, GitDiffResult result)
    {
        result.FilesChanged = new List<DiffFile>();
        var lines = statOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var match = Regex.Match(line, @"^\s*(.+?)\s*\|\s*(\d+)");
            if (match.Success)
            {
                var fileName = match.Groups[1].Value.Trim();
                var changes = line.Substring(match.Index + match.Length);
                var insertions = 0;
                var deletions = 0;
                foreach (var c in changes)
                {
                    if (c == '+') insertions++;
                    else if (c == '-') deletions++;
                }

                result.FilesChanged.Add(new DiffFile
                {
                    Path = fileName,
                    ChangeType = insertions > 0 && deletions > 0 ? "modified" : (insertions > 0 ? "added" : "deleted"),
                    Insertions = insertions,
                    Deletions = deletions
                });

                result.Insertions += insertions;
                result.Deletions += deletions;
            }
        }
    }

    private static string GetStatusDescription(char status)
    {
        return status switch
        {
            'A' => "added",
            'M' => "modified",
            'D' => "deleted",
            'R' => "renamed",
            'C' => "copied",
            'U' => "updated but unmerged",
            _ => "unknown"
        };
    }

    private static bool IsBranchCurrent(string branchName, string currentBranch)
    {
        return branchName == currentBranch || branchName == $"* {currentBranch}";
    }
}

/// <summary>
/// Git 数据模型类
/// </summary>
public class GitStatus
{
    public string Branch { get; set; } = string.Empty;
    public bool IsClean { get; set; }
    public List<FileStatus> Modified { get; set; } = new();
    public List<FileStatus> Staged { get; set; } = new();
    public List<string> Untracked { get; set; } = new();
    public List<string> Deleted { get; set; } = new();
    public List<RenamedFile> Renamed { get; set; } = new();
    public int Ahead { get; set; }
    public int Behind { get; set; }
}

public class FileStatus
{
    public string Path { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class RenamedFile
{
    public string OldPath { get; set; } = string.Empty;
    public string NewPath { get; set; } = string.Empty;
}

public class GitDiffResult
{
    public string Diff { get; set; } = string.Empty;
    public List<DiffFile> FilesChanged { get; set; } = new();
    public int Insertions { get; set; }
    public int Deletions { get; set; }
}

public class DiffFile
{
    public string Path { get; set; } = string.Empty;
    public string ChangeType { get; set; } = string.Empty;
    public int Insertions { get; set; }
    public int Deletions { get; set; }
}

public class GitLogResult
{
    public List<GitCommit> Commits { get; set; } = new();
    public int TotalCount { get; set; }
}

public class GitCommit
{
    public string Hash { get; set; } = string.Empty;
    public string ShortHash { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public long Timestamp { get; set; }
}

public class BranchResult
{
    public string CurrentBranch { get; set; } = string.Empty;
    public List<BranchInfo> Branches { get; set; } = new();
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class BranchInfo
{
    public string Name { get; set; } = string.Empty;
    public bool IsCurrent { get; set; }
    public bool IsRemote { get; set; }
}

public class CommitResult
{
    public bool Success { get; set; }
    public string CommitHash { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}