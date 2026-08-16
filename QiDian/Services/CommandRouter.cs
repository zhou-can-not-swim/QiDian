using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using QiDian.Contracts;

namespace QiDian.Services;

/// <summary>
/// 前缀命令路由：维护 前缀 → ICommandPlugin 的注册表（编译期注册，不动态扫描），
/// 解析搜索框中的 "前缀:参数" 并路由到对应插件，弹出右下角结果窗。
/// </summary>
public interface ICommandRouter
{
    /// <summary>已注册的全部命令插件（供插件窗口下拉框绑定）</summary>
    IReadOnlyList<ICommandPlugin> Plugins { get; }

    /// <summary>注册一个命令插件</summary>
    void Register(ICommandPlugin plugin);

    /// <summary>判断关键词是否命中已注册的前缀命令</summary>
    bool TryGetCommand(string keyword, out string prefix, out string arg);

    /// <summary>返回前缀对应的命令名称（未命中则原样返回）</summary>
    string Describe(string prefix);

    /// <summary>执行命令并弹出结果窗</summary>
    void Run(string prefix, string arg);
}

public class CommandRouter : ICommandRouter
{
    private readonly IServiceProvider _services;
    private readonly PopupHost _popupHost;
    private readonly Dictionary<string, ICommandPlugin> _plugins = new(StringComparer.OrdinalIgnoreCase);

    public CommandRouter(IServiceProvider services, PopupHost popupHost)
    {
        _services = services;
        _popupHost = popupHost;
    }

    public IReadOnlyList<ICommandPlugin> Plugins => _plugins.Values.ToList();

    public void Register(ICommandPlugin plugin)
    {
        if (string.IsNullOrWhiteSpace(plugin.Prefix))
            throw new ArgumentException("命令前缀不能为空", nameof(plugin));
        _plugins[plugin.Prefix] = plugin;
    }

    /// <summary>
    /// 解析 "前缀:参数"。前缀限定为 1~8 位纯字母，避免把 Windows 路径（C:\...）误判为命令。
    /// </summary>
    public bool TryGetCommand(string keyword, out string prefix, out string arg)
    {
        prefix = "";
        arg = "";
        if (string.IsNullOrWhiteSpace(keyword))
            return false;

        int colon = keyword.IndexOf(':');
        if (colon <= 0)
            return false;

        var head = keyword[..colon];
        if (head.Length > 8 || !head.All(char.IsAsciiLetter))
            return false;

        if (!_plugins.ContainsKey(head))
            return false;

        prefix = head;
        arg = keyword[(colon + 1)..];
        return true;
    }

    public string Describe(string prefix) =>
        _plugins.TryGetValue(prefix, out var p) ? p.Title : prefix;

    /// <summary>
    /// 路由到插件：解析 ViewModel/View，注入参数并执行，最后用 PopupHost 弹出结果。
    /// 必须在 UI 线程调用。
    /// </summary>
    public void Run(string prefix, string arg)
    {
        if (!_plugins.TryGetValue(prefix, out var plugin))
            return;

        var vm = Resolve(plugin.ViewModelType);
        var view = Resolve(plugin.ViewType) as FrameworkElement;
        if (vm == null || view == null)
            return;

        view.DataContext = vm;
        if (vm is IExecutableCommand exec)
            _ = exec.ExecuteAsync(arg);

        _popupHost.Show(view);
    }

    private object? Resolve(Type type) =>
        _services.GetService(type) ?? Activator.CreateInstance(type);
}
