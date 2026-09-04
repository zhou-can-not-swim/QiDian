using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;
using System.Windows.Controls;
using QiDian.Contracts;
using QiDian.Services;
using QiDian.ViewModels;
using Zhou.CrawlerAdapter.DependencyInjection;
using Zhou.LevelDB.DependencyInjection;
using QiDian.Services.SearchLogic;

namespace QiDian
{

    public partial class App : Application
    {
        private IHost _host = null!;

        // 托盘图标引用
        private TaskbarIcon _notifyIcon = null!;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 发现导航插件（在 IHost 构建前，以便把插件类型注册进 DI）
            var pluginsDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Navs");
            var modules = NavPluginLoader.LoadModules(pluginsDir);

            _host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    ConfigureServices(services);

                    //第三方自定义库
                    ConfigureThirdpartyServices(services);

                    // 注册插件：模块单例 + View/ViewModel 瞬态
                    foreach (var m in modules)
                    {
                        services.AddSingleton<INavModule>(m);
                        services.AddTransient(m.ViewType);
                        services.AddTransient(m.ViewModelType);
                    }
                })
                .Build();
            AppServiceLocator.Initialize(_host.Services);
            _ = Task.Run(UnionSearchService.InitStartMenuFiles);

            // 把插件页面写入静态注册表（ViewKey → View/ViewModel 映射）
            foreach (var m in modules)
                ViewModelRegistry.Register(m.ViewKey, m.ViewType, m.ViewModelType);

            await _host.StartAsync();

            InitializeTrayIcon();
            var mainWindow = _host.Services.GetRequiredService<SearchWindow>();
            //var mainWindow = _host.Services.GetRequiredService<NavWindow>();
            mainWindow.Show();

            // 初始化全局热键服务（切换窗口 / 隐藏窗口）
            var hotkeyService = _host.Services.GetRequiredService<IGlobalHotkeyService>();
            hotkeyService.Initialize(mainWindow);

            DispatcherUnhandledException += (s, args) =>
            {
                MessageBox.Show($"发生错误: {args.Exception.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                args.Handled = true;
            };
        }

        #region 托盘方法
        private void InitializeTrayIcon()
        {
            var iconPath = System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "Resources", "tray_icon.ico");

            _notifyIcon = new TaskbarIcon
            {
                Icon = new System.Drawing.Icon(iconPath),
                ToolTipText = "QiDian",
                MenuActivation = PopupActivationMode.LeftOrRightClick
            };

            var contextMenu = new ContextMenu();
            var showMenuItem = new MenuItem { Header = "显示窗口" };
            showMenuItem.Click += ShowMainWindow;  // 匹配正确签名
            var exitMenuItem = new MenuItem { Header = "退出" };
            exitMenuItem.Click += ExitApplication;  // 匹配正确签名

            contextMenu.Items.Add(showMenuItem);
            contextMenu.Items.Add(new Separator());
            contextMenu.Items.Add(exitMenuItem);
            _notifyIcon.ContextMenu = contextMenu;
            _notifyIcon.TrayMouseDoubleClick += ShowMainWindow;
        }

        public void ShowMainWindow(object sender, RoutedEventArgs e)
        {
            var mainWindow = _host.Services.GetRequiredService<SearchWindow>();

            if (mainWindow != null)
            {
                mainWindow.ShowInTaskbar = true;
                mainWindow.Show();
                if (mainWindow.WindowState == WindowState.Minimized)
                {
                    mainWindow.WindowState = WindowState.Normal;
                }
                mainWindow.Activate();
                mainWindow.Topmost = true;
                mainWindow.Topmost = false;
            }
        }

        public void MinimizeToTray()
        {
            // 隐藏所有窗口
            var searchWindow = _host.Services.GetRequiredService<SearchWindow>();
            if (searchWindow != null && searchWindow.IsVisible)
            {
                searchWindow.ShowInTaskbar = false;
                searchWindow.Hide();
            }

            var navWindow = _host.Services.GetRequiredService<NavWindow>();
            if (navWindow != null && navWindow.IsVisible)
            {
                navWindow.ShowInTaskbar = false;
                navWindow.Hide();
            }
        }

        public async void ExitApplication(object sender, RoutedEventArgs e)
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Dispose();
            }

            await _host.StopAsync();
            _host.Dispose();
            Shutdown();
        }
        #endregion

        private static void ConfigureServices(IServiceCollection services)
        {
            // --- 框架服务 ---
            services.AddSingleton<NavigationService>();
            services.AddSingleton<IWindowSwitcherService, WindowSwitcherService>();
            services.AddSingleton<IGlobalHotkeyService, GlobalHotkeyService>();
            services.AddSingleton<EverythingSearchService>();

            // --- 窗口 ---
            services.AddSingleton<NavWindow>();
            services.AddSingleton<SearchWindow>();

            // --- ViewModels（Transient）---
            services.AddTransient<NavViewModel>();
        }

        // --- 第三方自定义库 ---
        private static void ConfigureThirdpartyServices(IServiceCollection services)
        {
            services.AddCrawlerAdapter(options =>
            {
                options.Headless = true;
            });

            services.AddLevelDB(options =>
            {
                options.UseAppData = true;
                options.DirName = "QiDian";
                options.DatabaseName = "qidian";
            });
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            var hotkeyService = _host.Services.GetService<IGlobalHotkeyService>();
            hotkeyService?.UnregisterAll();

            await _host.StopAsync();
            _host.Dispose();
            base.OnExit(e);
        }

    }

}