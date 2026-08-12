using QiDian.Common;
using QiDian.Contracts;
using QiDian.Services;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;

namespace QiDian.ViewModels
{
    /// <summary>
    /// NavWindow ViewModel (ReactiveUI 版本)
    /// 管理左侧导航列表 + 当前选中页切换
    /// 导航项来自插件模块（INavModule）元数据，自动加载。
    /// </summary>
    public class NavViewModel : ViewModelBase
    {
        private readonly NavigationService _navigation;
        public ObservableCollection<NavItem> NavItems { get; } = new();

        /// <summary>当前显示的子 ViewModel（绑定右侧内容区）</summary>
        private ViewModelBase? _currentPage;
        public ViewModelBase? CurrentPage
        {
            get => _currentPage;
            set => this.RaiseAndSetIfChanged(ref _currentPage, value);
        }

        private NavItem? _selectedNavItem;
        public NavItem? SelectedNavItem
        {
            get => _selectedNavItem;
            set => this.RaiseAndSetIfChanged(ref _selectedNavItem, value);
        }

        /// <summary>导航命令</summary>
        public ReactiveCommand<string, Unit> NavigateCommand { get; }

        public NavViewModel(NavigationService? navigation, IEnumerable<INavModule> modules)
        {
            _navigation = navigation;

            // 创建导航命令
            NavigateCommand = ReactiveCommand.CreateFromTask<string>(NavigateAsync);

            // 监听 NavigationService 变化，同步 CurrentPage
            _navigation.CurrentViewModelChanged += (_, vm) => CurrentPage = vm;

            // 监听 SelectedNavItem 变化，触发导航
            this.WhenAnyValue(x => x.SelectedNavItem)
                .Where(x => x != null)
                .Subscribe(item =>
                {
                    if (item != null)
                        NavigateCommand.Execute(item.ViewKey).Subscribe();
                });

            // 用插件模块元数据构建左侧导航列表（按 Order 排序、按 ViewKey 去重）
            foreach (var m in modules.OrderBy(x => x.Order))
            {
                if (NavItems.All(n => n.ViewKey != m.ViewKey))
                    NavItems.Add(new NavItem(m.ViewKey, m.Title, m.Icon, m.Order));
            }

            if (NavItems.Count > 0)
                SelectedNavItem = NavItems[0];
        }

        /// <summary>
        /// 按 ViewKey 导航
        /// </summary>
        private async Task NavigateAsync(string viewKey)
        {
            await _navigation.NavigateToAsync(viewKey);
        }
    }

    /// <summary>导航菜单显示包装类（来自插件模块元数据）</summary>
    public class NavItem
    {
        public string ViewKey { get; }
        public string Title { get; }
        public string Icon { get; }
        public int Order { get; }

        public NavItem(string viewKey, string title, string icon, int order)
        {
            ViewKey = viewKey;
            Title = title;
            Icon = icon;
            Order = order;
        }
    }
}
