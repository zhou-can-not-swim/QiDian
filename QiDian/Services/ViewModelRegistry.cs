using System.Windows;

namespace QiDian.Services;


public static class ViewModelRegistry
{
    #region 初始化就有
    // ViewKey → ViewModel 类型
    private static readonly Dictionary<string, Type> _vmTypes = [];

    // ViewKey → View 类型
    private static readonly Dictionary<string, Type> _viewTypes = [];

    /// <summary>
    /// 注册一组 View ↔ ViewModel 映射
    /// </summary>
    public static void Register<TView, TViewModel>(string viewKey)
        where TView : FrameworkElement
        where TViewModel : Common.ViewModelBase
    {
        _vmTypes[viewKey] = typeof(TViewModel);
        _viewTypes[viewKey] = typeof(TView);
    }
    #endregion

    public static Type? GetViewModelType(string viewKey)
        => _vmTypes.TryGetValue(viewKey, out var t) ? t : null;

    public static Type? GetViewType(string viewKey)
        => _viewTypes.TryGetValue(viewKey, out var t) ? t : null;

    public static IReadOnlyCollection<string> GetAllKeys() => _vmTypes.Keys.ToList().AsReadOnly();
}