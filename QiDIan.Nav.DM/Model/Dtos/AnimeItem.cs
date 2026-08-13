using ReactiveUI;
using System.Collections.ObjectModel;

namespace QiDian.Nav.DM.Model.Dtos;

/// <summary>
/// 一部番剧。封面/简介/更新信息由爬虫填充；选集放在 <see cref="Episodes"/>。
/// 同一份实例会同时出现在首页卡片、详情页和播放页抽屉里，因此要复用对象而不是复制。
/// </summary>
public class AnimeItem : ReactiveObject
{
    /// <summary>番剧唯一标识（爬虫来源页 URL 或自定义 ID），用作详情页缓存键</summary>
    public string Id { get; init; } = "";

    /// <summary>番剧标题</summary>
    public string Title { get; init; } = "";

    /// <summary>封面图片地址（网页 URL 或本地路径），支持后续动态赋值</summary>
    private string _cover = "";
    public string Cover
    {
        get => _cover;
        set => this.RaiseAndSetIfChanged(ref _cover, value);
    }

    /// <summary>简介</summary>
    private string _description = "";
    public string Description
    {
        get => _description;
        set => this.RaiseAndSetIfChanged(ref _description, value);
    }

    /// <summary>更新信息，如"更新至 12 话" / "完结 全24话" / "周三 20:00"</summary>
    private string _updateInfo = "";
    public string UpdateInfo
    {
        get => _updateInfo;
        set => this.RaiseAndSetIfChanged(ref _updateInfo, value);
    }

    /// <summary>评分（可选，0 表示未评分）</summary>
    private double _score;
    public double Score
    {
        get => _score;
        set => this.RaiseAndSetIfChanged(ref _score, value);
    }

    /// <summary>选集。详情页 / 播放页共用同一份列表，播放进度写在每个 <c>EpisodeItem</c> 上</summary>
    public ObservableCollection<EpisodeItem> Episodes { get; } = new();
}
