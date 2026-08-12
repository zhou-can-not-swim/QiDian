using QiDian.Contracts;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace QiDian.Nav.Settings;

public class SettingsViewModel : ViewModelBase
{
    private string _titleText = "";
    public string TitleText
    {
        get => _titleText;
        set => this.RaiseAndSetIfChanged(ref _titleText, value);
    }

    [Reactive]
    public string StatusMessage { get; set; } = "设置插件页面已加载";

    public SettingsViewModel()
    {
        TitleText = "设置";
    }
}
