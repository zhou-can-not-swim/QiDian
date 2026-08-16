using System.Collections.ObjectModel;
using QiDian.Contracts;

namespace QiDian.Plugins.Help;

/// <summary>
/// h: 命令的 ViewModel。命令列表由主程序注入（所有已注册插件），
/// 因此执行时无需参数。
/// </summary>
public class HelpViewModel : ViewModelBase,IExecutableCommand

{
    /// <summary>全部已注册的命令插件（按前缀排序）</summary>
    public ObservableCollection<ICommandPlugin> Items { get; } = new();

    public HelpViewModel(IReadOnlyList<ICommandPlugin> commands)
    {
        foreach (var cmd in commands.OrderBy(c => c.Prefix, StringComparer.OrdinalIgnoreCase))
            Items.Add(cmd);
    }

    public async Task ExecuteAsync(string? argument)
    {
        await Task.Delay(1000);
    }
}
