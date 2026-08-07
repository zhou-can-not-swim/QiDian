using System.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace QiDian.Services
{
    public interface IWindowSwitcherService
    {
        void SwitchToNavWindow();
        void SwitchToSearchWindow();
        void ToggleWindows();
        void HideOrShowMainWindow();
    }

    public class WindowSwitcherService : IWindowSwitcherService
    {
        private readonly IServiceProvider _serviceProvider;

        // 缓存窗口实例
        private SearchWindow? _searchWindow;
        private NavWindow? _navWindow;

        public WindowSwitcherService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // 延迟获取窗口实例：第一次真正使用这个窗口时，才会从依赖注入容器中去创建/获取实例
        private SearchWindow SearchWindow => _searchWindow ??= _serviceProvider.GetRequiredService<SearchWindow>();
        private NavWindow NavWindow => _navWindow ??= _serviceProvider.GetRequiredService<NavWindow>();

        /// <summary>
        /// 切换到 NavWindow（副窗口）
        /// </summary>
        public void SwitchToNavWindow()
        {
            if (SearchWindow.IsVisible)
            {
                SearchWindow.Hide();
                SearchWindow.ShowInTaskbar = false;
            }

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
            if (NavWindow.IsVisible)
            {
                NavWindow.Hide();
                NavWindow.ShowInTaskbar = false;
            }

            SearchWindow.ShowInTaskbar = true;
            SearchWindow.Show();
            if (SearchWindow.WindowState == WindowState.Minimized)
            {
                SearchWindow.WindowState = WindowState.Normal;
            }
            SearchWindow.Activate();
        }

        /// <summary>
        /// 在两个窗口之间切换
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
        /// HideHotKey：隐藏当前窗口，再次触发显示 SearchWindow（主窗口）
        /// </summary>
        public void HideOrShowMainWindow()
        {
            // 检查是否有任何窗口可见
            bool anyVisible = SearchWindow.IsVisible || NavWindow.IsVisible;

            if (anyVisible)
            {
                // 有窗口可见 -> 隐藏所有窗口
                if (SearchWindow.IsVisible)
                {
                    SearchWindow.Hide();
                    SearchWindow.ShowInTaskbar = false;
                }
                if (NavWindow.IsVisible)
                {
                    NavWindow.Hide();
                    NavWindow.ShowInTaskbar = false;
                }
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
    }
}