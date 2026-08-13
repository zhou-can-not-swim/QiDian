using QiDian.Nav.DM.ViewModels;
using ReactiveUI;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace QiDian.Nav.DM.Views;

public partial class DMSearchView : UserControl, IViewFor<DMSearchViewModel>
{
    public DMSearchView()
    {
        InitializeComponent();

        Loaded += (_, _) =>
        {
            SearchTextBox.Focus();
            SearchTextBox.SelectAll();
        };

        // 有结果 → 显示结果；否则显示提示（未搜索 / 无结果时文案不同）
        this.WhenAnyValue(x => x.ViewModel!.Results, x => x.ViewModel!.IsSearching,
                (results, searching) =>
                {
                    if (searching) return Visibility.Collapsed;
                    if (results?.Count > 0) return Visibility.Collapsed;
                    return Visibility.Visible;
                })
            .BindTo(this, x => x.EmptyState.Visibility);

        this.WhenAnyValue(x => x.ViewModel!.HasSearched)
            .Select(searched => searched ? "未找到相关番剧，换个关键词试试" : "输入番剧名称开始搜索")
            .BindTo(this, x => x.EmptyStateText.Text);
    }

    private void Search_Click(object sender, RoutedEventArgs e) => RunSearch();

    private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            RunSearch();
            e.Handled = true;
        }
    }

    private async void RunSearch()
    {
        if (ViewModel != null)
            await ViewModel.SearchAsync();
    }

    private void Back_Click(object sender, RoutedEventArgs e) => ViewModel?.Back();

    #region ViewModel

    public DMSearchViewModel? ViewModel
    {
        get => (DMSearchViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    object? IViewFor.ViewModel
    {
        get => ViewModel;
        set => ViewModel = (DMSearchViewModel?)value;
    }

    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register("ViewModel", typeof(DMSearchViewModel), typeof(DMSearchView),
            new PropertyMetadata(null, (d, e) =>
            {
                if (d is DMSearchView view)
                    view.DataContext = e.NewValue;
            }));

    #endregion
}
