using QiDian.Contracts;

namespace QiDian.Nav.Home;

/// <summary>
/// Home 导航插件模块描述。主程序通过无参构造实例化它，读取导航元数据并注册页面。
/// </summary>
public sealed class HomeModule : INavModule
{
    public string ViewKey => "Home";
    public string Title => "首页";
    public string Icon => "";
    public int Order => 1;
    public Type ViewType => typeof(HomeView);
    public Type ViewModelType => typeof(HomeViewModel);
}
