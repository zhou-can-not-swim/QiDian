using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace QiDian.Nav.DM.Model.Dtos;

/// <summary>
/// 单个剧集条目。Source 为播放地址：本地视频文件路径或网页播放源（如 https://xxx/xxx.mp4）。
/// </summary>
public class EpisodeItem : ReactiveObject
{
    /// <summary>剧集唯一标识（当前为序号，后续接入网页源后可用播放地址或集数）</summary>
    public string Id { get; init; } = "";

    /// <summary>列表显示的标题，如"第1集"</summary>
    public string Title { get; init; } = "";

    /// <summary>本地文件路径或网页播放地址（同时作为保存进度的唯一键）</summary>
    public string Source { get; init; } = "";

    /// <summary>列表里展示的观看进度（如"上次看到 12:34" / "已看完" / "未观看"）</summary>
    [Reactive]
    public string ProgressText { get; set; } = "未观看";
}
