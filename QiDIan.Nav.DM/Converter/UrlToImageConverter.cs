using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace QiDian.Nav.DM.Converter;

/// <summary>
/// 把封面地址（网页 URL 或本地路径）转成 ImageSource。
/// 空地址 / 非法地址 / 加载失败返回 null —— 此时卡片底层会露出占位背景。
/// </summary>
public class UrlToImageConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var url = value as string;
        if (string.IsNullOrWhiteSpace(url)) return null;

        try
        {
            var uri = url.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? new Uri(url, UriKind.Absolute)
                : new Uri(Path.GetFullPath(url), UriKind.Absolute);

            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = uri;
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.EndInit();
            bmp.Freeze(); // 跨线程安全，避免 WPF 报"调用线程无法访问该对象"
            return bmp;
        }
        catch
        {
            return null;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
