using QiDian.Contracts;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace QiDian.Services;

/// <summary>
/// 导航服务 - 管理页面导航和 ViewModel 切换
/// </summary>
public interface INavigationService
{
    ViewModelBase? CurrentViewModel { get; }
    Task NavigateTo(string viewKey);
    Task NavigateToAsync(string viewKey);
    event EventHandler<ViewModelBase?>? CurrentViewModelChanged;
}

public class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private ViewModelBase? _currentViewModel;

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

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task NavigateTo(string viewKey)
    {
       await NavigateToAsync(viewKey);
    }

    public Task NavigateToAsync(string viewKey)
    {
        var vmType = ViewModelRegistry.GetViewModelType(viewKey);
        if (vmType == null)
            return Task.CompletedTask;

        // 通过 DI Scope 解析 ViewModel
        using var scope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var vm = scope.ServiceProvider.GetRequiredService(vmType) as ViewModelBase;
        CurrentViewModel = vm;

        return Task.CompletedTask;
    }
}
