using QiDian.Common;
using QiDian.Data;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows.Controls;

namespace QiDian.Services;

/// <summary>
/// 导航服务 - 管理页面导航和 ViewModel 切换
/// </summary>
public interface INavigationService
{
    Frame? MainFrame { get; set; }
    ViewModelBase? CurrentViewModel { get; }
    void Navigate(Type viewType);
    void NavigateTo(string viewKey);
    Task NavigateToAsync(string viewKey);
    event EventHandler<ViewModelBase?>? CurrentViewModelChanged;
}

public class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, Type> _viewModelMap = new();
    private ViewModelBase? _currentViewModel;

    public Frame? MainFrame { get; set; }
    private readonly AppDbContext _db;

    public ViewModelBase? CurrentViewModel
    {
        get => _currentViewModel;
        private set
        {
            _currentViewModel = value;
            CurrentViewModelChanged?.Invoke(this, value);
        }
    }

    public event EventHandler<ViewModelBase?>? CurrentViewModelChanged;

    public NavigationService(IServiceProvider serviceProvider, AppDbContext db)
    {
        _db = db;
        _serviceProvider = serviceProvider;
        InitializeViewModelMap();
    }

    private void InitializeViewModelMap()
    {
        // 动态注册所有 ViewModel 类型
        _db.NavigationMenus.Where(m => m.IsEnabled).ToList().ForEach(m =>
        {
            var vmType = ViewModelRegistry.GetViewModelType(m.ViewKey);
            if (vmType != null)
                _viewModelMap[m.ViewKey] = vmType;
        });
    }

    public void Navigate(Type viewType)
    {
        if (MainFrame == null) return;

        var view = Activator.CreateInstance(viewType);
        if (view != null)
        {
            MainFrame.Navigate(view);
        }
    }

    public void NavigateTo(string viewKey)
    {
        NavigateToAsync(viewKey).ConfigureAwait(false);
    }

    public Task NavigateToAsync(string viewKey)
    {
        if (!_viewModelMap.TryGetValue(viewKey, out var vmType))
            return Task.CompletedTask;

        // 通过 DI Scope 解析 ViewModel（支持 Scoped 依赖如 AppDbContext）
        using var scope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var vm = scope.ServiceProvider.GetRequiredService(vmType) as ViewModelBase;
        CurrentViewModel = vm;

        return Task.CompletedTask;
    }

}