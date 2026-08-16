using System.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace QiDian.Services
{
    public interface IWindowSwitcherService
    {
        void SwitchToNavWindow();
        void SwitchToSearchWindow();
        void SwitchToPluginWindow();
        void ToggleWindows();
        void TogglePluginWindow();
        void HideOrShowMainWindow();
        void HideAllWindows();
    }

    public class WindowSwitcherService : IWindowSwitcherService
    {
        private readonly IServiceProvider _serviceProvider;

        // 缓存窗口实例
        private SearchWindow? _searchWindow;
        private NavWindow? _navWindow;
        private PluginWindow? _pluginWindow;
        private PopupHost? _popupHost;

        public WindowSwitcherService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // 延迟获取窗口实例：第一次真正使用这个窗口时，才会从依赖注入容器中去创建/获取实例
        private SearchWindow SearchWindow => _searchWindow ??= _serviceProvider.GetRequiredService<SearchWindow>();
        private NavWindow NavWindow => _navWindow ??= _serviceProvider.GetRequiredService<NavWindow>();
        private PluginWindow PluginWindow => _pluginWindow ??= _serviceProvider.GetRequiredService<PluginWindow>();
        private PopupHost PopupHost => _popupHost ??= _serviceProvider.GetRequiredService<PopupHost>();

        /// <summary>
        /// 切换到 NavWindow（副窗口）
        /// </summary>
        public void SwitchToNavWindow()
        {
            HideSearch();
            HidePlugin();

            NavWindow.ShowInTaskbar = true;
            NavWindow.Show();
            if (NavWindow.WindowState == WindowState.Minimized)
            {
                NavWindow.WindowState = WindowState.Normal;
            }
            NavWindow.Activate();
        }

        /// <summary>
        /// 切换到 SearchWindow（主窗口）
        /// </summary>
        public void SwitchToSearchWindow()
        {
            HideNav();
            HidePlugin();

            SearchWindow.ShowInTaskbar = true;
            SearchWindow.Show();
            if (SearchWindow.WindowState == WindowState.Minimized)
            {
                SearchWindow.WindowState = WindowState.Normal;
            }
            SearchWindow.Activate();
        }

        /// <summary>
        /// 切换到 PluginWindow（内置服务窗口）
        /// </summary>
        public void SwitchToPluginWindow()
        {
            HideSearch();
            HideNav();

            PluginWindow.ShowInTaskbar = true;
            PluginWindow.Show();
            if (PluginWindow.WindowState == WindowState.Minimized)
            {
                PluginWindow.WindowState = WindowState.Normal;
            }
            PluginWindow.Activate();
        }

        /// <summary>
        /// 在两个窗口之间切换（SearchWindow / NavWindow）
        /// </summary>
        public void ToggleWindows()
        {
            if (SearchWindow.IsVisible)
            {
                SwitchToNavWindow();
            }
            else
            {
                SwitchToSearchWindow();
            }
        }

        /// <summary>
        /// 插件热键：在 SearchWindow 与 PluginWindow 之间切换（回到 Search 的入口）
        /// </summary>
        public void TogglePluginWindow()
        {
            if (PluginWindow.IsVisible)
            {
                SwitchToSearchWindow();
            }
            else
            {
                SwitchToPluginWindow();
            }
        }

        /// <summary>
        /// HideHotKey：隐藏当前所有窗口，再次触发显示 SearchWindow（主窗口）
        /// </summary>
        public void HideOrShowMainWindow()
        {
            // 检查是否有任何窗口可见
            bool anyVisible = SearchWindow.IsVisible || NavWindow.IsVisible || PluginWindow.IsVisible;

            if (anyVisible)
            {
                // 有窗口可见 -> 隐藏所有窗口
                HideAllWindows();
            }
            else
            {
                // 没有窗口可见 -> 显示 SearchWindow（主窗口）
                SearchWindow.ShowInTaskbar = true;
                SearchWindow.Show();
                if (SearchWindow.WindowState == WindowState.Minimized)
                {
                    SearchWindow.WindowState = WindowState.Normal;
                }
                SearchWindow.Activate();
            }
        }

        /// <summary>隐藏所有窗口（托盘收起时用）</summary>
        public void HideAllWindows()
        {
            HideSearch();
            HideNav();
            HidePlugin();
        }

        #region 私有辅助：隐藏单个窗口（含弹窗）

        private void HideSearch()
        {
            if (SearchWindow.IsVisible)
            {
                SearchWindow.Hide();
                SearchWindow.ShowInTaskbar = false;
            }
            HidePopup();
        }

        private void HideNav()
        {
            if (NavWindow.IsVisible)
            {
                NavWindow.Hide();
                NavWindow.ShowInTaskbar = false;
            }
            HidePopup();
        }

        private void HidePlugin()
        {
            if (PluginWindow.IsVisible)
            {
                PluginWindow.Hide();
                PluginWindow.ShowInTaskbar = false;
            }
            HidePopup();
        }

        /// <summary>弹窗是瞬态结果面，窗口切换时一并收起</summary>
        private void HidePopup()
        {
            if (PopupHost.IsVisible)
            {
                PopupHost.Hide();
            }
        }

        #endregion
    }
}
