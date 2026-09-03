using QiDian.Nav.WebSitePage.Models;
using QiDian.Nav.WebSitePage.ViewModels;
using ReactiveUI;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace QiDian.Nav.WebSitePage
{

    public partial class WebSitePageView : UserControl, IViewFor<WebSitePageViewModel>
    {
        /// <summary>抽屉宽度（px），与 XAML 中 DrawerHost.Width 保持一致</summary>
        private const double DrawerWidth = 340;

        private WebSitePageViewModel? _attachedVm;


        public WebSitePageView()
        {
            InitializeComponent();
        }

        /// <summary>点击列表（含重复点击已选项）：始终让抽屉滑出</summary>
        private void SiteList_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel == null) return;
            if (FindAncestor<ListBoxItem>(e.OriginalSource as DependencyObject) is { } item
                && item.DataContext is WebSiteItem site)
            {
                ViewModel.SelectSite(site);
            }
        }

        /// <summary>抽屉子控件请求关闭</summary>
        private void DetailView_CloseRequested(object? sender, EventArgs e)
        {
            ViewModel?.CloseDrawer();
        }

        #region 抽屉滑入/滑出动画（监听 ViewModel.IsDrawerOpen）

        private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(WebSitePageViewModel.IsDrawerOpen))
            {
                AnimateDrawer(ViewModel?.IsDrawerOpen == true);
            }
            else if (e.PropertyName == nameof(WebSitePageViewModel.IsAddOpen))
            {
                AnimateOverlay(AddSiteLayer, ViewModel?.IsAddOpen == true, NewNameBox,
                    () => ViewModel?.IsAddOpen == true);
            }
            else if (e.PropertyName == nameof(WebSitePageViewModel.IsTagOpen))
            {
                AnimateOverlay(AddTagLayer, ViewModel?.IsTagOpen == true, NewTagNameBox,
                    () => ViewModel?.IsTagOpen == true);
            }
        }

        /// <summary>淡入/淡出对话框覆盖层，打开时把焦点交给首个输入框</summary>
        private void AnimateOverlay(Grid layer, bool open, TextBox? focusBox, Func<bool> isOpenNow)
        {
            if (open)
            {
                layer.Visibility = Visibility.Visible;
                var fadeIn = new DoubleAnimation(layer.Opacity, 1, TimeSpan.FromMilliseconds(150))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                layer.BeginAnimation(OpacityProperty, fadeIn);

                // 等层布局完成后再聚焦，确保输入框可接收键盘
                if (focusBox != null)
                    Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
                    {
                        focusBox.Focus();
                        focusBox.SelectAll();
                    }));
            }
            else
            {
                var fadeOut = new DoubleAnimation(layer.Opacity, 0, TimeSpan.FromMilliseconds(120))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
                };
                fadeOut.Completed += (_, _) =>
                {
                    // 淡出期间若又被打开，则不要隐藏
                    if (!isOpenNow())
                        layer.Visibility = Visibility.Collapsed;
                };
                layer.BeginAnimation(OpacityProperty, fadeOut);
            }
        }

        /// <summary>网站弹窗快捷键：回车确认，Esc 取消</summary>
        private void AddSiteLayer_KeyDown(object sender, KeyEventArgs e)
        {
            if (ViewModel is { } vm)
                HandleDialogKey(e, vm.CancelAddCommand, vm.ConfirmAddCommand);
        }

        /// <summary>标签弹窗快捷键：回车确认，Esc 取消</summary>
        private void AddTagLayer_KeyDown(object sender, KeyEventArgs e)
        {
            if (ViewModel is { } vm)
                HandleDialogKey(e, vm.CancelTagCommand, vm.ConfirmTagCommand);
        }

        /// <summary>对话框通用快捷键处理（多行输入框内回车不拦截）</summary>
        private static void HandleDialogKey(KeyEventArgs e, ICommand cancel, ICommand confirm)
        {
            if (e.OriginalSource is TextBox { AcceptsReturn: true }) return;

            switch (e.Key)
            {
                case Key.Escape:
                    if (cancel.CanExecute(null))
                    {
                        e.Handled = true;
                        cancel.Execute(null);
                    }
                    break;
                case Key.Enter:
                    if (confirm.CanExecute(null))
                    {
                        e.Handled = true;
                        confirm.Execute(null);
                    }
                    break;
            }
        }

        private void AnimateDrawer(bool open)
        {
            double from = DrawerSlide.X; // 以当前位移为起点，中断动画时也不会跳动

            if (open)
            {
                DrawerHost.Visibility = Visibility.Visible;
                var anim = new DoubleAnimation(from, 0, TimeSpan.FromMilliseconds(220))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                DrawerSlide.BeginAnimation(TranslateTransform.XProperty, anim);
            }
            else
            {
                var anim = new DoubleAnimation(from, DrawerWidth, TimeSpan.FromMilliseconds(180))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
                };
                anim.Completed += (_, _) =>
                {
                    // 关闭动画期间若又被打开，则不要隐藏
                    if (ViewModel?.IsDrawerOpen != true)
                        DrawerHost.Visibility = Visibility.Collapsed;
                };
                DrawerSlide.BeginAnimation(TranslateTransform.XProperty, anim);
            }
        }

        #endregion

        #region ViewModel

        public WebSitePageViewModel? ViewModel
        {
            get => (WebSitePageViewModel?)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        object? IViewFor.ViewModel
        {
            get => ViewModel;
            set => ViewModel = (WebSitePageViewModel?)value;
        }

        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register("ViewModel", typeof(WebSitePageViewModel), typeof(WebSitePageView),
                new PropertyMetadata(null, (d, e) =>
                {
                    if (d is not WebSitePageView view) return;
                    view.DataContext = e.NewValue;

                    // 切换订阅：跟随 ViewModel 的生命周期
                    if (view._attachedVm is { } oldVm)
                        oldVm.PropertyChanged -= view.OnVmPropertyChanged;
                    view._attachedVm = e.NewValue as WebSitePageViewModel;
                    if (view._attachedVm is { } newVm)
                        newVm.PropertyChanged += view.OnVmPropertyChanged;
                }));

        #endregion

        /// <summary>沿可视树向上查找指定类型的祖先（含自身）</summary>
        private static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T match) return match;
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }
    }
}
