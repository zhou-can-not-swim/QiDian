using QiDian.Contracts;
using QiDian.Nav.WebSitePage.ViewModels;

namespace QiDian.Nav.WebSitePage;

/// <summary>
/// Home 导航插件模块描述。主程序通过无参构造实例化它，读取导航元数据并注册页面。
/// </summary>
public sealed class WebSitePageModule : INavModule
{
    public string ViewKey => "WebSitePage";
    public string Title => "网站精选";
    public string Icon => "";
    public int Order => 3;
    public Type ViewType => typeof(WebSitePageView);
    public Type ViewModelType => typeof(WebSitePageViewModel);
}
