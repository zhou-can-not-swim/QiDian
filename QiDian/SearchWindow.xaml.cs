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
                this.OneWayBind(ViewModel, vm => vm.RecentItems, v => v.RecentListBox.ItemsSource).DisposeWith(d);
                this.Bind(ViewModel, vm => vm.SelectedFile, v => v.RecentListBox.SelectedItem)
                    .DisposeWith(d);
            });

            // 使窗口可拖动（点击按钮等交互元素时不触发，避免误拖）
            MouseLeftButtonDown += (s, e) =>
            {
                if (e.OriginalSource is Button) return;
                DragMove();
            };
            _windowSwitcher = windowSwitcher;
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            // 每次打开窗口都重置为初始状态（两层默认折叠，保留用户上次的折叠/展开选择）
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

        // ============ 两层折叠/展开 ============

        private void RecentHeader_Click(object sender, RoutedEventArgs e)
        {
            // 切换展开/折叠，折叠时只显示第一行数据（由 ViewModel 控制数据量）
            _viewModel.ToggleRecentExpanded();
        }

        private void FixedHeader_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.IsFixedExpanded = !_viewModel.IsFixedExpanded;
        }

        private void RecentListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 更新 ViewModel 的选中项（第一层）
            if (RecentListBox.SelectedItem is FileEntry selected)
            {
                _viewModel.SelectedFile = selected;
            }
        }

        private void RecentListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (RecentListBox.SelectedItem is FileEntry)
            {
                // TODO(真实逻辑)：接入真实打开动作
                _viewModel.OpenSelectedFileMock();
            }
        }

        /// <summary>
        /// 键盘导航：上下/左右键选择第一层结果，Enter 打开（当前为模拟），Escape 关闭
        /// </summary>
        private void SearchTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Down:
                case Key.Right:
                    // 向下/向右移动选择
                    if (_viewModel.RecentItems.Count > 0)
                    {
                        int nextIndex = RecentListBox.SelectedIndex + 1;
                        if (nextIndex < _viewModel.RecentItems.Count)
                        {
                            RecentListBox.SelectedIndex = nextIndex;
                            RecentListBox.ScrollIntoView(RecentListBox.SelectedItem);
                        }
                        e.Handled = true;
                    }
                    break;

                case Key.Up:
                case Key.Left:
                    // 向上/向左移动选择
                    if (_viewModel.RecentItems.Count > 0)
                    {
                        int prevIndex = RecentListBox.SelectedIndex - 1;
                        if (prevIndex >= 0)
                        {
                            RecentListBox.SelectedIndex = prevIndex;
                            RecentListBox.ScrollIntoView(RecentListBox.SelectedItem);
                        }
                        e.Handled = true;
                    }
                    break;

                case Key.Enter:
                    // 打开选中的文件（当前为模拟打开）
                    if (RecentListBox.SelectedItem is FileEntry)
                    {
                        _viewModel.OpenSelectedFileMock();
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
