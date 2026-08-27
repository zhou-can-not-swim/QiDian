using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace QiDian.Nav.DM.Converter;

/// <summary>int 值等于 0 → Collapsed，否则 Visible（用于"暂无选集"提示）</summary>
public class CountToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is int n && n > 0 ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
