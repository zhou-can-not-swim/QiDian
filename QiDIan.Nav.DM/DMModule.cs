using QiDian.Contracts;
using QiDian.Nav.DM;

namespace QiDian.Nav.DM;

/// <summary>
/// Home 导航插件模块描述。主程序通过无参构造实例化它，读取导航元数据并注册页面。
/// </summary>
public sealed class DMModule : INavModule
{
    public string ViewKey => "DM";
    public string Title => "动漫巴士网";
    public string Icon => "";
    public int Order => 2;
    public Type ViewType => typeof(DMView);
    public Type ViewModelType => typeof(DMViewModel);
}
