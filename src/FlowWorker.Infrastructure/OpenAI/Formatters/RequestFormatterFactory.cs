using FlowWorker.Core.Interfaces;

namespace FlowWorker.Infrastructure.OpenAI.Formatters;

/// <summary>
/// 请求格式化器工厂
/// 管理和提供不同的请求格式化器
/// </summary>
public class RequestFormatterFactory : IRequestFormatterFactory
{
    private readonly Dictionary<string, IRequestFormatter> _formatters;
    private readonly string _defaultFormatterName;

    public RequestFormatterFactory()
    {
        // 注册所有可用的格式化器
        var formatters = new List<IRequestFormatter>
        {
            new ClineRequestFormatter()
            // 未来可以在这里添加更多格式化器
            // new OpenAIRequestFormatter(),
            // new AnthropicRequestFormatter(),
            // new GeminiRequestFormatter(),
        };

        _formatters = formatters.ToDictionary(f => f.Name, f => f);
        _defaultFormatterName = "cline"; // 默认使用 cline 模式
    }

    public IEnumerable<IRequestFormatter> GetAllFormatters()
    {
        return _formatters.Values;
    }

    public IRequestFormatter GetFormatter(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return GetDefaultFormatter();
        }

        var formatterName = name.ToLowerInvariant();
        if (_formatters.TryGetValue(formatterName, out var formatter))
        {
            return formatter;
        }

        throw new ArgumentException($"未找到名为 '{name}' 的请求格式化器", nameof(name));
    }

    public IRequestFormatter GetDefaultFormatter()
    {
        return _formatters[_defaultFormatterName];
    }

    public bool HasFormatter(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        return _formatters.ContainsKey(name.ToLowerInvariant());
    }
}
