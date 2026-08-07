using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;
using System.Windows.Controls;
using QiDian.Data;
using QiDian.Services;
using QiDian.ViewModels;
using QiDian.Views;

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

            _host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    ConfigureServices(services);
                })
                .Build();

            AppServiceLocator.Initialize(_host.Services);

            // 注册 View ↔ ViewModel 映射
            RegisterViewModels();

            // 初始化数据库
            using (var scope = _host.Services.CreateScope())
            {
                var initializer = scope.ServiceProvider.GetRequiredService<DbInitializer>();
                await initializer.InitializeAsync();
            }

            await _host.StartAsync();


            InitializeTrayIcon();
            var mainWindow = _host.Services.GetRequiredService<SearchWindow>();
            mainWindow.Show();

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
            var mainWindow = _host.Services.GetRequiredService<NavWindow>();

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
            // --- EF Core SQLite ---
            var dbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "QiDian.db");
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite($"Data Source={dbPath}"),
                ServiceLifetime.Scoped);

            services.AddScoped<DbInitializer>();

            // --- 框架服务 ---
            services.AddSingleton<NavigationService>();
            services.AddSingleton<IWindowSwitcherService, WindowSwitcherService>();

            // --- 窗口 ---
            services.AddSingleton<NavWindow>();
            services.AddSingleton<SearchWindow>();

            // --- ViewModels（Transient）---
            services.AddTransient<NavViewModel>();
            services.AddTransient<HomeViewModel>();

            // --- Views（Transient）---
            services.AddTransient<HomeView>();
        }

        private static void RegisterViewModels()
        {
            ViewModelRegistry.Register<HomeView, HomeViewModel>("Home");
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            await _host.StopAsync();
            _host.Dispose();
            base.OnExit(e);
        }

    }

}