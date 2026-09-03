using QiDian.Nav.WebSitePage.ViewModels;
using ReactiveUI;
using System;
using System.Windows;
using System.Windows.Controls;

namespace QiDian.Nav.WebSitePage
{

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
                        view.DataContext = e.NewValue; 
                }));

        #endregion
    }
}
