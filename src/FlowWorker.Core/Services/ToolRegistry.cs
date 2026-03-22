using FlowWorker.Core.Interfaces;

namespace FlowWorker.Core.Services;

/// <summary>
/// 工具信息类
/// </summary>
public class ToolInfo
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Actions { get; set; } = new();
}

/// <summary>
/// 工具注册表
/// 用于注册和管理所有可用的工具
/// </summary>
public class ToolRegistry
{
    private readonly Dictionary<string, IToolHandler> _tools = new();
    
    /// <summary>
    /// 注册工具
    /// </summary>
    /// <param name="tool">工具处理器</param>
    public void Register(IToolHandler tool)
    {
        if (tool == null) throw new ArgumentNullException(nameof(tool));
        if (string.IsNullOrWhiteSpace(tool.Name)) throw new ArgumentException("工具名称不能为空", nameof(tool));
        
        _tools[tool.Name] = tool;
    }
    
    /// <summary>
    /// 获取工具
    /// </summary>
    /// <param name="name">工具名称</param>
    /// <returns>工具处理器，如果不存在则返回 null</returns>
    public IToolHandler? GetTool(string name)
    {
        return _tools.TryGetValue(name, out var tool) ? tool : null;
    }
    
    /// <summary>
    /// 尝试获取工具
    /// </summary>
    /// <param name="name">工具名称</param>
    /// <param name="tool">输出的工具处理器</param>
    /// <returns>是否成功获取</returns>
    public bool TryGetTool(string name, out IToolHandler? tool)
    {
        return _tools.TryGetValue(name, out tool);
    }
    
    /// <summary>
    /// 获取所有已注册的工具
    /// </summary>
    /// <returns>工具信息列表</returns>
    public IEnumerable<ToolInfo> GetAllTools()
    {
        return _tools.Values.Select(t => new ToolInfo { Name = t.Name });
    }
    
    /// <summary>
    /// 检查工具是否存在
    /// </summary>
    /// <param name="name">工具名称</param>
    /// <returns>是否存在</returns>
    public bool HasTool(string name)
    {
        return _tools.ContainsKey(name);
    }
    
    /// <summary>
    /// 获取工具数量
    /// </summary>
    public int Count => _tools.Count;
    
    /// <summary>
    /// 清除所有工具
    /// </summary>
    public void Clear()
    {
        _tools.Clear();
    }
    
    /// <summary>
    /// 注销工具
    /// </summary>
    /// <param name="name">工具名称</param>
    /// <returns>是否成功注销</returns>
    public bool Unregister(string name)
    {
        return _tools.Remove(name);
    }
}