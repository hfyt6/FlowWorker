using FlowWorker.Core.Interfaces;
using FlowWorker.Core.Tools;
using Microsoft.Extensions.DependencyInjection;

namespace FlowWorker.Core.Services;

/// <summary>
/// 工具服务依赖注入扩展类
/// </summary>
public static class ToolServiceCollectionExtensions
{
    /// <summary>
    /// 注册所有内置工具到 DI 容器
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddBuiltInTools(this IServiceCollection services)
    {
        // 注册工具注册表和工具执行器
        services.AddSingleton<ToolRegistry>();
        services.AddSingleton<ToolExecutor>();

        // 注册所有工具处理器
        services.AddSingleton<IToolHandler, CalculatorTool>();
        services.AddSingleton<IToolHandler, FilesystemTool>();
        services.AddSingleton<IToolHandler, NetworkTool>();
        services.AddSingleton<IToolHandler, ProcessTool>();
        services.AddSingleton<IToolHandler, TextTool>();
        services.AddSingleton<IToolHandler, CodeAnalysisTool>();
        services.AddSingleton<IToolHandler, CodeManipulationTool>();
        services.AddSingleton<IToolHandler, VersionControlTool>();

        // 自动注册工具到注册表
        services.BuildServiceProvider().GetRequiredService<ToolRegistry>();

        return services;
    }

    /// <summary>
    /// 注册所有内置工具到 DI 容器，并在注册后自动注册到 ToolRegistry
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddBuiltInToolsWithAutoRegistration(this IServiceCollection services)
    {
        // 注册工具注册表和工具执行器
        services.AddSingleton<ToolRegistry>();
        services.AddSingleton<ToolExecutor>();

        // 注册所有工具处理器
        services.AddSingleton<IToolHandler, CalculatorTool>();
        services.AddSingleton<IToolHandler, FilesystemTool>();
        services.AddSingleton<IToolHandler, NetworkTool>();
        services.AddSingleton<IToolHandler, ProcessTool>();
        services.AddSingleton<IToolHandler, TextTool>();
        services.AddSingleton<IToolHandler, CodeAnalysisTool>();
        services.AddSingleton<IToolHandler, CodeManipulationTool>();
        services.AddSingleton<IToolHandler, VersionControlTool>();

        // 使用工厂方法在启动时自动注册工具
        services.AddSingleton<ToolRegistry>(provider =>
        {
            var registry = new ToolRegistry();
            
            // 从 DI 容器获取并注册所有工具
            var tools = provider.GetServices<IToolHandler>();
            foreach (var tool in tools)
            {
                registry.Register(tool);
            }
            
            return registry;
        });

        return services;
    }
}