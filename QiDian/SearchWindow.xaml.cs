using Microsoft.Extensions.DependencyInjection;
using QiDian.Models;
using QiDian.Services;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace QiDian
{
    public partial class SearchWindow : Window,IViewFor<SearchViewModel>
    {
        private SearchViewModel _viewModel;
        private readonly IWindowSwitcherService _windowSwitcher;

        public SearchWindow(IWindowSwitcherService windowSwitcher)
        {
            InitializeComponent();
            // 初始化 ViewModel（同时赋给 ViewModel 依赖属性，触发 DataContext 自动配置）
            ViewModel = _viewModel = new SearchViewModel();

            this.WhenActivated(d =>
            {
                this.Bind(ViewModel, vm => vm.SearchKeyword, v => v.SearchTextBox.Text).DisposeWith(d);
                this.OneWayBind(ViewModel, vm => vm.Results, v => v.ResultsListBox.ItemsSource).DisposeWith(d);
                // 绑定选中项，彻底抛弃SelectionChanged后台事件
                this.Bind(ViewModel, vm => vm.SelectedFile, v => v.ResultsListBox.SelectedItem)
                    .DisposeWith(d);

                // 有结果就显示结果列表（搜索期间保留已有结果，避免列表闪烁）
                this.WhenAnyValue(x => x.ViewModel!.Results)
                    .Select(results => results?.Any() == true ? Visibility.Visible : Visibility.Collapsed)
                    .BindTo(this, x => x.ResultsListBox.Visibility)
                    .DisposeWith(d);

                // 仅「已搜索、无搜索在进行、且无结果」时显示空状态提示
                this.WhenAnyValue(x => x.ViewModel!.HasSearched, x => x.ViewModel!.IsSearching,
                        x => x.ViewModel!.Results,
                        (hasSearched, isSearching, results) =>
                            hasSearched && !isSearching && results?.Any() != true
                                ? Visibility.Visible : Visibility.Collapsed)
                    .BindTo(this, x => x.EmptyStateBorder.Visibility)
                    .DisposeWith(d);
            });



            // 使窗口可拖动
            MouseLeftButtonDown += (s, e) => DragMove();
            _windowSwitcher = windowSwitcher;
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            // 每次打开窗口都重置为「只显示搜索栏」的初始状态
            _viewModel.Reset();
            SearchTextBox.Focus();
            SearchTextBox.SelectAll();
        }

        private void SearchWindow_Deactivated(object sender, EventArgs e)
        {
            // 延迟隐藏，避免因点击窗口内部控件触发 Deactivated 导致误关
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!IsActive)
                {
                    Hide();
                    ShowInTaskbar = false;
                }
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void ResultsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 更新 ViewModel 的选中项
            if (ResultsListBox.SelectedItem is FileEntry selected)
            {
                _viewModel.SelectedFile = selected;
            }
        }

        private void ResultsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ResultsListBox.SelectedItem is FileEntry result)
            {
                _viewModel.OpenFileCommand.Execute().Subscribe();
                _windowSwitcher.HideOrShowMainWindow();
            }
        }

        /// <summary>
        /// 键盘导航：上下键选择结果，Enter 打开，Escape 关闭
        /// </summary>
        private void SearchTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Down:
                    // 向下移动选择
                    if (_viewModel.Results.Count() > 0)
                    {
                        int nextIndex = ResultsListBox.SelectedIndex + 1;
                        if (nextIndex < _viewModel.Results.Count())
                        {
                            ResultsListBox.SelectedIndex = nextIndex;
                            ResultsListBox.ScrollIntoView(ResultsListBox.SelectedItem);
                        }
                        e.Handled = true;
                    }
                    break;

                case Key.Up:
                    // 向上移动选择
                    if (_viewModel.Results.Count() > 0)
                    {
                        int prevIndex = ResultsListBox.SelectedIndex - 1;
                        if (prevIndex >= 0)
                        {
                            ResultsListBox.SelectedIndex = prevIndex;
                            ResultsListBox.ScrollIntoView(ResultsListBox.SelectedItem);
                        }
                        e.Handled = true;
                    }
                    break;

                case Key.Enter:
                    // 打开选中的文件
                    if (ResultsListBox.SelectedItem is FileEntry result)
                    {
                        _viewModel.OpenFileCommand.Execute().Subscribe();
                        _windowSwitcher.HideOrShowMainWindow();
                        e.Handled = true;
                    }
                    break;

                case Key.Escape:
                    // 关闭搜索窗口
                    Hide();
                    ShowInTaskbar = false;
                    e.Handled = true;
                    break;
            }
        }

        private void NavClick(object sender, RoutedEventArgs e)
        {
            // 使用 WindowSwitcherService 切换到 NavWindow
            var serviceProvider = AppServiceLocator.ServiceProvider;
            if (serviceProvider == null)
            {
                MessageBox.Show("服务容器尚未初始化");
                return;
            }

            var switcher = serviceProvider.GetRequiredService<IWindowSwitcherService>();
            switcher.SwitchToNavWindow();
        }

        #region ViewModel

        public SearchViewModel? ViewModel
        {
            get => (SearchViewModel?)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        object? IViewFor.ViewModel
        {
            get => ViewModel;
            set => ViewModel = (SearchViewModel?)value;
        }

        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register("ViewModel", typeof(SearchViewModel), typeof(SearchWindow),
                new PropertyMetadata(null, (d, e) =>
                {
                    if (d is SearchWindow view)
                        view.DataContext = e.NewValue;
                }));

        #endregion
    }
}