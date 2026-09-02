using QiDian.Nav.WebSitePage.Models;
using QiDian.Nav.WebSitePage.ViewModels;
using ReactiveUI;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

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
