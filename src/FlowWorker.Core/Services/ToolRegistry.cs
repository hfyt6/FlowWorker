using FlowWorker.Core.Interfaces;

namespace FlowWorker.Core.Services;

/// <summary>
/// 工具信息类
/// </summary>
public class ToolInfo
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string HandlerName { get; set; } = string.Empty;
}

/// <summary>
/// 工具注册表
/// 用于注册和管理所有可用的工具
/// 支持细粒度工具名注册，每个工具处理器可以注册多个细粒度工具名
/// </summary>
public class ToolRegistry
{
    // 细粒度工具名 -> 工具处理器
    private readonly Dictionary<string, IToolHandler> _tools = new();
    
    // 工具处理器列表（用于获取所有处理器）
    private readonly List<IToolHandler> _handlers = new();
    
    /// <summary>
    /// 注册工具处理器
    /// 会自动注册该处理器支持的所有细粒度工具名
    /// </summary>
    /// <param name="tool">工具处理器</param>
    public void Register(IToolHandler tool)
    {
        if (tool == null) throw new ArgumentNullException(nameof(tool));
        if (string.IsNullOrWhiteSpace(tool.Name)) throw new ArgumentException("工具名称不能为空", nameof(tool));
        
        _handlers.Add(tool);
        
        // 注册所有支持的细粒度工具名
        foreach (var action in tool.SupportedActions)
        {
            _tools[action.ToLowerInvariant()] = tool;
        }
    }
    
    /// <summary>
    /// 获取工具处理器（通过细粒度工具名）
    /// </summary>
    /// <param name="name">工具名称（细粒度）</param>
    /// <returns>工具处理器，如果不存在则返回 null</returns>
    public IToolHandler? GetTool(string name)
    {
        return _tools.TryGetValue(name.ToLowerInvariant(), out var tool) ? tool : null;
    }
    
    /// <summary>
    /// 尝试获取工具处理器
    /// </summary>
    /// <param name="name">工具名称（细粒度）</param>
    /// <param name="tool">输出的工具处理器</param>
    /// <returns>是否成功获取</returns>
    public bool TryGetTool(string name, out IToolHandler? tool)
    {
        var result = _tools.TryGetValue(name.ToLowerInvariant(), out var foundTool);
        tool = foundTool;
        return result;
    }
    
    /// <summary>
    /// 获取所有已注册的工具信息
    /// </summary>
    /// <returns>工具信息列表</returns>
    public IEnumerable<ToolInfo> GetAllTools()
    {
        return _tools.Select(t => new ToolInfo 
        { 
            Name = t.Key, 
            HandlerName = t.Value.Name 
        });
    }
    
    /// <summary>
    /// 获取所有已注册的工具处理器
    /// </summary>
    /// <returns>工具处理器列表</returns>
    public IEnumerable<IToolHandler> GetAllHandlers()
    {
        return _handlers;
    }
    
    /// <summary>
    /// 检查工具是否存在（细粒度工具名）
    /// </summary>
    /// <param name="name">工具名称</param>
    /// <returns>是否存在</returns>
    public bool HasTool(string name)
    {
        return _tools.ContainsKey(name.ToLowerInvariant());
    }
    
    /// <summary>
    /// 获取工具数量（细粒度工具名数量）
    /// </summary>
    public int Count => _tools.Count;
    
    /// <summary>
    /// 清除所有工具
    /// </summary>
    public void Clear()
    {
        _tools.Clear();
        _handlers.Clear();
    }
    
    /// <summary>
    /// 注销工具处理器
    /// </summary>
    /// <param name="handlerName">工具处理器名称</param>
    /// <returns>是否成功注销</returns>
    public bool UnregisterHandler(string handlerName)
    {
        var handler = _handlers.FirstOrDefault(h => h.Name == handlerName);
        if (handler == null) return false;
        
        _handlers.Remove(handler);
        
        // 移除该处理器注册的所有细粒度工具名
        var keysToRemove = _tools.Where(t => t.Value == handler).Select(t => t.Key).ToList();
        foreach (var key in keysToRemove)
        {
            _tools.Remove(key);
        }
        
        return true;
    }
}