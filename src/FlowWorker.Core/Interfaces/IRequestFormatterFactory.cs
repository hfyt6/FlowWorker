namespace FlowWorker.Core.Interfaces;

/// <summary>
/// 请求格式化器工厂接口
/// 用于管理和获取不同的请求格式化器
/// </summary>
public interface IRequestFormatterFactory
{
    /// <summary>
    /// 获取所有可用的格式化器
    /// </summary>
    /// <returns>格式化器列表</returns>
    IEnumerable<IRequestFormatter> GetAllFormatters();

    /// <summary>
    /// 根据名称获取格式化器
    /// </summary>
    /// <param name="name">格式化器名称</param>
    /// <returns>格式化器实例</returns>
    IRequestFormatter GetFormatter(string name);

    /// <summary>
    /// 获取默认格式化器
    /// </summary>
    /// <returns>默认格式化器实例</returns>
    IRequestFormatter GetDefaultFormatter();

    /// <summary>
    /// 检查格式化器是否存在
    /// </summary>
    /// <param name="name">格式化器名称</param>
    /// <returns>是否存在</returns>
    bool HasFormatter(string name);
}
