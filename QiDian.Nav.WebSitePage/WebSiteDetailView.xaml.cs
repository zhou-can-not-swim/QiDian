using QiDian.Nav.WebSitePage.Models;
using QiDian.Nav.WebSitePage.ViewModels;
using ReactiveUI;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace QiDian.Nav.WebSitePage
{
    /// <summary>
    /// 抽屉子控件：接收父页面传入的 Site（依赖属性），展示网站详情并提供"打开/关闭"操作。
    /// 父传子数据流：父 XAML 用 Site="{Binding SelectedSite}" 把选中项传给本控件。
    /// </summary>
    public partial class WebSiteDetailView : UserControl,IViewFor<WebSiteDetailViewModel>
    {
        public WebSiteDetailView()
        {
            InitializeComponent();
        }

        /// <summary>父页面传入的网站数据</summary>
        public WebSiteItem? Site
        {
            get => (WebSiteItem?)GetValue(SiteProperty);
            set => SetValue(SiteProperty, value);
        }

        public static readonly DependencyProperty SiteProperty =
            DependencyProperty.Register(nameof(Site), typeof(WebSiteItem), typeof(WebSiteDetailView),
                new PropertyMetadata(null, (d, _) =>
                {
                    // 把控件自身的 DataContext 指向传入的数据，内部 XAML 即可直接 {Binding Name} 等
                    ((WebSiteDetailView)d).DataContext = ((WebSiteDetailView)d).Site;
                }));

        /// <summary>请求父页面收起抽屉（父负责滑出动画与布局）</summary>
        public event EventHandler? CloseRequested;

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            if (Site == null || string.IsNullOrWhiteSpace(Site.Url)) return;
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = Site.Url,
                    UseShellExecute = true // 交给系统默认浏览器打开
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开网站失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }


        #region ViewModel

        public WebSiteDetailViewModel? ViewModel
        {
            get => (WebSiteDetailViewModel?)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        object? IViewFor.ViewModel
        {
            get => ViewModel;
            set => ViewModel = (WebSiteDetailViewModel?)value;
        }

        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register("ViewModel", typeof(WebSiteDetailViewModel), typeof(WebSiteDetailView),
                new PropertyMetadata(null, (d, e) =>
                {
                    if (d is not WebSitePageView view) return;
                    view.DataContext = e.NewValue;

                }));

        #endregion
    }
}
