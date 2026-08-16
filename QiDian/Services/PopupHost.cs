using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace QiDian.Services;

/// <summary>
/// 右下角命令结果弹窗：单例复用（一个窗口、换内容），停靠在屏幕右下角。
/// Esc / 失焦 / 超时自动隐藏；鼠标悬停时暂缓超时隐藏。
/// </summary>
public class PopupHost : Window
{
    private const double ScreenMargin = 20;
    private static readonly TimeSpan AutoHideDelay = TimeSpan.FromSeconds(6);

    private DispatcherTimer? _autoHideTimer;

    public PopupHost()
    {
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        Topmost = true;
        ShowInTaskbar = false;
        // 不激活弹窗：避免抢走宿主窗口焦点后又被 Deactivated 立即隐藏
        ShowActivated = false;
        ResizeMode = ResizeMode.NoResize;
        SizeToContent = SizeToContent.WidthAndHeight;
        WindowStartupLocation = WindowStartupLocation.Manual;

        PreviewKeyDown += OnPreviewKeyDown;
        Deactivated += (_, _) => Hide();
        MouseEnter += (_, _) => CancelAutoHide();
        MouseLeave += (_, _) => ScheduleAutoHide();
    }

    /// <summary>显示插件提供的内容，停靠在右下角并开始自动隐藏计时</summary>
    public void Show(FrameworkElement content)
    {
        Content = new Border
        {
            Background = Brushes.White,
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(16),
            Child = content
        };

        Show();
        PositionBottomRight();
        ScheduleAutoHide();
    }

    private void PositionBottomRight()
    {
        UpdateLayout();
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - ActualWidth - ScreenMargin;
        Top = workArea.Bottom - ActualHeight - ScreenMargin;
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Hide();
            e.Handled = true;
        }
    }

    private void ScheduleAutoHide()
    {
        CancelAutoHide();
        _autoHideTimer = new DispatcherTimer { Interval = AutoHideDelay };
        _autoHideTimer.Tick += (_, _) => { Hide(); _autoHideTimer?.Stop(); };
        _autoHideTimer.Start();
    }

    private void CancelAutoHide()
    {
        if (_autoHideTimer != null)
        {
            _autoHideTimer.Stop();
            _autoHideTimer = null;
        }
    }
}
