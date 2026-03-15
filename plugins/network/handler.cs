using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace FlowWorker.Plugins.Network
{
    /// <summary>
    /// 网络工具处理器，支持Windows和Linux系统
    /// </summary>
    public class NetworkHandler
    {
        private static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        private static readonly bool IsLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        private static readonly HttpClient HttpClient = new HttpClient();

        #region HTTP Request

        /// <summary>
        /// 发送HTTP请求（支持HTTP和HTTPS）
        /// </summary>
        public async Task<ToolResponse> HttpRequestAsync(JsonElement parameters)
        {
            try
            {
                var url = parameters.GetProperty("url").GetString();
                if (string.IsNullOrWhiteSpace(url))
                {
                    return ToolResponse.Error("INVALID_URL", "URL不能为空");
                }

                // 验证URL格式并检查协议
                if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                {
                    return ToolResponse.Error("INVALID_URL", "无效的URL格式");
                }

                // 验证协议类型（支持http和https）
                if (uri.Scheme != "http" && uri.Scheme != "https")
                {
                    return ToolResponse.Error("INVALID_URL", "仅支持HTTP和HTTPS协议");
                }

                var method = parameters.TryGetProperty("method", out var methodProp)
                    ? methodProp.GetString()?.ToUpperInvariant() ?? "GET"
                    : "GET";

                var timeout = parameters.TryGetProperty("timeout", out var timeoutProp)
                    ? timeoutProp.GetInt32()
                    : 30000;

                var followRedirects = parameters.TryGetProperty("follow_redirects", out var redirectProp)
                    ? redirectProp.GetBoolean()
                    : true;

                // 是否验证SSL证书（HTTPS可选）
                var verifySsl = parameters.TryGetProperty("verify_ssl", out var sslProp)
                    ? sslProp.GetBoolean()
                    : true;

                using var cts = new CancellationTokenSource(timeout);
                using var request = new HttpRequestMessage(new HttpMethod(method), url);

                // 设置请求头
                if (parameters.TryGetProperty("headers", out var headersProp) && headersProp.ValueKind == JsonValueKind.Object)
                {
                    foreach (var header in headersProp.EnumerateObject())
                    {
                        var headerValue = header.Value.GetString();
                        if (!string.IsNullOrWhiteSpace(headerValue))
                        {
                            request.Headers.TryAddWithoutValidation(header.Name, headerValue);
                        }
                    }
                }

                // 设置请求体
                if (parameters.TryGetProperty("body", out var bodyProp))
                {
                    var body = bodyProp.GetString();
                    if (!string.IsNullOrWhiteSpace(body))
                    {
                        request.Content = new StringContent(body, Encoding.UTF8, "application/json");
                    }
                }

                // 配置HttpClientHandler以支持HTTP和HTTPS
                var handler = new HttpClientHandler
                {
                    AllowAutoRedirect = followRedirects,
                    // 对于HTTPS，控制SSL证书验证
                    ServerCertificateCustomValidationCallback = verifySsl ? null : (sender, cert, chain, sslPolicyErrors) => true
                };

                using var client = new HttpClient(handler);
                client.Timeout = TimeSpan.FromMilliseconds(timeout);

                var stopwatch = Stopwatch.StartNew();
                using var response = await client.SendAsync(request, cts.Token);
                stopwatch.Stop();

                var responseBody = await response.Content.ReadAsStringAsync();

                // 提取响应头
                var responseHeaders = new Dictionary<string, string>();
                foreach (var header in response.Headers)
                {
                    responseHeaders[header.Key] = string.Join(", ", header.Value);
                }
                foreach (var header in response.Content.Headers)
                {
                    responseHeaders[header.Key] = string.Join(", ", header.Value);
                }

                return ToolResponse.Success(new
                {
                    status_code = (int)response.StatusCode,
                    status_text = response.StatusCode.ToString(),
                    headers = responseHeaders,
                    body = responseBody,
                    response_time = (int)stopwatch.ElapsedMilliseconds,
                    protocol = uri.Scheme
                });
            }
            catch (TaskCanceledException)
            {
                return ToolResponse.Error("TIMEOUT", "请求超时");
            }
            catch (HttpRequestException ex)
            {
                if (ex.Message.Contains("SSL") || ex.Message.Contains("certificate") || ex.Message.Contains("TLS"))
                {
                    return ToolResponse.Error("SSL_ERROR", $"SSL/TLS错误: {ex.Message}");
                }
                return ToolResponse.Error("CONNECTION_FAILED", $"连接失败: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ToolResponse.Error("EXECUTION_FAILED", ex.Message);
            }
        }

        #endregion

        #region Download File

        /// <summary>
        /// 下载文件
        /// </summary>
        public async Task<ToolResponse> DownloadFileAsync(JsonElement parameters)
        {
            try
            {
                var url = parameters.GetProperty("url").GetString();
                if (string.IsNullOrWhiteSpace(url))
                {
                    return ToolResponse.Error("INVALID_URL", "URL不能为空");
                }

                var savePath = parameters.GetProperty("save_path").GetString();
                if (string.IsNullOrWhiteSpace(savePath))
                {
                    return ToolResponse.Error("INVALID_PARAMETERS", "保存路径不能为空");
                }

                // 验证URL格式
                if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                {
                    return ToolResponse.Error("INVALID_URL", "无效的URL格式");
                }

                // 检查文件是否已存在
                var overwrite = parameters.TryGetProperty("overwrite", out var overwriteProp) && overwriteProp.GetBoolean();
                if (File.Exists(savePath) && !overwrite)
                {
                    return ToolResponse.Error("FILE_EXISTS", $"文件已存在: {savePath}");
                }

                // 确保目录存在
                var directory = Path.GetDirectoryName(savePath);
                if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var timeout = parameters.TryGetProperty("timeout", out var timeoutProp)
                    ? timeoutProp.GetInt32()
                    : 60000;

                using var cts = new CancellationTokenSource(timeout);
                using var request = new HttpRequestMessage(HttpMethod.Get, url);

                // 设置请求头
                if (parameters.TryGetProperty("headers", out var headersProp) && headersProp.ValueKind == JsonValueKind.Object)
                {
                    foreach (var header in headersProp.EnumerateObject())
                    {
                        var headerValue = header.Value.GetString();
                        if (!string.IsNullOrWhiteSpace(headerValue))
                        {
                            request.Headers.TryAddWithoutValidation(header.Name, headerValue);
                        }
                    }
                }

                var stopwatch = Stopwatch.StartNew();
                using var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);
                response.EnsureSuccessStatusCode();

                using var contentStream = await response.Content.ReadAsStreamAsync();
                using var fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None);
                await contentStream.CopyToAsync(fileStream, cts.Token);
                stopwatch.Stop();

                var fileInfo = new FileInfo(savePath);

                return ToolResponse.Success(new
                {
                    success = true,
                    file_path = savePath,
                    file_size = fileInfo.Length,
                    download_time = (int)stopwatch.ElapsedMilliseconds
                });
            }
            catch (TaskCanceledException)
            {
                return ToolResponse.Error("TIMEOUT", "下载超时");
            }
            catch (HttpRequestException ex)
            {
                return ToolResponse.Error("CONNECTION_FAILED", $"连接失败: {ex.Message}");
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

        #endregion

        #region Ping Host

        /// <summary>
        /// Ping主机
        /// </summary>
        public async Task<ToolResponse> PingHostAsync(JsonElement parameters)
        {
            try
            {
                var host = parameters.GetProperty("host").GetString();
                if (string.IsNullOrWhiteSpace(host))
                {
                    return ToolResponse.Error("INVALID_HOST", "主机地址不能为空");
                }

                var count = parameters.TryGetProperty("count", out var countProp)
                    ? countProp.GetInt32()
                    : 4;

                var timeout = parameters.TryGetProperty("timeout", out var timeoutProp)
                    ? timeoutProp.GetInt32()
                    : 5;

                var packetSize = parameters.TryGetProperty("packet_size", out var sizeProp)
                    ? sizeProp.GetInt32()
                    : 32;

                // 限制参数范围
                count = Math.Clamp(count, 1, 100);
                timeout = Math.Clamp(timeout, 1, 60);
                packetSize = Math.Clamp(packetSize, 1, 65500);

                // 使用系统ping命令以获得更详细的信息
                if (IsWindows)
                {
                    return await PingWindowsAsync(host, count, timeout, packetSize);
                }
                else if (IsLinux)
                {
                    return await PingLinuxAsync(host, count, timeout, packetSize);
                }
                else
                {
                    return await PingGenericAsync(host, count, timeout, packetSize);
                }
            }
            catch (Exception ex)
            {
                return ToolResponse.Error("EXECUTION_FAILED", ex.Message);
            }
        }

        private async Task<ToolResponse> PingWindowsAsync(string host, int count, int timeout, int packetSize)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "ping.exe",
                Arguments = $"-n {count} -w {timeout * 1000} -l {packetSize} {host}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.GetEncoding(65001) // UTF-8
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                return ToolResponse.Error("EXECUTION_FAILED", "无法启动ping进程");
            }

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            return ParsePingOutput(output, host, count);
        }

        private async Task<ToolResponse> PingLinuxAsync(string host, int count, int timeout, int packetSize)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "/bin/ping",
                Arguments = $"-c {count} -W {timeout} -s {packetSize} {host}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                return ToolResponse.Error("EXECUTION_FAILED", "无法启动ping进程");
            }

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            return ParsePingOutput(output, host, count);
        }

        private async Task<ToolResponse> PingGenericAsync(string host, int count, int timeout, int packetSize)
        {
            try
            {
                using var ping = new Ping();
                var details = new List<object>();
                var successCount = 0;
                var times = new List<double>();
                string ipAddress = null;

                for (int i = 0; i < count; i++)
                {
                    try
                    {
                        var reply = await ping.SendPingAsync(host, timeout * 1000, new byte[packetSize]);
                        
                        if (ipAddress == null && reply.Address != null)
                        {
                            ipAddress = reply.Address.ToString();
                        }

                        if (reply.Status == IPStatus.Success)
                        {
                            successCount++;
                            var time = reply.RoundtripTime;
                            times.Add(time);
                            
                            details.Add(new
                            {
                                sequence = i + 1,
                                time = time,
                                ttl = reply.Options?.Ttl ?? 0,
                                status = "success"
                            });
                        }
                        else
                        {
                            details.Add(new
                            {
                                sequence = i + 1,
                                time = (double?)null,
                                ttl = (int?)null,
                                status = reply.Status == IPStatus.TimedOut ? "timeout" : "error"
                            });
                        }
                    }
                    catch
                    {
                        details.Add(new
                        {
                            sequence = i + 1,
                            time = (double?)null,
                            ttl = (int?)null,
                            status = "error"
                        });
                    }
                }

                // 解析域名获取IP
                if (ipAddress == null)
                {
                    try
                    {
                        var addresses = await Dns.GetHostAddressesAsync(host);
                        if (addresses.Length > 0)
                        {
                            ipAddress = addresses[0].ToString();
                        }
                    }
                    catch { }
                }

                var packetLoss = count > 0 ? ((count - successCount) * 100.0 / count) : 100;

                return ToolResponse.Success(new
                {
                    success = successCount > 0,
                    host = host,
                    ip_address = ipAddress,
                    packets_sent = count,
                    packets_received = successCount,
                    packet_loss_percent = Math.Round(packetLoss, 2),
                    min_time = times.Count > 0 ? times.Min() : 0,
                    max_time = times.Count > 0 ? times.Max() : 0,
                    avg_time = times.Count > 0 ? times.Average() : 0,
                    details = details
                });
            }
            catch (Exception ex)
            {
                return ToolResponse.Error("HOST_UNREACHABLE", $"无法访问主机: {ex.Message}");
            }
        }

        private ToolResponse ParsePingOutput(string output, string host, int count)
        {
            try
            {
                var details = new List<object>();
                string ipAddress = null;
                var successCount = 0;
                var times = new List<double>();

                // 解析IP地址
                var ipMatch = Regex.Match(output, @"\[(\d+\.\d+\.\d+\.\d+)\]|from\s+(\d+\.\d+\.\d+\.\d+)|Reply from (\d+\.\d+\.\d+\.\d+)");
                if (ipMatch.Success)
                {
                    ipAddress = ipMatch.Groups.Cast<Group>().Skip(1).FirstOrDefault(g => !string.IsNullOrEmpty(g.Value))?.Value;
                }

                // 解析每次ping的结果
                var replyMatches = Regex.Matches(output, @"(?:Reply from|来自).*?[:=]\s*(?:bytes=\d+\s+)?time[=:]([\d.]+)ms|time[=:]([\d.]+)ms", RegexOptions.IgnoreCase);
                var timeoutMatches = Regex.Matches(output, @"Request timed out|Destination host unreachable|无法访问目标主机|超时", RegexOptions.IgnoreCase);

                int sequence = 0;
                foreach (Match match in replyMatches)
                {
                    sequence++;
                    var timeStr = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
                    if (double.TryParse(timeStr, out var time))
                    {
                        successCount++;
                        times.Add(time);
                        details.Add(new
                        {
                            sequence = sequence,
                            time = time,
                            ttl = 0,
                            status = "success"
                        });
                    }
                }

                // 补充超时项
                while (details.Count < count)
                {
                    details.Add(new
                    {
                        sequence = details.Count + 1,
                        time = (double?)null,
                        ttl = (int?)null,
                        status = "timeout"
                    });
                }

                // 解析统计信息
                var statsMatch = Regex.Match(output, @"(?:Minimum|最小).*?([\d.]+).*?(?:Maximum|最大).*?([\d.]+).*?(?:Average|平均).*?([\d.]+)", RegexOptions.IgnoreCase);
                double minTime = 0, maxTime = 0, avgTime = 0;
                
                if (statsMatch.Success)
                {
                    minTime = double.Parse(statsMatch.Groups[1].Value);
                    maxTime = double.Parse(statsMatch.Groups[2].Value);
                    avgTime = double.Parse(statsMatch.Groups[3].Value);
                }
                else if (times.Count > 0)
                {
                    minTime = times.Min();
                    maxTime = times.Max();
                    avgTime = times.Average();
                }

                // 解析丢包率
                var lossMatch = Regex.Match(output, @"(\d+)% loss|丢失\s*=\s*\d+.*?(\d+)%", RegexOptions.IgnoreCase);
                double packetLoss = 0;
                if (lossMatch.Success)
                {
                    var lossGroup = lossMatch.Groups[1].Success ? lossMatch.Groups[1] : lossMatch.Groups[2];
                    packetLoss = double.Parse(lossGroup.Value);
                }
                else
                {
                    packetLoss = count > 0 ? ((count - successCount) * 100.0 / count) : 100;
                }

                return ToolResponse.Success(new
                {
                    success = successCount > 0,
                    host = host,
                    ip_address = ipAddress,
                    packets_sent = count,
                    packets_received = successCount,
                    packet_loss_percent = Math.Round(packetLoss, 2),
                    min_time = Math.Round(minTime, 2),
                    max_time = Math.Round(maxTime, 2),
                    avg_time = Math.Round(avgTime, 2),
                    details = details
                });
            }
            catch (Exception ex)
            {
                return ToolResponse.Error("EXECUTION_FAILED", $"解析ping结果失败: {ex.Message}");
            }
        }

        #endregion

        #region Resolve DNS

        /// <summary>
        /// DNS解析
        /// </summary>
        public async Task<ToolResponse> ResolveDnsAsync(JsonElement parameters)
        {
            try
            {
                var domain = parameters.GetProperty("domain").GetString();
                if (string.IsNullOrWhiteSpace(domain))
                {
                    return ToolResponse.Error("INVALID_DOMAIN", "域名不能为空");
                }

                var recordType = parameters.TryGetProperty("record_type", out var typeProp)
                    ? typeProp.GetString()?.ToUpperInvariant() ?? "A"
                    : "A";

                var dnsServer = parameters.TryGetProperty("dns_server", out var dnsProp)
                    ? dnsProp.GetString()
                    : null;

                // 使用系统命令进行DNS解析
                if (IsWindows)
                {
                    return await ResolveDnsWindowsAsync(domain, recordType, dnsServer);
                }
                else if (IsLinux)
                {
                    return await ResolveDnsLinuxAsync(domain, recordType, dnsServer);
                }
                else
                {
                    return await ResolveDnsGenericAsync(domain, recordType);
                }
            }
            catch (Exception ex)
            {
                return ToolResponse.Error("RESOLUTION_FAILED", ex.Message);
            }
        }

        private async Task<ToolResponse> ResolveDnsWindowsAsync(string domain, string recordType, string dnsServer)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "nslookup.exe",
                Arguments = $"-type={recordType} {domain} {dnsServer ?? ""}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                return ToolResponse.Error("EXECUTION_FAILED", "无法启动nslookup进程");
            }

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            return ParseNslookupOutput(output, domain, recordType);
        }

        private async Task<ToolResponse> ResolveDnsLinuxAsync(string domain, string recordType, string dnsServer)
        {
            // 优先使用dig命令，如果不存在则使用nslookup
            var digExists = File.Exists("/usr/bin/dig") || File.Exists("/bin/dig");
            
            ProcessStartInfo startInfo;
            if (digExists)
            {
                var dnsArg = !string.IsNullOrWhiteSpace(dnsServer) ? $"@{dnsServer}" : "";
                startInfo = new ProcessStartInfo
                {
                    FileName = "dig",
                    Arguments = $"{dnsArg} {domain} {recordType} +short",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
            }
            else
            {
                startInfo = new ProcessStartInfo
                {
                    FileName = "nslookup",
                    Arguments = $"-type={recordType} {domain} {dnsServer ?? ""}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
            }

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                return ToolResponse.Error("EXECUTION_FAILED", "无法启动DNS解析进程");
            }

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (digExists)
            {
                return ParseDigOutput(output, domain, recordType);
            }
            else
            {
                return ParseNslookupOutput(output, domain, recordType);
            }
        }

        private async Task<ToolResponse> ResolveDnsGenericAsync(string domain, string recordType)
        {
            try
            {
                var records = new List<object>();

                switch (recordType)
                {
                    case "A":
                        var addresses = await Dns.GetHostAddressesAsync(domain);
                        foreach (var addr in addresses.Where(a => a.AddressFamily == AddressFamily.InterNetwork))
                        {
                            records.Add(new
                            {
                                type = "A",
                                value = addr.ToString(),
                                ttl = 0
                            });
                        }
                        break;

                    case "AAAA":
                        var addresses6 = await Dns.GetHostAddressesAsync(domain);
                        foreach (var addr in addresses6.Where(a => a.AddressFamily == AddressFamily.InterNetworkV6))
                        {
                            records.Add(new
                            {
                                type = "AAAA",
                                value = addr.ToString(),
                                ttl = 0
                            });
                        }
                        break;

                    default:
                        return ToolResponse.Error("RESOLUTION_FAILED", $"不支持的记录类型: {recordType}");
                }

                return ToolResponse.Success(new
                {
                    domain = domain,
                    record_type = recordType,
                    records = records,
                    ttl = 0
                });
            }
            catch (Exception ex)
            {
                return ToolResponse.Error("RESOLUTION_FAILED", ex.Message);
            }
        }

        private ToolResponse ParseNslookupOutput(string output, string domain, string recordType)
        {
            var records = new List<object>();
            var lines = output.Split('\n');
            var inAnswerSection = false;

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                
                if (trimmedLine.Contains("Name:") || trimmedLine.Contains("名称:"))
                {
                    inAnswerSection = true;
                    continue;
                }

                if (inAnswerSection && (trimmedLine.Contains("Address:") || trimmedLine.Contains("Addresses:") || trimmedLine.Contains("地址:")))
                {
                    var parts = trimmedLine.Split(new[] { ':', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2)
                    {
                        var value = parts.Last().Trim();
                        if (!string.IsNullOrWhiteSpace(value) && !records.Any(r => ((dynamic)r).value == value))
                        {
                            records.Add(new
                            {
                                type = recordType,
                                value = value,
                                ttl = 0
                            });
                        }
                    }
                }
            }

            if (records.Count == 0)
            {
                // 尝试其他格式解析
                var matches = Regex.Matches(output, @"Address(?:es)?:\s*([\d.:a-fA-F]+)");
                foreach (Match match in matches)
                {
                    var value = match.Groups[1].Value.Trim();
                    if (!string.IsNullOrWhiteSpace(value) && !records.Any(r => ((dynamic)r).value == value))
                    {
                        records.Add(new
                        {
                            type = recordType,
                            value = value,
                            ttl = 0
                        });
                    }
                }
            }

            if (records.Count == 0)
            {
                return ToolResponse.Error("RESOLUTION_FAILED", "无法解析域名或没有找到记录");
            }

            return ToolResponse.Success(new
            {
                domain = domain,
                record_type = recordType,
                records = records,
                ttl = 0
            });
        }

        private ToolResponse ParseDigOutput(string output, string domain, string recordType)
        {
            var records = new List<object>();
            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith(";"))
                    continue;

                // 简单解析dig输出
                if (!string.IsNullOrWhiteSpace(trimmedLine))
                {
                    records.Add(new
                    {
                        type = recordType,
                        value = trimmedLine,
                        ttl = 0
                    });
                }
            }

            if (records.Count == 0)
            {
                return ToolResponse.Error("RESOLUTION_FAILED", "无法解析域名或没有找到记录");
            }

            return ToolResponse.Success(new
            {
                domain = domain,
                record_type = recordType,
                records = records,
                ttl = 0
            });
        }

        #endregion

        #region Check Port

        /// <summary>
        /// 检查端口状态
        /// </summary>
        public async Task<ToolResponse> CheckPortAsync(JsonElement parameters)
        {
            try
            {
                var host = parameters.GetProperty("host").GetString();
                if (string.IsNullOrWhiteSpace(host))
                {
                    return ToolResponse.Error("INVALID_HOST", "主机地址不能为空");
                }

                var port = parameters.GetProperty("port").GetInt32();
                if (port < 1 || port > 65535)
                {
                    return ToolResponse.Error("INVALID_PORT", "端口号必须在1-65535之间");
                }

                var timeout = parameters.TryGetProperty("timeout", out var timeoutProp)
                    ? timeoutProp.GetInt32()
                    : 5000;

                var protocol = parameters.TryGetProperty("protocol", out var protocolProp)
                    ? protocolProp.GetString()?.ToLowerInvariant() ?? "tcp"
                    : "tcp";

                timeout = Math.Clamp(timeout, 100, 60000);

                var stopwatch = Stopwatch.StartNew();
                bool isOpen = false;

                if (protocol == "tcp")
                {
                    isOpen = await CheckTcpPortAsync(host, port, timeout);
                }
                else if (protocol == "udp")
                {
                    isOpen = await CheckUdpPortAsync(host, port, timeout);
                }
                else
                {
                    return ToolResponse.Error("INVALID_PARAMETERS", "不支持的协议类型");
                }

                stopwatch.Stop();

                var serviceName = GetServiceName(port);

                return ToolResponse.Success(new
                {
                    host = host,
                    port = port,
                    protocol = protocol,
                    is_open = isOpen,
                    response_time = (int)stopwatch.ElapsedMilliseconds,
                    service_name = serviceName
                });
            }
            catch (Exception ex)
            {
                return ToolResponse.Error("CONNECTION_FAILED", ex.Message);
            }
        }

        private async Task<bool> CheckTcpPortAsync(string host, int port, int timeout)
        {
            try
            {
                using var client = new TcpClient();
                using var cts = new CancellationTokenSource(timeout);
                
                await client.ConnectAsync(host, port).WaitAsync(cts.Token);
                return client.Connected;
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> CheckUdpPortAsync(string host, int port, int timeout)
        {
            try
            {
                using var udpClient = new UdpClient();
                udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, timeout);
                
                var endPoint = new IPEndPoint(await ResolveHostAsync(host), port);
                var sendBytes = Encoding.ASCII.GetBytes("CHECK");
                
                await udpClient.SendAsync(sendBytes, sendBytes.Length, endPoint);
                
                var receiveTask = udpClient.ReceiveAsync();
                if (await Task.WhenAny(receiveTask, Task.Delay(timeout)) == receiveTask)
                {
                    return true;
                }
                
                return false;
            }
            catch
            {
                // UDP端口检查不可靠，无响应可能是开放也可能是过滤
                return false;
            }
        }

        private async Task<IPAddress> ResolveHostAsync(string host)
        {
            if (IPAddress.TryParse(host, out var ipAddress))
            {
                return ipAddress;
            }

            var addresses = await Dns.GetHostAddressesAsync(host);
            return addresses.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork) 
                ?? addresses.FirstOrDefault();
        }

        private string GetServiceName(int port)
        {
            var commonPorts = new Dictionary<int, string>
            {
                { 20, "FTP Data" },
                { 21, "FTP Control" },
                { 22, "SSH" },
                { 23, "Telnet" },
                { 25, "SMTP" },
                { 53, "DNS" },
                { 80, "HTTP" },
                { 110, "POP3" },
                { 143, "IMAP" },
                { 443, "HTTPS" },
                { 3306, "MySQL" },
                { 3389, "RDP" },
                { 5432, "PostgreSQL" },
                { 6379, "Redis" },
                { 8080, "HTTP Proxy" },
                { 27017, "MongoDB" }
            };

            return commonPorts.TryGetValue(port, out var service) ? service : "Unknown";
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
        public ToolError Error { get; set; }
        public long ExecutionTime { get; set; }

        public static ToolResponse Success(object data)
        {
            return new ToolResponse
            {
                Status = "success",
                Data = data
            };
        }

        public static ToolResponse Error(string code, string message)
        {
            return new ToolResponse
            {
                Status = "error",
                Error = new ToolError { Code = code, Message = message }
            };
        }
    }

    public class ToolError
    {
        public string Code { get; set; }
        public string Message { get; set; }
    }
}
