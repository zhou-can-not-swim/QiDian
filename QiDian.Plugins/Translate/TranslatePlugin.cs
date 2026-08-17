using QiDian.Contracts;
using QiDian.Plugins.Translate;

namespace QiDian.Plugins.Translate;

/// <summary>
/// h: 命令 —— 列出全部已注册的前缀命令，作为命令系统的「目录」。
/// </summary>
public sealed class TranslatePlugin : ICommandPlugin
{
    public string Prefix => "tr";
    public string Title => "翻译命令";
    public string Description => "翻译结果";
    public Type ViewType => typeof(TranslateView);
    public Type ViewModelType => typeof(TranslateViewModel);
}
