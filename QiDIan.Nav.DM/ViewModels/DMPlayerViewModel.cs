using QiDian.Contracts;
using QiDian.Nav.DM.Model.Dtos;
using QiDian.Nav.DM.Model.Entities;
using ReactiveUI.Fody.Helpers;
using System.Collections.ObjectModel;

namespace QiDian.Nav.DM.ViewModels;

/// <summary>
/// 全屏播放页：占满整个导航内容区，右下角抽屉里列出全部剧集可自由切换。
/// 播放进度按 <see cref="EpisodeItem.Source"/> 记录，下次打开自动续播（逻辑在 View 里）。
/// </summary>
public class DMPlayerViewModel : ViewModelBase
{
    private readonly DMNavigator _navigator;

    /// <summary>当前这部番剧（抽屉列表来自它的选集）</summary>
    public AnimeItem Anime { get; }

    /// <summary>全部剧集（与详情页共用同一份列表）</summary>
    public ObservableCollection<EpisodeItem> Episodes => Anime.Episodes;

    /// <summary>当前播放的剧集。抽屉里选中的就是它，改变即切换播放</summary>
    [Reactive]
    public EpisodeItem? CurrentEpisode { get; set; }

    /// <summary>当前播放时间 / 总时长</summary>
    [Reactive]
    public string PositionText { get; set; } = "00:00 / 00:00";

    /// <summary>底部状态提示（加载中 / 自动续播 / 播放失败等）</summary>
    [Reactive]
    public string StatusMessage { get; set; } = "正在加载…";

    public DMPlayerViewModel(DMNavigator navigator, AnimeItem anime, EpisodeItem episode)
    {
        _navigator = navigator;
        Anime = anime;
        CurrentEpisode = episode;
    }

    /// <summary>返回番剧详情页（回到同一个详情实例）</summary>
    public void Back() => _navigator.OpenAnime(Anime);

    /// <summary>刷新某集列表文案（供 View 保存进度后调用）</summary>
    public void RefreshProgressText(EpisodeItem? episode)
    {
        if (episode == null || string.IsNullOrWhiteSpace(episode.Source)) return;

        var saved = PlaybackPositionStore.GetPositionSeconds(episode.Source);
        var dur = PlaybackPositionStore.GetDurationSeconds(episode.Source);

        if (saved <= 0 || dur <= 0)
            episode.ProgressText = "未观看";
        else if (saved >= dur - PlaybackPositionStore.EndThresholdSeconds)
            episode.ProgressText = "已看完";
        else
            episode.ProgressText = $"上次看到 {FormatSeconds(saved)}";
    }

    /// <summary>把秒数格式化成 mm:ss 或 hh:mm:ss</summary>
    public static string FormatSeconds(double seconds)
    {
        var t = TimeSpan.FromSeconds(Math.Max(0, seconds));
        return t.TotalHours >= 1
            ? $"{(int)t.TotalHours:00}:{t.Minutes:00}:{t.Seconds:00}"
            : $"{t.Minutes:00}:{t.Seconds:00}";
    }
}
