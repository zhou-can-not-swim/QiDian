using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace QiDian.Converters
{

    //返回给Image的是BitmapSource
    internal class FilePathToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string filePath && File.Exists(filePath))
            {
                try
                {
                    var icon = System.Drawing.Icon.ExtractAssociatedIcon(filePath);
                    if (icon != null)
                    {
                        return ConvertIconToBitmapSource(icon);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"提取图标失败: {ex.Message}");
                }
            }

            // 返回默认图标
            return GetDefaultIcon(new FormattedText(
                    "📄",
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Segoe UI Emoji"),
                    20,
                    Brushes.DarkGray));
        }

        private BitmapSource ConvertIconToBitmapSource(System.Drawing.Icon icon)
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
                    icon.Dispose();
                }
            }
        }

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        // 修复：正确的默认图标，无参数错误，Emoji正常显示
        private BitmapSource GetDefaultIcon(FormattedText text)
        {
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