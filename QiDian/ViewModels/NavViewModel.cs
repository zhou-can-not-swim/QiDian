using QiDian.Common;
using QiDian.Data;
using QiDian.Services;
using Microsoft.EntityFrameworkCore;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows;

namespace QiDian.ViewModels
{
    /// <summary>
    /// NavWindow ViewModel (ReactiveUI 版本)
    /// 管理左侧导航列表 + 当前选中页切换
    /// </summary>
    public class NavViewModel : ViewModelBase
    {
        private readonly NavigationService _navigation;
        private readonly AppDbContext _db;
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

        public NavViewModel(NavigationService? navigation, AppDbContext db)
        {
            _navigation = navigation;
            _db = db;

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

            InitializeAsync().ConfigureAwait(true);
        }

        /// <summary>
        /// 按 ViewKey 导航
        /// </summary>
        private async Task NavigateAsync(string viewKey)
        {
            await _navigation.NavigateToAsync(viewKey);
        }


        public async Task InitializeAsync()
        {
            var menus = await _db.NavigationMenus
                .Where(m => m.IsEnabled)
                .OrderBy(m => m.Order)
                .ToListAsync();

                NavItems.Clear();
                foreach (var m in menus)
                    NavItems.Add(new NavItem(m));

                if (NavItems.Count > 0)
                    SelectedNavItem = NavItems[0];

        }
    }

    /// <summary>导航菜单显示包装类</summary>
    public class NavItem
    {
        public int Id { get; }
        public string Title { get; }
        public string ViewKey { get; }
        public string Icon { get; }
        public int Order { get; }

        public NavItem(NavigationMenu menu)
        {
            Id = menu.Id;
            Title = menu.Title;
            ViewKey = menu.ViewKey;
            Icon = menu.Icon;
            Order = menu.Order;
        }
    }
}