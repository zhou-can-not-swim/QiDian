using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using QiDian.Models;
using QiDian.Services;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace QiDian
{
    public partial class SearchWindow : Window
    {
        private SearchViewModel _viewModel;
        private readonly IWindowSwitcherService _windowSwitcher;

        public SearchWindow(IWindowSwitcherService windowSwitcher)
        {
            InitializeComponent();
            // 初始化 ViewModel
            _viewModel = new SearchViewModel();
            ResultsListBox.ItemsSource = _viewModel.SearchResults;
            SearchTextBox.TextChanged += SearchTextBox_TextChanged;

            // 搜索结果变更时同步更新可见性
            _viewModel.SearchResults.CollectionChanged += (s, e) => UpdateResultsVisibility();

            // 使窗口可拖动
            MouseLeftButtonDown += (s, e) => DragMove();
            _windowSwitcher = windowSwitcher;
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            SearchTextBox.Focus();
            SearchTextBox.SelectAll();
        }

        /// <summary>
        /// 窗口失活（点击界面外部）时自动隐藏
        /// </summary>
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

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // 更新 ViewModel 的搜索关键词
            _viewModel.SearchKeyword = SearchTextBox.Text;

            // 根据搜索词和结果数量控制界面显示
            UpdateResultsVisibility();

            // 如果有结果，自动选择第一个
            if (_viewModel.SearchResults.Count > 0 && ResultsListBox.SelectedIndex == -1)
            {
                ResultsListBox.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// 控制结果列表和空状态的可见性
        /// </summary>
        private void UpdateResultsVisibility()
        {
            bool hasKeyword = !string.IsNullOrWhiteSpace(SearchTextBox.Text);

            if (!hasKeyword)
            {
                // 没有搜索词 → 隐藏所有结果区域
                ResultsListBox.Visibility = Visibility.Collapsed;
            }
            else if (_viewModel.SearchResults.Count > 0)
            {
                // 有搜索词且有结果 → 显示结果列表
                ResultsListBox.Visibility = Visibility.Visible;
            }
            else
            {
                // 有搜索词但无结果 → 显示空状态提示
                ResultsListBox.Visibility = Visibility.Collapsed;
            }
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
                    if (_viewModel.SearchResults.Count > 0)
                    {
                        int nextIndex = ResultsListBox.SelectedIndex + 1;
                        if (nextIndex < _viewModel.SearchResults.Count)
                        {
                            ResultsListBox.SelectedIndex = nextIndex;
                            ResultsListBox.ScrollIntoView(ResultsListBox.SelectedItem);
                        }
                        e.Handled = true;
                    }
                    break;

                case Key.Up:
                    // 向上移动选择
                    if (_viewModel.SearchResults.Count > 0)
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
    }
}