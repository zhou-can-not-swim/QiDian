using QiDian.Contracts;
using QiDian.Nav.DM.Model.Dtos;
using QiDian.Nav.DM.Model.Entities;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System.Collections.ObjectModel;
using System.Reactive;

namespace QiDian.Nav.DM.ViewModels;

/// <summary>
/// 番剧搜索页。搜索逻辑在 <see cref="SearchAsync"/> 的 TODO 处接入爬虫；
/// 搜索结果显示在 <see cref="Results"/>，点击结果进入详情页。
/// </summary>
public class DMSearchViewModel : ViewModelBase
{
    private readonly DMNavigator _navigator;

    /// <summary>搜索关键字（搜索框双向绑定）</summary>
    [Reactive]
    public string Keyword { get; set; } = "";

    /// <summary>是否正在搜索（界面可显示转圈/禁用按钮）</summary>
    [Reactive]
    public bool IsSearching { get; set; }

    /// <summary>是否已经搜索过（用于空结果时显示"未找到"提示）</summary>
    [Reactive]
    public bool HasSearched { get; set; }

    /// <summary>搜索结果</summary>
    public ObservableCollection<AnimeItem> Results { get; } = new();

    /// <summary>点击搜索结果 → 进入详情页</summary>
    public ReactiveCommand<AnimeItem, Unit> OpenAnimeCommand { get; }

    public DMSearchViewModel(DMNavigator navigator)
    {
        _navigator = navigator;
        OpenAnimeCommand = ReactiveCommand.Create<AnimeItem>(OpenAnime);
    }

    /// <summary>
    /// 执行搜索。
    /// TODO: 在这里接入爬虫搜索接口 —— 把结果 Add 进 <see cref="Results"/>，
    /// 每个结果的 Id 用来源页 URL，Cover / UpdateInfo 按搜索结果填充（详情页会再爬完整集数）。
    /// </summary>
    public async Task SearchAsync()
    {
        var keyword = Keyword?.Trim();
        if (string.IsNullOrWhiteSpace(keyword)) return;

        IsSearching = true;
        HasSearched = true;
        Results.Clear();

        try
        {
            // TODO: 接入爬虫搜索。示例结构：
            // var list = await _searchService.SearchAsync(keyword);
            // foreach (var item in list) Results.Add(item);
            await Task.Delay(100); // 仅演示等待，接爬虫后删除
        }
        finally
        {
            IsSearching = false;
        }
    }

    /// <summary>返回首页</summary>
    public void Back() => _navigator.GoHome();

    /// <summary>点击某条搜索结果 → 进入详情页</summary>
    public void OpenAnime(AnimeItem anime) => _navigator.OpenAnime(anime);
}
