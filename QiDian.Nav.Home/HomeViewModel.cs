using QiDian.Contracts;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace QiDian.Nav.Home;

public class HomeViewModel : ViewModelBase
{
    private string _welcomeText = "";
    public string WelcomeText
    {
        get => _welcomeText;
        set => this.RaiseAndSetIfChanged(ref _welcomeText, value);
    }

    [Reactive]
    public string StatusMessage { get; set; } = "插件页面已加载，首页运行正常";

    public HomeViewModel()
    {
        WelcomeText = "欢迎使用 QiDian";
    }
}
