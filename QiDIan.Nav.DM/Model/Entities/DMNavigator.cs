using QiDian.Contracts;
using QiDian.Nav.DM.Model.Dtos;
using QiDian.Nav.DM.ViewModels;

namespace QiDian.Nav.DM.Model.Entities;

/// <summary>
/// DM 插件内部的页面导航器。因为主程序的导航粒度只到"DM 这个导航页"，
/// DM 页内部再跳转（首页 → 搜索 → 详情 → 播放）就由它统一调度：
/// 宿主 <see cref="DMViewModel"/> 只负责展示 <see cref="CurrentPage"/>，
/// 各子页面通过本类发起跳转，不需要互相引用。
/// </summary>
public sealed class DMNavigator
{
    private readonly DMViewModel _host;

    // 首页缓存在这：从别的页面返回首页时回到同一个实例，保留滚动位置和已加载数据
    private DMHomeViewModel? _home;

    // 详情页按番剧 Id 缓存：从播放页返回时回到同一个详情实例，不重新加载
    private readonly Dictionary<string, DMDetailViewModel> _detailCache = new();

    public DMNavigator(DMViewModel host)
    {
        _host = host;
    }

    /// <summary>去首页（首个使用，之后复用同一实例）</summary>
    public void GoHome()
    {
        _home ??= new DMHomeViewModel(this);
        NavigateTo(_home);
    }

    /// <summary>打开搜索页</summary>
    public void OpenSearch() => NavigateTo(new DMSearchViewModel(this));

    /// <summary>打开某部番剧的详情页（同一部番剧复用同一实例）</summary>
    public void OpenAnime(AnimeItem anime)
    {
        if (anime == null) return;

        if (!_detailCache.TryGetValue(anime.Id, out var vm))
            _detailCache[anime.Id] = vm = new DMDetailViewModel(this, anime);

        NavigateTo(vm);
    }

    /// <summary>直接播放某部番剧的某一集（跳到全屏播放页）</summary>
    public void OpenPlayer(AnimeItem anime, EpisodeItem episode)
    {
        if (anime == null || episode == null) return;
        NavigateTo(new DMPlayerViewModel(this, anime, episode));
    }

    /// <summary>切换到指定子页面（由宿主展示）</summary>
    public void NavigateTo(ViewModelBase page) => _host.ShowPage(page);
}
