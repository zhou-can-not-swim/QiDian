using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace QiDian.Converters
{
    /// <summary>
    /// 图标转换器：绑定 FileEntry.Icon（ImageSource）。
    /// 图标提取是重操作（ExtractAssociatedIcon），由 ViewModel 在后台线程调用
    /// ExtractIcon 完成并缓存，避免在 UI 线程同步提取导致数据量多时界面卡死。
    /// Icon 为 null 时显示默认图标。
    /// </summary>
    internal class FilePathToIconConverter : IValueConverter
    {
        private static readonly ConcurrentDictionary<string, ImageSource> Cache = new();
        private static ImageSource? _defaultIcon;

        public static ImageSource DefaultIcon
        {
            get
            {
                if (_defaultIcon == null)
                {
                    _defaultIcon = CreateDefaultIcon();
                    _defaultIcon.Freeze();
                }
                return _defaultIcon;
            }
        }

        /// <summary>
        /// 后台线程调用：提取路径对应的图标（带缓存，返回结果已 Freeze，可跨线程使用）。
        /// 提取失败或文件不存在时返回默认图标。
        /// </summary>
        public static ImageSource ExtractIcon(string path)
        {
            return Cache.GetOrAdd(path, p =>
            {
                if (!File.Exists(p))
                    return DefaultIcon;
                try
                {
                    using var icon = System.Drawing.Icon.ExtractAssociatedIcon(p);
                    if (icon == null)
                        return DefaultIcon;
                    var bmp = ConvertIconToBitmapSource(icon);
                    bmp.Freeze(); // 冻结后可在后台线程创建、UI 线程绑定使用
                    return bmp;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"提取图标失败: {ex.Message}");
                    return DefaultIcon;
                }
            });
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // value = FileEntry.Icon（后台线程填充；null 时显示默认图标）
            return value is ImageSource img ? img : DefaultIcon;
        }

        private static BitmapSource ConvertIconToBitmapSource(System.Drawing.Icon icon)
        {
            using (var bitmap = icon.ToBitmap())
            {
                IntPtr hBitmap = bitmap.GetHbitmap();
                try
                {
                    return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                        hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                }
                finally
                {
                    DeleteObject(hBitmap);
                }
            }
        }

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        // 默认图标：无参数错误，Emoji正常显示
        private static BitmapSource CreateDefaultIcon()
        {
            var text = new FormattedText(
                    "📄",
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Segoe UI Emoji"),
                    20,
                    Brushes.DarkGray);

            var drawingVisual = new DrawingVisual();
            using (var dc = drawingVisual.RenderOpen())
            {
                // 背景
                dc.DrawRectangle(Brushes.LightGray, null, new Rect(0, 0, 32, 32));

                // 居中
                double x = (32 - text.Width) / 2;
                double y = (32 - text.Height) / 2;
                dc.DrawText(text, new Point(x, y));
            }

            var rtb = new RenderTargetBitmap(32, 32, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(drawingVisual);
            rtb.Freeze();
            return rtb;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
