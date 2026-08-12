using QiDian.Contracts;

namespace QiDian.Nav.Settings;

/// <summary>
/// Settings 导航插件模块描述。主程序通过无参构造实例化它，读取导航元数据并注册页面。
/// </summary>
public sealed class SettingsModule : INavModule
{
    public string ViewKey => "Settings";
    public string Title => "设置";
    public string Icon => "";
    public int Order => 99;
    public Type ViewType => typeof(SettingsView);
    public Type ViewModelType => typeof(SettingsViewModel);
}
