namespace QiDian.Contracts;

/// <summary>
/// 内置命令插件契约：每个前缀（如 tr:、h:）对应一个插件。
/// 插件在编译期注册（无需运行时扫描），提供右下角弹窗的内容 UserControl 与执行逻辑。
/// 主程序的 CommandRouter 负责解析前缀并路由，PopupHost 负责弹窗宿主。
/// </summary>
public interface ICommandPlugin
{
    /// <summary>命令前缀（不含冒号），如 "tr"、"h"</summary>
    string Prefix { get; }

    /// <summary>显示名称，如 "翻译"、"内置命令"</summary>
    string Title { get; }

    /// <summary>一句话说明，展示在 h: 命令列表里</summary>
    string Description { get; }

    /// <summary>弹窗内容 View 类型（UserControl）</summary>
    Type ViewType { get; }

    /// <summary>弹窗内容 ViewModel 类型</summary>
    Type ViewModelType { get; }
}

/// <summary>
/// 可选：需要接收冒号后参数执行的插件，由其 ViewModel 实现此接口。
/// 主程序路由时调用 ExecuteAsync（UI 线程），结果为异步填充进 ViewModel。
/// </summary>
public interface IExecutableCommand
{
    /// <summary>执行命令。argument 为冒号后的内容（可能为空字符串），可为 null。</summary>
    Task ExecuteAsync(string? argument);
}
