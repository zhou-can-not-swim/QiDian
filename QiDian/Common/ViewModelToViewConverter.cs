using QiDian.Services;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using System.Globalization;
using System.Windows.Data;

namespace QiDian.Common
{
    /// <summary>
    /// ViewModel → View 转换器
    /// 绑定在 MainWindow ContentControl 上，自动将 CurrentPage(ViewModel) 渲染为对应 View。
    /// View 通过 DI 解析（无参构造），ViewModel 通过 IViewFor 接口赋值。
    /// </summary>
    public class ViewModelToViewConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not ViewModelBase vm) return null;

            var sp = AppServiceLocator.ServiceProvider;
            if (sp == null) return null;

            // 从 Registry 查找 View 类型
            Type? viewType = null;
            foreach (var key in ViewModelRegistry.GetAllKeys())
            {
                if (ViewModelRegistry.GetViewModelType(key) == vm.GetType())
                {
                    viewType = ViewModelRegistry.GetViewType(key);
                    break;
                }
            }

            if (viewType == null) return null;

            // DI Scope 解析 View -->创建view
            var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
            var scope = scopeFactory.CreateScope();
            var view = scope.ServiceProvider.GetRequiredService(viewType);

            // 这个界面的 ViewModel就是我找到的vm,
            if (view is IViewFor viewFor)
                viewFor.ViewModel = vm;

            return view;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}