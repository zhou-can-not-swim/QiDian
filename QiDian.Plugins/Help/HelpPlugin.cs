using QiDian.Contracts;

namespace QiDian.Plugins.Help;

/// <summary>
/// h: 命令 —— 列出全部已注册的前缀命令，作为命令系统的「目录」。
/// </summary>
public sealed class HelpPlugin : ICommandPlugin
{
    public string Prefix => "h";
    public string Title => "内置命令";
    public string Description => "列出全部内置命令前缀";
    public Type ViewType => typeof(HelpView);
    public Type ViewModelType => typeof(HelpViewModel);
}
