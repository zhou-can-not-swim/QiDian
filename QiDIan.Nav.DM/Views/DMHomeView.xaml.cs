using QiDian.Nav.DM.ViewModels;
using ReactiveUI;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace QiDian.Nav.DM.Views;

public partial class DMHomeView : UserControl, IViewFor<DMHomeViewModel>
{
    public DMHomeView()
    {
        InitializeComponent();
    }

    /// <summary>点击顶部搜索栏 → 进入搜索页</summary>
    private void SearchBar_Click(object sender, RoutedEventArgs e)
        => ViewModel?.OpenSearch();

    /// <summary>
    /// 横向栏目上滚动鼠标滚轮：只有该行确实有横向溢出时才转成横向滑动；
    /// 否则不拦截，放行给外层页面做纵向滚动——否则卡片行没溢出时滚轮会"失灵"。
    /// </summary>
    private void HorizontalScroll_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is not ScrollViewer scroller || scroller.ScrollableWidth <= 0) return;

        scroller.ScrollToHorizontalOffset(scroller.HorizontalOffset - e.Delta);
        e.Handled = true;
    }

    #region ViewModel

    public DMHomeViewModel? ViewModel
    {
        get => (DMHomeViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    object? IViewFor.ViewModel
    {
        get => ViewModel;
        set => ViewModel = (DMHomeViewModel?)value;
    }

    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register("ViewModel", typeof(DMHomeViewModel), typeof(DMHomeView),
            new PropertyMetadata(null, (d, e) =>
            {
                if (d is DMHomeView view)
                    view.DataContext = e.NewValue;
            }));

    #endregion
}
