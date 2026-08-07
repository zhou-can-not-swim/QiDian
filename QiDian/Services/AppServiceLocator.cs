using Microsoft.Extensions.DependencyInjection;

namespace QiDian.Services;

/// <summary>
/// 静态服务定位器 —— 供 ViewModelToViewConverter 等无法通过 DI 构造的组件访问 IServiceProvider。
/// 在 App.OnStartup 中通过 AppServiceLocator.Initialize(...) 初始化。
/// </summary>
public static class AppServiceLocator
{
    public static IServiceProvider? ServiceProvider { get; private set; }
    public static void Initialize(IServiceProvider sp) => ServiceProvider = sp;
}