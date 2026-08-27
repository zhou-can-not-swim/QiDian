using QiDian.Contracts;
using QiDian.Nav.DM.Model.Dtos;
using QiDian.Nav.DM.Model.Entities;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Reactive;

namespace QiDian.Nav.DM.ViewModels;

/// <summary>
/// 番剧详情页：封面 + 简介 + 选集。点某一集进入全屏播放页。
/// 详情数据（完整简介、选集列表）在 <see cref="LoadDetail"/> 的 TODO 处用 <see cref="Anime.Id"/> 爬取。
/// </summary>
public class DMDetailViewModel : ViewModelBase
{
    private readonly DMNavigator _navigator;

    /// <summary>当前这部番剧（与首页卡片 / 播放页抽屉是同一实例）</summary>
    public AnimeItem Anime { get; }

    /// <summary>选集列表（与播放页抽屉共用）</summary>
    public ObservableCollection<EpisodeItem> Episodes => Anime.Episodes;

    /// <summary>点某集 → 进入播放页</summary>
    public ReactiveCommand<EpisodeItem, Unit> PlayEpisodeCommand { get; }

    public DMDetailViewModel(DMNavigator navigator, AnimeItem anime)
    {
        _navigator = navigator;
        Anime = anime;
        PlayEpisodeCommand = ReactiveCommand.Create<EpisodeItem>(PlayEpisode);
        LoadDetail();
    }

    /// <summary>
    /// 加载详情：完整简介 + 选集列表。
    /// TODO: 用 Anime.Id（爬虫来源页 URL）抓取详情页，填充 Anime.Description 和 Anime.Episodes。
    /// 注意：Anime.Episodes 是 ObservableCollection，直接 Add 即可，界面会自动刷新。
    /// </summary>
    private void LoadDetail()
    {
        // TODO: 接入爬虫后在此填充详情。
        // 示例：var detail = await _service.GetDetailAsync(Anime.Id);
        //       Anime.Description = detail.Description;
        //       foreach (var ep in detail.Episodes) Anime.Episodes.Add(ep);
    }

    /// <summary>点某集 → 进入全屏播放页</summary>
    public void PlayEpisode(EpisodeItem episode) => _navigator.OpenPlayer(Anime, episode);

    /// <summary>返回首页</summary>
    public void Back() => _navigator.GoHome();
}
