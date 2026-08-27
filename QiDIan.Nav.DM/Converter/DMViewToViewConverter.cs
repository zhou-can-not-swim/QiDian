using QiDian.Nav.DM.ViewModels;
using QiDian.Nav.DM.Views;
using System.Globalization;
using System.Windows.Data;

namespace QiDian.Nav.DM.Converter;

/// <summary>
/// DM 插件内部的 ViewModel → View 转换器，供宿主 <see cref="DMView"/> 的 ContentControl 使用。
/// 主程序全局的 ViewModelToViewConverter 走 DI + 注册表，不适用于插件内部这些瞬态页面，
/// 所以这里在插件内自己做一个直白的映射。
/// </summary>
public class DMViewToViewConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value switch
        {
            DMHomeViewModel vm => new DMHomeView { ViewModel = vm },
            DMSearchViewModel vm => new DMSearchView { ViewModel = vm },
            DMDetailViewModel vm => new DMDetailView { ViewModel = vm },
            DMPlayerViewModel vm => new DMPlayerView { ViewModel = vm },
            _ => null,
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
