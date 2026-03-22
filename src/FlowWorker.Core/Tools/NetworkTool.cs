using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using FlowWorker.Core.Interfaces;

namespace FlowWorker.Core.Tools;

/// <summary>
/// 网络工具处理器
/// 提供 HTTP 请求、文件下载、Ping、DNS 解析、端口检查等功能
/// </summary>
public class NetworkTool : IToolHandler
{
    private static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    private static readonly bool IsLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    private static readonly HttpClient HttpClient = new HttpClient();

    public string Name => "Network";

    public async Task<ToolResponse> ExecuteAsync(string action, JsonElement parameters)
    {
        return action.ToLowerInvariant() switch
        {
            "http_request" => await HttpRequestAsync(parameters),
            "download_file" => await DownloadFileAsync(parameters),
            "ping_host" => await PingHostAsync(parameters),
            "resolve_dns" => await ResolveDnsAsync(parameters),
            "check_port" => await CheckPortAsync(parameters),
            _ => ToolResponse.Error("UNKNOWN_ACTION", $"未知的操作：{action}")
        };
    }

    /// <summary>
    /// 发送 HTTP 请求
    /// </summary>
    private async Task<ToolResponse> HttpRequestAsync(JsonElement parameters)
    {
        try
        {
            var url = parameters.GetProperty("url").GetString();
            if (string.IsNullOrWhiteSpace(url))
            {
                return ToolResponse.Error("INVALID_URL", "URL 不能为空");
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                return ToolResponse.Error("INVALID_URL", "无效的 URL 格式");
            }

            if (uri.Scheme != "http" && uri.Scheme != "https")
            {
                return ToolResponse.Error("INVALID_URL", "仅支持 HTTP 和 HTTPS 协议");
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

            var verifySsl = parameters.TryGetProperty("verify_ssl", out var sslProp)
                ? sslProp.GetBoolean()
                : true;

            using var cts = new CancellationTokenSource(timeout);
            using var request = new HttpRequestMessage(new HttpMethod(method), url);

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

            if (parameters.TryGetProperty("body", out var bodyProp))
            {
                var body = bodyProp.GetString();
                if (!string.IsNullOrWhiteSpace(body))
                {
                    request.Content = new StringContent(body, Encoding.UTF8, "application/json");
                }
            }

            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = followRedirects,
                ServerCertificateCustomValidationCallback = verifySsl ? null : (sender, cert, chain, sslPolicyErrors) => true
            };

            using var client = new HttpClient(handler);
            client.Timeout = TimeSpan.FromMilliseconds(timeout);

            var stopwatch = Stopwatch.StartNew();
            using var response = await client.SendAsync(request, cts.Token);
            stopwatch.Stop();

            var responseBody = await response.Content.ReadAsStringAsync();

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
                return ToolResponse.Error("SSL_ERROR", $"SSL/TLS 错误：{ex.Message}");
            }
            return ToolResponse.Error("CONNECTION_FAILED", $"连接失败：{ex.Message}");
        }
        catch (Exception ex)
        {
            return ToolResponse.Error("EXECUTION_FAILED", ex.Message);
        }
    }

    /// <summary>
    /// 下载文件
    /// </summary>
    private async Task<ToolResponse> DownloadFileAsync(JsonElement parameters)
    {
        try
        {
            var url = parameters.GetProperty("url").GetString();
            if (string.IsNullOrWhiteSpace(url))
            {
                return ToolResponse.Error("INVALID_URL", "URL 不能为空");
            }

            var savePath = parameters.GetProperty("save_path").GetString();
            if (string.IsNullOrWhiteSpace(savePath))
            {
                return ToolResponse.Error("INVALID_PARAMETERS", "保存路径不能为空");
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                return ToolResponse.Error("INVALID_URL", "无效的 URL 格式");
            }

            var overwrite = parameters.TryGetProperty("overwrite", out var overwriteProp) && overwriteProp.GetBoolean();
            if (File.Exists(savePath) && !overwrite)
            {
                return ToolResponse.Error("FILE_EXISTS", $"文件已存在：{savePath}");
            }

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
            return ToolResponse.Error("CONNECTION_FAILED", $"连接失败：{ex.Message}");
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
    /// Ping 主机
    /// </summary>
    private async Task<ToolResponse> PingHostAsync(JsonElement parameters)
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

            count = Math.Clamp(count, 1, 100);
            timeout = Math.Clamp(timeout, 1, 60);

            try
            {
                using var ping = new Ping();
                var details = new List<object>();
                var successCount = 0;
                var times = new List<double>();
                string? ipAddress = null;

                for (int i = 0; i < count; i++)
                {
                    try
                    {
                        var reply = await ping.SendPingAsync(host, timeout * 1000);
                        
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
                return ToolResponse.Error("HOST_UNREACHABLE", $"无法访问主机：{ex.Message}");
            }
        }
        catch (Exception ex)
        {
            return ToolResponse.Error("EXECUTION_FAILED", ex.Message);
        }
    }

    /// <summary>
    /// DNS 解析
    /// </summary>
    private async Task<ToolResponse> ResolveDnsAsync(JsonElement parameters)
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

            var records = new List<object>();

            switch (recordType)
            {
                case "A":
                    var addresses = await Dns.GetHostAddressesAsync(domain);
                    foreach (var addr in addresses.Where(a => a.AddressFamily == AddressFamily.InterNetwork))
                    {
                        records.Add(new { type = "A", value = addr.ToString(), ttl = 0 });
                    }
                    break;

                case "AAAA":
                    var addresses6 = await Dns.GetHostAddressesAsync(domain);
                    foreach (var addr in addresses6.Where(a => a.AddressFamily == AddressFamily.InterNetworkV6))
                    {
                        records.Add(new { type = "AAAA", value = addr.ToString(), ttl = 0 });
                    }
                    break;

                default:
                    return ToolResponse.Error("RESOLUTION_FAILED", $"不支持的记录类型：{recordType}");
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

    /// <summary>
    /// 检查端口状态
    /// </summary>
    private async Task<ToolResponse> CheckPortAsync(JsonElement parameters)
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
                return ToolResponse.Error("INVALID_PORT", "端口号必须在 1-65535 之间");
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
}