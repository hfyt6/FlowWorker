using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using SysProcess = System.Diagnostics.Process;
using ProcessStartInfo = System.Diagnostics.ProcessStartInfo;

namespace FlowWorker.Plugins.Process
{
    /// <summary>
    /// 进程管理工具处理器，支持Windows、Linux和macOS系统
    /// </summary>
    public class ProcessHandler
    {
        private static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        private static readonly bool IsLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        private static readonly bool IsMacOS = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        private static readonly bool IsUnixLike = IsLinux || IsMacOS;

        #region Security Configuration

        /// <summary>
        /// 危险命令模式列表 - 这些命令可能破坏系统或造成数据丢失
        /// </summary>
        private static readonly HashSet<string> DangerousCommandPatterns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // 文件系统破坏性操作
            "rm -rf /", "rm -rf /*", "rm -rf ~", "rm -rf ~/*",
            "del /f /s /q c:\\", "rmdir /s /q c:\\",
            "format ", "format:", "mkfs.", "dd if=", "dd of=/dev/",
            
            // 系统关键文件操作
            "> /etc/passwd", "> /etc/shadow", "> /etc/hosts",
            ":(){ :|:& };:", // Fork bomb
            
            // 权限提升和敏感操作
            "chmod -R 777 /", "chmod 777 /",
            "chown -R root:root /", "chown root:root /",
            
            // 网络危险操作
            "iptables -F", "iptables --flush",
            "ufw disable", "firewall-cmd --disable",
            
            // Windows 危险命令
            "rd /s /q c:\\", "deltree /y c:\\",
            "erase /f /s /q c:\\", "format c:",
            "diskpart /s", // 配合脚本可能危险
        };

        /// <summary>
        /// 需要警告的高风险命令模式
        /// </summary>
        private static readonly List<Regex> HighRiskPatterns = new List<Regex>
        {
            // 递归删除
            new Regex(@"\brm\s+-[a-zA-Z]*r[a-zA-Z]*\s+.*(/|~|\\|C:\\|D:\\)", RegexOptions.IgnoreCase),
            new Regex(@"\bdel\s+/[a-zA-Z]*s[a-zA-Z]*\s+.*(\\|C:\\|D:\\)", RegexOptions.IgnoreCase),
            
            // 强制操作
            new Regex(@"\b(rm|del|rd|rmdir|deltree)\s+.*\s+(/-?[fF]|/-?[qQ]|/-?[yY]|/-?[sS])", RegexOptions.IgnoreCase),
            
            // 系统目录操作
            new Regex(@"\b(cp|mv|rm|del|rd)\s+.*\s+(C:\\Windows|/usr|/bin|/sbin|/lib|/sys|/dev|/boot)", RegexOptions.IgnoreCase),
            
            // 远程执行
            new Regex(@"\b(curl|wget)\s+.*\|\s*(sh|bash|zsh|powershell|cmd)", RegexOptions.IgnoreCase),
            new Regex(@"\b(curl|wget)\s+.*-O\s*-.*\|\s*(sh|bash|zsh)", RegexOptions.IgnoreCase),
            
            // 编码/解码执行（可能隐藏恶意命令）
            new Regex(@"\b(base64|xxd|openssl enc)\s+.*\|\s*(sh|bash|zsh|eval)", RegexOptions.IgnoreCase),
            
            // 下载执行
            new Regex(@"\b(invoke-expression|iex)\s+.*\(.*(new-object|net\.webclient|downloadstring)", RegexOptions.IgnoreCase),
            new Regex(@"\b(invoke-webrequest|iwr|wget|curl)\s+.*\|\s*(iex|invoke-expression)", RegexOptions.IgnoreCase),
        };

        /// <summary>
        /// 允许的命令白名单（可选使用）
        /// </summary>
        private static readonly HashSet<string> AllowedCommands = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // 文件操作
            "ls", "dir", "cat", "type", "head", "tail", "more", "less",
            "find", "grep", "where", "which",
            "cp", "copy", "xcopy", "robocopy",
            "mv", "move", "ren", "rename",
            "mkdir", "md", "rmdir", "rd",
            "touch", "echo",
            
            // 进程管理
            "ps", "tasklist", "top", "htop",
            "kill", "taskkill", "pkill", "killall",
            
            // 系统信息
            "uname", "ver", "systeminfo", "hostname",
            "whoami", "id", "groups",
            "df", "du", "diskusage", "fsutil",
            "free", "vmstat", "systeminfo",
            
            // 网络
            "ping", "tracert", "traceroute", "nslookup", "dig",
            "netstat", "ss", "ipconfig", "ifconfig", "ip",
            "curl", "wget", "Invoke-WebRequest",
            
            // 开发工具
            "git", "dotnet", "node", "npm", "python", "python3", "pip",
            "docker", "kubectl", "helm",
            "code", "vim", "vi", "nano", "notepad",
            
            // 压缩
            "tar", "zip", "unzip", "gzip", "gunzip", "7z", "rar",
            
            // 其他常用
            "cd", "pwd", "chdir", "exit", "clear", "cls",
            "date", "time", "cal", "uptime",
            "ssh", "scp", "sftp",
            "chmod", "chown", "sudo",
        };

        #endregion

        #region Security Validation

        /// <summary>
        /// 命令安全审查结果
        /// </summary>
        public class SecurityValidationResult
        {
            public bool IsAllowed { get; set; }
            public string RiskLevel { get; set; } // "safe", "warning", "dangerous", "blocked"
            public string Message { get; set; }
            public List<string> Warnings { get; set; } = new List<string>();
        }

        /// <summary>
        /// 对命令进行安全审查
        /// </summary>
        private SecurityValidationResult ValidateCommandSecurity(string command, string[] args)
        {
            var result = new SecurityValidationResult();
            var fullCommand = (command + " " + string.Join(" ", args ?? Array.Empty<string>())).Trim();
            var normalizedCommand = NormalizeCommand(fullCommand);

            // 1. 检查绝对禁止的危险命令
            foreach (var dangerousPattern in DangerousCommandPatterns)
            {
                if (normalizedCommand.Contains(dangerousPattern, StringComparison.OrdinalIgnoreCase))
                {
                    result.IsAllowed = false;
                    result.RiskLevel = "blocked";
                    result.Message = $"检测到危险的命令模式: '{dangerousPattern}'。此命令被禁止执行以防止系统损坏。";
                    return result;
                }
            }

            // 2. 检查高风险模式
            var highRiskWarnings = new List<string>();
            foreach (var pattern in HighRiskPatterns)
            {
                if (pattern.IsMatch(fullCommand))
                {
                    highRiskWarnings.Add($"匹配高风险模式: {pattern.ToString()}");
                }
            }

            // 3. 检查命令注入尝试
            if (DetectCommandInjection(fullCommand))
            {
                highRiskWarnings.Add("检测到可能的命令注入尝试");
            }

            // 4. 检查路径遍历
            if (DetectPathTraversal(fullCommand))
            {
                highRiskWarnings.Add("检测到可能的路径遍历攻击");
            }

            // 5. 检查是否使用shell执行多命令
            if (ContainsMultipleCommands(fullCommand))
            {
                result.Warnings.Add("命令包含多个子命令，请确保您了解每个子命令的作用");
            }

            // 设置结果
            if (highRiskWarnings.Count > 0)
            {
                result.IsAllowed = true; // 允许但警告
                result.RiskLevel = "warning";
                result.Message = "命令存在潜在风险，请谨慎执行";
                result.Warnings.AddRange(highRiskWarnings);
            }
            else
            {
                result.IsAllowed = true;
                result.RiskLevel = "safe";
                result.Message = "命令通过安全检查";
            }

            return result;
        }

        /// <summary>
        /// 规范化命令用于比较
        /// </summary>
        private string NormalizeCommand(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
                return string.Empty;

            // 移除多余空格
            var normalized = Regex.Replace(command.Trim(), @"\s+", " ");
            
            // 统一路径分隔符
            normalized = normalized.Replace("\\", "/");
            
            // 统一引号
            normalized = normalized.Replace("'", "\"");
            
            return normalized.ToLowerInvariant();
        }

        /// <summary>
        /// 检测命令注入尝试
        /// </summary>
        private bool DetectCommandInjection(string command)
        {
            // 检测常见的命令注入模式
            var injectionPatterns = new[]
            {
                ";", "&&", "||", "|", "`", "$", 
                "$(", "${", "<(", ">(",
                "\n", "\r",
            };

            // 如果在引号外发现这些字符，可能是注入
            var inQuote = false;
            var quoteChar = '\0';
            
            for (int i = 0; i < command.Length; i++)
            {
                var c = command[i];
                
                if ((c == '"' || c == '\'') && (i == 0 || command[i - 1] != '\\'))
                {
                    if (!inQuote)
                    {
                        inQuote = true;
                        quoteChar = c;
                    }
                    else if (c == quoteChar)
                    {
                        inQuote = false;
                    }
                }
                else if (!inQuote)
                {
                    foreach (var pattern in injectionPatterns)
                    {
                        if (i + pattern.Length <= command.Length && 
                            command.Substring(i, pattern.Length) == pattern)
                        {
                            return true;
                        }
                    }
                }
            }
            
            return false;
        }

        /// <summary>
        /// 检测路径遍历攻击
        /// </summary>
        private bool DetectPathTraversal(string command)
        {
            // 检测路径遍历模式
            var traversalPatterns = new[]
            {
                "../", "..\\", "..../", "....\\",
                "/../", "\\..\\",
            };

            return traversalPatterns.Any(pattern => 
                command.Contains(pattern, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 检查是否包含多个命令
        /// </summary>
        private bool ContainsMultipleCommands(string command)
        {
            // 检测多命令执行符
            var multiCommandPatterns = new[] { ";", "&&", "||" };
            
            // 排除管道符（单个命令内的管道是合法的）
            var commandWithoutPipes = Regex.Replace(command, @"\|[^|&;]*", "");
            
            return multiCommandPatterns.Any(pattern => 
                commandWithoutPipes.Contains(pattern));
        }

        #endregion

        #region Execute Command

        /// <summary>
        /// 执行系统命令
        /// </summary>
        public async Task<ToolResponse> ExecuteCommandAsync(JsonElement parameters)
        {
            try
            {
                var command = parameters.GetProperty("command").GetString();
                if (string.IsNullOrWhiteSpace(command))
                {
                    return ToolResponse.Error("INVALID_PARAMETERS", "命令不能为空");
                }

                var args = new List<string>();
                if (parameters.TryGetProperty("args", out var argsProp) && argsProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var arg in argsProp.EnumerateArray())
                    {
                        args.Add(arg.GetString() ?? string.Empty);
                    }
                }

                // 安全检查
                var securityCheck = parameters.TryGetProperty("skip_security_check", out var skipCheckProp) 
                    && skipCheckProp.GetBoolean();
                
                if (!securityCheck)
                {
                    var validationResult = ValidateCommandSecurity(command, args.ToArray());
                    
                    if (!validationResult.IsAllowed)
                    {
                        return ToolResponse.Error("SECURITY_BLOCKED", validationResult.Message);
                    }
                    
                    // 如果有警告，检查是否需要用户确认
                    if (validationResult.RiskLevel == "warning")
                    {
                        var confirmed = parameters.TryGetProperty("require_confirmation", out var confirmProp) 
                            && confirmProp.GetBoolean();
                        
                        if (!confirmed)
                        {
                            // 返回需要确认的错误，包含安全信息
                            return ToolResponse.Error("REQUIRES_CONFIRMATION", $"命令存在安全风险: {validationResult.Message}", new
                            {
                                risk_level = validationResult.RiskLevel,
                                message = validationResult.Message,
                                warnings = validationResult.Warnings,
                                command = command,
                                args = args.ToArray()
                            });
                        }
                        // 用户已确认，继续执行
                    }
                }

                var workingDir = parameters.TryGetProperty("working_directory", out var wdProp)
                    ? wdProp.GetString()
                    : null;

                var timeout = parameters.TryGetProperty("timeout", out var timeoutProp)
                    ? timeoutProp.GetInt32()
                    : 30000;

                // 根据操作系统选择合适的shell
                string shellPath;
                string shellArgs;
                
                if (IsWindows)
                {
                    shellPath = "cmd.exe";
                    shellArgs = $"/c \"{EscapeShellCommand(command)} {string.Join(" ", args.Select(EscapeArgument))}\"";
                }
                else if (IsMacOS)
                {
                    // macOS 优先使用 zsh，回退到 bash
                    shellPath = File.Exists("/bin/zsh") ? "/bin/zsh" : "/bin/bash";
                    shellArgs = $"-c \"{EscapeShellCommand(command)} {string.Join(" ", args.Select(EscapeArgument))}\"";
                }
                else // Linux
                {
                    shellPath = "/bin/bash";
                    shellArgs = $"-c \"{EscapeShellCommand(command)} {string.Join(" ", args.Select(EscapeArgument))}\"";
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = shellPath,
                    Arguments = shellArgs,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                if (!string.IsNullOrWhiteSpace(workingDir) && Directory.Exists(workingDir))
                {
                    startInfo.WorkingDirectory = workingDir;
                }

                // 设置环境变量
                if (parameters.TryGetProperty("environment", out var envProp) && envProp.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in envProp.EnumerateObject())
                    {
                        startInfo.Environment[prop.Name] = prop.Value.GetString() ?? string.Empty;
                    }
                }

                var stopwatch = Stopwatch.StartNew();
                using var process = SysProcess.Start(startInfo);
                if (process == null)
                {
                    return ToolResponse.Error("EXECUTION_FAILED", "无法启动进程");
                }

                var cts = new CancellationTokenSource(timeout);
                try
                {
                    await Task.WhenAll(
                        process.WaitForExitAsync(cts.Token),
                        Task.Delay(timeout, cts.Token)
                    );
                }
                catch (OperationCanceledException)
                {
                    try { process.Kill(); } catch { }
                    return ToolResponse.Error("TIMEOUT", $"命令执行超时（{timeout}ms）");
                }

                stopwatch.Stop();

                var stdout = await process.StandardOutput.ReadToEndAsync();
                var stderr = await process.StandardError.ReadToEndAsync();

                return ToolResponse.Success(new
                {
                    exit_code = process.ExitCode,
                    stdout = stdout,
                    stderr = stderr,
                    execution_time = (int)stopwatch.ElapsedMilliseconds
                });
            }
            catch (Exception ex)
            {
                return ToolResponse.Error("EXECUTION_FAILED", ex.Message);
            }
        }

        #endregion

        #region Run Process

        /// <summary>
        /// 运行进程
        /// </summary>
        public Task<ToolResponse> RunProcessAsync(JsonElement parameters)
        {
            try
            {
                var command = parameters.GetProperty("command").GetString();
                if (string.IsNullOrWhiteSpace(command))
                {
                    return Task.FromResult(ToolResponse.Error("INVALID_PARAMETERS", "命令不能为空"));
                }

                var args = new List<string>();
                if (parameters.TryGetProperty("args", out var argsProp) && argsProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var arg in argsProp.EnumerateArray())
                    {
                        args.Add(arg.GetString() ?? string.Empty);
                    }
                }

                // 安全检查
                var securityCheck = parameters.TryGetProperty("skip_security_check", out var skipCheckProp) 
                    && skipCheckProp.GetBoolean();
                
                if (!securityCheck)
                {
                    var validationResult = ValidateCommandSecurity(command, args.ToArray());
                    
                    if (!validationResult.IsAllowed)
                    {
                        return Task.FromResult(ToolResponse.Error("SECURITY_BLOCKED", validationResult.Message));
                    }
                }

                var workingDir = parameters.TryGetProperty("working_directory", out var wdProp)
                    ? wdProp.GetString()
                    : null;

                var redirectStdout = parameters.TryGetProperty("redirect_stdout", out var redirectOutProp)
                    ? redirectOutProp.GetBoolean()
                    : true;

                var redirectStderr = parameters.TryGetProperty("redirect_stderr", out var redirectErrProp)
                    ? redirectErrProp.GetBoolean()
                    : true;

                var startInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = string.Join(" ", args.Select(EscapeArgument)),
                    RedirectStandardOutput = redirectStdout,
                    RedirectStandardError = redirectStderr,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                if (!string.IsNullOrWhiteSpace(workingDir) && Directory.Exists(workingDir))
                {
                    startInfo.WorkingDirectory = workingDir;
                }

                // 设置环境变量
                if (parameters.TryGetProperty("environment", out var envProp) && envProp.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in envProp.EnumerateObject())
                    {
                        startInfo.Environment[prop.Name] = prop.Value.GetString() ?? string.Empty;
                    }
                }

                var process = SysProcess.Start(startInfo);
                if (process == null)
                {
                    return Task.FromResult(ToolResponse.Error("EXECUTION_FAILED", "无法启动进程"));
                }

                return Task.FromResult(ToolResponse.Success(new
                {
                    pid = process.Id,
                    process_name = process.ProcessName,
                    start_time = DateTime.UtcNow.ToString("O")
                }));
            }
            catch (Exception ex)
            {
                return Task.FromResult(ToolResponse.Error("EXECUTION_FAILED", ex.Message));
            }
        }

        #endregion

        #region Kill Process

        /// <summary>
        /// 终止进程
        /// </summary>
        public Task<ToolResponse> KillProcessAsync(JsonElement parameters)
        {
            try
            {
                var pid = parameters.GetProperty("pid").GetInt32();
                if (pid <= 0)
                {
                    return Task.FromResult(ToolResponse.Error("INVALID_PARAMETERS", "无效的进程ID"));
                }

                var force = parameters.TryGetProperty("force", out var forceProp) && forceProp.GetBoolean();

                try
                {
                    var process = SysProcess.GetProcessById(pid);
                    if (process.HasExited)
                    {
                        return Task.FromResult(ToolResponse.Error("PROCESS_NOT_FOUND", $"进程 {pid} 已退出"));
                    }

                    if (force)
                    {
                        process.Kill(true); // 强制终止整个进程树
                    }
                    else
                    {
                        process.CloseMainWindow();
                        if (!process.WaitForExit(5000))
                        {
                            process.Kill();
                        }
                    }

                    return Task.FromResult(ToolResponse.Success(new
                    {
                        success = true,
                        message = $"进程 {pid} 已终止"
                    }));
                }
                catch (ArgumentException)
                {
                    return Task.FromResult(ToolResponse.Error("PROCESS_NOT_FOUND", $"进程 {pid} 不存在"));
                }
                catch (InvalidOperationException)
                {
                    return Task.FromResult(ToolResponse.Error("PROCESS_NOT_FOUND", $"进程 {pid} 已退出"));
                }
            }
            catch (Exception ex)
            {
                return Task.FromResult(ToolResponse.Error("EXECUTION_FAILED", ex.Message));
            }
        }

        #endregion

        #region List Processes

        /// <summary>
        /// 列出进程
        /// </summary>
        public async Task<ToolResponse> ListProcessesAsync(JsonElement parameters)
        {
            try
            {
                var filter = parameters.TryGetProperty("filter", out var filterProp)
                    ? filterProp.GetString()?.ToLowerInvariant()
                    : null;

                var includeSystem = parameters.TryGetProperty("include_system", out var sysProp) && sysProp.GetBoolean();

                var processes = new List<object>();

                if (IsWindows)
                {
                    processes = await ListWindowsProcessesAsync(filter, includeSystem);
                }
                else if (IsLinux)
                {
                    processes = await ListLinuxProcessesAsync(filter, includeSystem);
                }
                else if (IsMacOS)
                {
                    processes = await ListMacOSProcessesAsync(filter, includeSystem);
                }
                else
                {
                    // 使用通用方法
                    processes = ListGenericProcesses(filter, includeSystem);
                }

                return ToolResponse.Success(new
                {
                    processes = processes,
                    total_count = processes.Count
                });
            }
            catch (Exception ex)
            {
                return ToolResponse.Error("EXECUTION_FAILED", ex.Message);
            }
        }

        private async Task<List<object>> ListWindowsProcessesAsync(string filter, bool includeSystem)
        {
            var processes = new List<object>();

            // 使用 PowerShell 获取更详细的进程信息
            var startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = "-Command \"Get-Process | Select-Object Id, ProcessName, Path, CPU, WorkingSet, StartTime | ConvertTo-Json -Compress\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

                using var process = SysProcess.Start(startInfo);
            if (process != null)
            {
                var output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                try
                {
                    var json = JsonDocument.Parse(output);
                    if (json.RootElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var proc in json.RootElement.EnumerateArray())
                        {
                            var name = proc.TryGetProperty("ProcessName", out var nameProp) ? nameProp.GetString() : "Unknown";
                            var pid = proc.TryGetProperty("Id", out var idProp) ? idProp.GetInt32() : 0;
                            var path = proc.TryGetProperty("Path", out var pathProp) ? pathProp.GetString() : "";
                            var cpu = proc.TryGetProperty("CPU", out var cpuProp) && cpuProp.ValueKind != JsonValueKind.Null ? cpuProp.GetDouble() : 0;
                            var workingSet = proc.TryGetProperty("WorkingSet", out var memProp) && memProp.ValueKind != JsonValueKind.Null ? memProp.GetInt64() : 0;

                            // 过滤系统进程
                            if (!includeSystem && IsSystemProcess(name, path))
                                continue;

                            // 应用过滤器
                            if (!string.IsNullOrWhiteSpace(filter) &&
                                !name.ToLowerInvariant().Contains(filter) &&
                                !(path?.ToLowerInvariant().Contains(filter) ?? false))
                                continue;

                            processes.Add(new
                            {
                                pid = pid,
                                name = name,
                                command_line = path ?? name,
                                cpu_percent = Math.Round(cpu, 2),
                                memory_mb = Math.Round(workingSet / (1024.0 * 1024.0), 2),
                                status = "running",
                                start_time = proc.TryGetProperty("StartTime", out var timeProp) && timeProp.ValueKind != JsonValueKind.Null
                                    ? timeProp.GetDateTime().ToString("O")
                                    : null
                            });
                        }
                    }
                }
                catch { }
            }

            // 如果 PowerShell 方法失败，使用 .NET 方法
            if (processes.Count == 0)
            {
                foreach (var proc in SysProcess.GetProcesses())
                {
                    try
                    {
                        if (!includeSystem && IsSystemProcess(proc.ProcessName, proc.MainModule?.FileName))
                            continue;

                        if (!string.IsNullOrWhiteSpace(filter) &&
                            !proc.ProcessName.ToLowerInvariant().Contains(filter))
                            continue;

                        processes.Add(new
                        {
                            pid = proc.Id,
                            name = proc.ProcessName,
                            command_line = proc.MainModule?.FileName ?? proc.ProcessName,
                            cpu_percent = 0,
                            memory_mb = Math.Round(proc.WorkingSet64 / (1024.0 * 1024.0), 2),
                            status = proc.Responding ? "running" : "unknown",
                            start_time = proc.StartTime.ToUniversalTime().ToString("O")
                        });
                    }
                    catch { }
                }
            }

            return processes;
        }

        private async Task<List<object>> ListLinuxProcessesAsync(string filter, bool includeSystem)
        {
            var processes = new List<object>();

            // 使用 ps 命令获取进程信息
            var startInfo = new ProcessStartInfo
            {
                FileName = "/bin/sh",
                Arguments = "-c \"ps aux --no-headers\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

                using var process = SysProcess.Start(startInfo);
            if (process != null)
            {
                var output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    var parts = Regex.Split(line.Trim(), "\\s+");
                    if (parts.Length >= 11)
                    {
                        try
                        {
                            var user = parts[0];
                            var pid = int.Parse(parts[1]);
                            var cpu = double.Parse(parts[2]);
                            var mem = double.Parse(parts[3]);
                            var vsz = long.Parse(parts[4]);
                            var rss = long.Parse(parts[5]);
                            var tty = parts[6];
                            var stat = parts[7];
                            var start = parts[8];
                            var time = parts[9];
                            var command = string.Join(" ", parts.Skip(10));

                            // 过滤系统进程
                            if (!includeSystem && (user == "root" || user == "sys" || user == "daemon"))
                                continue;

                            // 应用过滤器
                            if (!string.IsNullOrWhiteSpace(filter) &&
                                !command.ToLowerInvariant().Contains(filter))
                                continue;

                            var status = stat.Contains('R') ? "running" :
                                        stat.Contains('S') ? "sleeping" :
                                        stat.Contains('T') ? "stopped" :
                                        stat.Contains('Z') ? "zombie" : "unknown";

                            processes.Add(new
                            {
                                pid = pid,
                                name = Path.GetFileName(command.Split(' ')[0]),
                                command_line = command,
                                cpu_percent = cpu,
                                memory_mb = Math.Round(rss / 1024.0, 2),
                                status = status,
                                start_time = start
                            });
                        }
                        catch { }
                    }
                }
            }

            return processes;
        }

        private async Task<List<object>> ListMacOSProcessesAsync(string filter, bool includeSystem)
        {
            var processes = new List<object>();

            // macOS 使用 ps 命令，但格式略有不同
            var startInfo = new ProcessStartInfo
            {
                FileName = "/bin/ps",
                Arguments = "-axo pid,ppid,user,pcpu,pmem,rss,vsz,stat,start_time,time,comm,command",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

                using var process = SysProcess.Start(startInfo);
            if (process != null)
            {
                var output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                // 跳过标题行
                for (int i = 1; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    var parts = Regex.Split(line, "\\s+");
                    if (parts.Length >= 11)
                    {
                        try
                        {
                            var pid = int.Parse(parts[0]);
                            var ppid = int.Parse(parts[1]);
                            var user = parts[2];
                            var cpu = double.Parse(parts[3]);
                            var mem = double.Parse(parts[4]);
                            var rss = long.Parse(parts[5]) * 1024; // KB to bytes
                            var vsz = long.Parse(parts[6]);
                            var stat = parts[7];
                            var start = parts[8];
                            var time = parts[9];
                            var comm = parts[10];
                            var command = string.Join(" ", parts.Skip(11));

                            // 过滤系统进程
                            if (!includeSystem && (user == "root" || user == "_" + comm))
                                continue;

                            // 应用过滤器
                            if (!string.IsNullOrWhiteSpace(filter) &&
                                !comm.ToLowerInvariant().Contains(filter) &&
                                !command.ToLowerInvariant().Contains(filter))
                                continue;

                            var status = stat.Contains('R') ? "running" :
                                        stat.Contains('S') ? "sleeping" :
                                        stat.Contains('T') ? "stopped" :
                                        stat.Contains('Z') ? "zombie" : "unknown";

                            processes.Add(new
                            {
                                pid = pid,
                                name = comm,
                                command_line = string.IsNullOrWhiteSpace(command) ? comm : command,
                                cpu_percent = cpu,
                                memory_mb = Math.Round(rss / (1024.0 * 1024.0), 2),
                                status = status,
                                start_time = start
                            });
                        }
                        catch { }
                    }
                }
            }

            return processes;
        }

        private List<object> ListGenericProcesses(string filter, bool includeSystem)
        {
            var processes = new List<object>();

                foreach (var proc in SysProcess.GetProcesses())
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(filter) &&
                        !proc.ProcessName.ToLowerInvariant().Contains(filter))
                        continue;

                    processes.Add(new
                    {
                        pid = proc.Id,
                        name = proc.ProcessName,
                        command_line = proc.MainModule?.FileName ?? proc.ProcessName,
                        cpu_percent = 0,
                        memory_mb = Math.Round(proc.WorkingSet64 / (1024.0 * 1024.0), 2),
                        status = proc.Responding ? "running" : "unknown",
                        start_time = DateTime.UtcNow.ToString("O")
                    });
                }
                catch { }
            }

            return processes;
        }

        #endregion

        #region Get Process Info

        /// <summary>
        /// 获取进程详细信息
        /// </summary>
        public async Task<ToolResponse> GetProcessInfoAsync(JsonElement parameters)
        {
            try
            {
                var pid = parameters.GetProperty("pid").GetInt32();
                if (pid <= 0)
                {
                    return ToolResponse.Error("INVALID_PARAMETERS", "无效的进程ID");
                }

                object processInfo;

                if (IsWindows)
                {
                    processInfo = await GetWindowsProcessInfoAsync(pid);
                }
                else if (IsLinux)
                {
                    processInfo = await GetLinuxProcessInfoAsync(pid);
                }
                else if (IsMacOS)
                {
                    processInfo = await GetMacOSProcessInfoAsync(pid);
                }
                else
                {
                    processInfo = GetGenericProcessInfo(pid);
                }

                if (processInfo == null)
                {
                    return ToolResponse.Error("PROCESS_NOT_FOUND", $"进程 {pid} 不存在");
                }

                return ToolResponse.Success(processInfo);
            }
            catch (Exception ex)
            {
                return ToolResponse.Error("EXECUTION_FAILED", ex.Message);
            }
        }

        private async Task<object> GetWindowsProcessInfoAsync(int pid)
        {
            try
            {
                var process = SysProcess.GetProcessById(pid);
                if (process.HasExited)
                    return null;

                // 使用 PowerShell 获取详细信息
                var startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-Command \"Get-Process -Id {pid} | Select-Object Id, ProcessName, Path, CommandLine, CPU, WorkingSet, PagedMemorySize, PrivateMemorySize, VirtualMemorySize, StartTime, Parent, Threads, UserProcessorTime | ConvertTo-Json -Compress\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var psProcess = SysProcess.Start(startInfo);
                if (psProcess != null)
                {
                    var output = await psProcess.StandardOutput.ReadToEndAsync();
                    await psProcess.WaitForExitAsync();

                    try
                    {
                        var json = JsonDocument.Parse(output);
                        var proc = json.RootElement;

                        var name = proc.TryGetProperty("ProcessName", out var nameProp) ? nameProp.GetString() : process.ProcessName;
                        var path = proc.TryGetProperty("Path", out var pathProp) ? pathProp.GetString() : process.MainModule?.FileName;
                        var commandLine = proc.TryGetProperty("CommandLine", out var cmdProp) ? cmdProp.GetString() : path;
                        var cpu = proc.TryGetProperty("CPU", out var cpuProp) && cpuProp.ValueKind != JsonValueKind.Null ? cpuProp.GetDouble() : 0;
                        var workingSet = proc.TryGetProperty("WorkingSet", out var wsProp) && wsProp.ValueKind != JsonValueKind.Null ? wsProp.GetInt64() : process.WorkingSet64;
                        var privateMem = proc.TryGetProperty("PrivateMemorySize", out var pmProp) && pmProp.ValueKind != JsonValueKind.Null ? pmProp.GetInt64() : 0;
                        var parentId = 0;
                        if (proc.TryGetProperty("Parent", out var parentProp) && parentProp.ValueKind != JsonValueKind.Null)
                        {
                            if (parentProp.TryGetProperty("Id", out var parentIdProp))
                                parentId = parentIdProp.GetInt32();
                        }
                        var threads = proc.TryGetProperty("Threads", out var threadsProp) && threadsProp.ValueKind == JsonValueKind.Array
                            ? threadsProp.GetArrayLength()
                            : process.Threads.Count;

                        return new
                        {
                            pid = pid,
                            name = name,
                            command_line = commandLine ?? path ?? name,
                            executable_path = path,
                            cpu_percent = Math.Round(cpu, 2),
                            memory_mb = Math.Round(workingSet / (1024.0 * 1024.0), 2),
                            memory_percent = 0,
                            status = process.Responding ? "running" : "unknown",
                            start_time = proc.TryGetProperty("StartTime", out var timeProp) && timeProp.ValueKind != JsonValueKind.Null
                                ? timeProp.GetDateTime().ToString("O")
                                : process.StartTime.ToUniversalTime().ToString("O"),
                            parent_pid = parentId,
                            threads = threads,
                            user = Environment.UserName
                        };
                    }
                    catch { }
                }

                // 回退到基本 .NET 方法
                return new
                {
                    pid = pid,
                    name = process.ProcessName,
                    command_line = process.MainModule?.FileName ?? process.ProcessName,
                    executable_path = process.MainModule?.FileName,
                    cpu_percent = 0,
                    memory_mb = Math.Round(process.WorkingSet64 / (1024.0 * 1024.0), 2),
                    memory_percent = 0,
                    status = process.Responding ? "running" : "unknown",
                    start_time = process.StartTime.ToUniversalTime().ToString("O"),
                    parent_pid = 0,
                    threads = process.Threads.Count,
                    user = Environment.UserName
                };
            }
            catch (ArgumentException)
            {
                return null;
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        private async Task<object> GetLinuxProcessInfoAsync(int pid)
        {
            try
            {
                // 读取 /proc/[pid]/stat
                var statPath = $"/proc/{pid}/stat";
                var statusPath = $"/proc/{pid}/status";
                var cmdlinePath = $"/proc/{pid}/cmdline";
                var exePath = $"/proc/{pid}/exe";

                if (!File.Exists(statPath))
                    return null;

                string name = "Unknown";
                string commandLine = "";
                string executablePath = "";
                int parentPid = 0;
                int threads = 0;
                string user = "Unknown";
                string status = "unknown";

                // 读取 stat 文件
                var statContent = await File.ReadAllTextAsync(statPath);
                var statMatch = Regex.Match(statContent, @"\(([^)]+)\)\s+(\S+)\s+(\S+)\s+(\S+)\s+(\S+)\s+(\S+)\s+(\S+)\s+(\S+)\s+(\S+)\s+(\S+)\s+(\S+)\s+(\S+)\s+(\S+)\s+(\S+)\s+(\S+)\s+(\S+)\s+(\S+)\s+(\S+)\s+(\S+)\s+(\S+)\s+(\S+)\s+(\S+)");
                if (statMatch.Success)
                {
                    name = statMatch.Groups[1].Value;
                    status = statMatch.Groups[3].Value;
                    parentPid = int.Parse(statMatch.Groups[4].Value);
                    threads = int.Parse(statMatch.Groups[20].Value);
                }

                // 读取 cmdline
                if (File.Exists(cmdlinePath))
                {
                    var cmdlineBytes = await File.ReadAllBytesAsync(cmdlinePath);
                    commandLine = Encoding.UTF8.GetString(cmdlineBytes).Replace('\0', ' ').Trim();
                }

                // 读取 exe 链接
                try
                {
                    if (File.Exists(exePath))
                    {
                        executablePath = System.IO.Path.GetFullPath(exePath);
                    }
                }
                catch { }

                // 读取 status 文件获取用户信息
                if (File.Exists(statusPath))
                {
                    var statusContent = await File.ReadAllTextAsync(statusPath);
                    var uidMatch = Regex.Match(statusContent, @"Uid:\s+(\d+)");
                    if (uidMatch.Success)
                    {
                        var uid = uidMatch.Groups[1].Value;
                        user = GetLinuxUserName(int.Parse(uid));
                    }
                }

                // 使用 ps 获取 CPU 和内存信息
                var startInfo = new ProcessStartInfo
                {
                    FileName = "/bin/sh",
                    Arguments = $"-c \"ps -p {pid} -o pid,pcpu,pmem,rss,vsz --no-headers\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                double cpuPercent = 0;
                double memPercent = 0;
                long rss = 0;

                using var process = SysProcess.Start(startInfo);
                if (process != null)
                {
                    var output = await process.StandardOutput.ReadToEndAsync();
                    await process.WaitForExitAsync();

                    var parts = Regex.Split(output.Trim(), "\\s+");
                    if (parts.Length >= 5)
                    {
                        cpuPercent = double.Parse(parts[1]);
                        memPercent = double.Parse(parts[2]);
                        rss = long.Parse(parts[3]) * 1024; // KB to bytes
                    }
                }

                var statusMap = new Dictionary<string, string>
                {
                    { "R", "running" },
                    { "S", "sleeping" },
                    { "D", "sleeping" },
                    { "T", "stopped" },
                    { "Z", "zombie" },
                    { "X", "unknown" }
                };

                return new
                {
                    pid = pid,
                    name = name,
                    command_line = string.IsNullOrWhiteSpace(commandLine) ? name : commandLine,
                    executable_path = executablePath,
                    cpu_percent = cpuPercent,
                    memory_mb = Math.Round(rss / (1024.0 * 1024.0), 2),
                    memory_percent = memPercent,
                    status = statusMap.GetValueOrDefault(status, "unknown"),
                    start_time = DateTime.UtcNow.ToString("O"),
                    parent_pid = parentPid,
                    threads = threads,
                    user = user
                };
            }
            catch
            {
                return null;
            }
        }

        private async Task<object> GetMacOSProcessInfoAsync(int pid)
        {
            try
            {
                // macOS 使用 ps 命令获取进程信息
                var startInfo = new ProcessStartInfo
                {
                    FileName = "/bin/ps",
                    Arguments = $"-p {pid} -o pid,ppid,user,pcpu,pmem,rss,vsz,stat,start_time,time,comm,command",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = SysProcess.Start(startInfo);
                if (process == null)
                    return null;

                var output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length < 2)
                    return null;

                var parts = Regex.Split(lines[1].Trim(), "\\s+");
                if (parts.Length < 11)
                    return null;

                var actualPid = int.Parse(parts[0]);
                var parentPid = int.Parse(parts[1]);
                var user = parts[2];
                var cpuPercent = double.Parse(parts[3]);
                var memPercent = double.Parse(parts[4]);
                var rss = long.Parse(parts[5]) * 1024; // KB to bytes
                var vsz = long.Parse(parts[6]);
                var stat = parts[7];
                var start = parts[8];
                var time = parts[9];
                var comm = parts[10];
                var command = string.Join(" ", parts.Skip(11));

                var statusMap = new Dictionary<string, string>
                {
                    { "R", "running" },
                    { "S", "sleeping" },
                    { "T", "stopped" },
                    { "Z", "zombie" },
                    { "I", "sleeping" }, // Idle (macOS specific)
                    { "U", "unknown" }   // Uninterruptible wait (macOS specific)
                };

                return new
                {
                    pid = actualPid,
                    name = comm,
                    command_line = string.IsNullOrWhiteSpace(command) ? comm : command,
                    executable_path = $"/proc/{pid}/exe", // macOS 不支持直接读取
                    cpu_percent = cpuPercent,
                    memory_mb = Math.Round(rss / (1024.0 * 1024.0), 2),
                    memory_percent = memPercent,
                    status = statusMap.GetValueOrDefault(stat, "unknown"),
                    start_time = start,
                    parent_pid = parentPid,
                    threads = 0, // macOS ps 不直接提供线程数
                    user = user
                };
            }
            catch
            {
                return null;
            }
        }

        private object GetGenericProcessInfo(int pid)
        {
            try
            {
                var process = SysProcess.GetProcessById(pid);
                if (process.HasExited)
                    return null;

                return new
                {
                    pid = pid,
                    name = process.ProcessName,
                    command_line = process.MainModule?.FileName ?? process.ProcessName,
                    executable_path = process.MainModule?.FileName,
                    cpu_percent = 0,
                    memory_mb = Math.Round(process.WorkingSet64 / (1024.0 * 1024.0), 2),
                    memory_percent = 0,
                    status = process.Responding ? "running" : "unknown",
                    start_time = DateTime.UtcNow.ToString("O"),
                    parent_pid = 0,
                    threads = process.Threads.Count,
                    user = Environment.UserName
                };
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// 转义shell命令中的特殊字符
        /// </summary>
        private string EscapeShellCommand(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
                return "";

            if (IsWindows)
            {
                // Windows: 转义双引号
                return command.Replace("\"", "\\\"");
            }
            else
            {
                // Unix-like (Linux/macOS): 转义双引号和美元符号
                return command.Replace("\"", "\\\"").Replace("$", "\\$");
            }
        }

        private string EscapeArgument(string arg)
        {
            if (string.IsNullOrWhiteSpace(arg))
                return "";

            if (IsWindows)
            {
                // Windows 参数转义
                if (arg.Contains(" ") || arg.Contains("\"") || arg.Contains("\t"))
                {
                    return "\"" + arg.Replace("\"", "\\\"") + "\"";
                }
                return arg;
            }
            else
            {
                // Unix-like (Linux/macOS) 参数转义
                return "'" + arg.Replace("'", "'\\''") + "'";
            }
        }

        private bool IsSystemProcess(string name, string path)
        {
            var systemProcesses = new[]
            {
                "svchost", "csrss", "smss", "services", "lsass", "winlogon",
                "explorer", "System", "Registry", "Memory Compression",
                "init", "systemd", "kthreadd", "ksoftirqd", "kworker",
                "rcu_gp", "rcu_par_gp", "slub_", "netns", "kauditd",
                // macOS 系统进程
                "kernel_task", "launchd", "WindowServer", "Dock", "Finder",
                "SystemUIServer", "loginwindow", "coreaudiod", "bluetoothd",
                "configd", "notifyd", "securityd", "mds", "mds_stores"
            };

            if (systemProcesses.Any(sp => name?.ToLowerInvariant().Contains(sp.ToLowerInvariant()) ?? false))
                return true;

            if (IsWindows && (path?.StartsWith("C:\\Windows\\System32", StringComparison.OrdinalIgnoreCase) ?? false))
                return true;

            if (IsMacOS && (path?.StartsWith("/System/", StringComparison.OrdinalIgnoreCase) ?? false))
                return true;

            return false;
        }

        private string GetLinuxUserName(int uid)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "/bin/sh",
                    Arguments = $"-c \"getent passwd {uid} | cut -d: -f1\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = SysProcess.Start(startInfo);
                if (process != null)
                {
                    var output = process.StandardOutput.ReadToEnd().Trim();
                    process.WaitForExit();
                    return string.IsNullOrWhiteSpace(output) ? uid.ToString() : output;
                }
            }
            catch { }

            return uid.ToString();
        }

        #endregion
    }

    /// <summary>
    /// 工具响应类
    /// </summary>
    public class ToolResponse
    {
        public string Status { get; set; }
        public object Data { get; set; }
        public ToolError ErrorInfo { get; set; }
        public long ExecutionTime { get; set; }

        public static ToolResponse Success(object data)
        {
            return new ToolResponse
            {
                Status = "success",
                Data = data
            };
        }

        public static ToolResponse Error(string code, string message, object data = null)
        {
            return new ToolResponse
            {
                Status = "error",
                ErrorInfo = new ToolError { Code = code, Message = message, Data = data }
            };
        }
    }

    public class ToolError
    {
        public string Code { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
    }

    public static class DictionaryExtensions
    {
        public static TValue GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue defaultValue)
        {
            return dict.TryGetValue(key, out var value) ? value : defaultValue;
        }
    }
}
