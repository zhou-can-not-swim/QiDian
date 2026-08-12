namespace QiDian.Contracts;

/// <summary>
/// 导航插件契约：每个导航页面由一个独立的类库（DLL）实现此接口。
/// 主程序启动时扫描 Navs 文件夹，发现并加载所有实现，自动生成左侧导航菜单。
/// </summary>
public interface INavModule
{
    /// <summary>唯一标识（用于导航 / ViewModelRegistry 映射）</summary>
    string ViewKey { get; }

    /// <summary>左侧导航标题</summary>
    string Title { get; }

    /// <summary>左侧导航图标（Segoe MDL2 Assets 字形，如 ""）</summary>
    string Icon { get; }

    /// <summary>排序（越小越靠前）</summary>
    int Order { get; }

    /// <summary>页面 View 类型（UserControl）</summary>
    Type ViewType { get; }

    /// <summary>页面 ViewModel 类型</summary>
    Type ViewModelType { get; }
}
