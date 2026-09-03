using QiDian.Nav.WebSitePage.ViewModels;
using ReactiveUI;
using System;
using System.Windows;
using System.Windows.Controls;

namespace QiDian.Nav.WebSitePage
{
    /// <summary>
    /// 抽屉子控件：父页面通过 ViewModel="{Binding Detail}" 把抽屉详情 VM（内含当前选中站点）传进来，
    /// 内部界面全部绑定该 VM —— 列表点谁，这里就展示谁。
    /// </summary>
    public partial class WebSiteDetailView : UserControl, IViewFor<WebSiteDetailViewModel>
    {
        public WebSiteDetailView()
        {
            InitializeComponent();
        }

        /// <summary>请求父页面收起抽屉（父负责滑出动画与布局）</summary>
        public event EventHandler? CloseRequested;

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
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
                    if (d is WebSiteDetailView view)
                        view.DataContext = e.NewValue; // 内部 XAML 直接 {Binding Site.Name} 等
                }));

        #endregion
    }
}
