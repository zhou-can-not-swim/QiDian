using System.Globalization;
using System.Windows.Data;

namespace QiDian.Nav.DM.Converter;

/// <summary>取标题首字符，用于封面未加载时的占位文字</summary>
public class TitleFirstCharConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var s = value as string;
        if (string.IsNullOrWhiteSpace(s)) return "番";
        return s.Trim().Substring(0, 1);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
