using QiDian.Contracts;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;

namespace QiDian.Nav.DM;

public class DMViewModel : ViewModelBase
{
    /// <summary>剧集列表（左侧）</summary>
    public ObservableCollection<EpisodeItem> Episodes { get; } = new();

    /// <summary>当前选中的剧集</summary>
    [Reactive]
    public EpisodeItem? SelectedEpisode { get; set; }

    /// <summary>底部状态提示（加载中 / 自动续播等）</summary>
    [Reactive]
    public string StatusMessage { get; set; } = "选择剧集开始播放";

    /// <summary>当前播放时间 / 总时长</summary>
    [Reactive]
    public string PositionText { get; set; } = "00:00 / 00:00";

    public DMViewModel()
    {
        LoadEpisodes();

        if (Episodes.Count > 0)
            SelectedEpisode = Episodes[0];
    }

    /// <summary>
    /// 加载剧集列表。
    /// 目前为测试阶段：扫描本地测试视频文件夹；后续改成网页播放源后，
    /// 在这里替换为根据动漫页面解析出的各集播放地址即可（保留 EpisodeItem.Source 为播放地址）。
    /// </summary>
    private void LoadEpisodes()
    {
        // TODO: 测试用本地视频，后面改为网页播放源路径
        var testDir = @"C:\Users\qiyu.zhou\Desktop\test";
        if (Directory.Exists(testDir))
        {
            var files = Directory.GetFiles(testDir, "*.mp4")
                .OrderBy(NaturalSortKey)
                .ToList();

            for (int i = 0; i < files.Count; i++)
            {
                var path = files[i];
                Episodes.Add(new EpisodeItem
                {
                    Id = (i + 1).ToString(),
                    Title = $"第{i + 1}集",
                    Source = path,
                });
            }
        }
    }

    /// <summary>刷新列表里某一集的进度文案（供 View 保存进度后调用）</summary>
    public void RefreshProgressText(EpisodeItem? episode)
    {
        if (episode == null || string.IsNullOrWhiteSpace(episode.Source)) return;

        var saved = PlaybackPositionStore.GetPositionSeconds(episode.Source);
        var dur = PlaybackPositionStore.GetDurationSeconds(episode.Source);

        if (saved <= 0 || dur <= 0)
        {
            episode.ProgressText = "未观看";
        }
        else if (saved >= dur - PlaybackPositionStore.EndThresholdSeconds)
        {
            episode.ProgressText = "已看完";
        }
        else
        {
            episode.ProgressText = $"上次看到 {FormatSeconds(saved)}";
        }
    }

    /// <summary>把秒数格式化成 mm:ss 或 hh:mm:ss</summary>
    public static string FormatSeconds(double seconds)
    {
        var t = TimeSpan.FromSeconds(Math.Max(0, seconds));
        return t.TotalHours >= 1
            ? $"{(int)t.TotalHours:00}:{t.Minutes:00}:{t.Seconds:00}"
            : $"{t.Minutes:00}:{t.Seconds:00}";
    }

    // 简单自然排序：让 1、2、3 ... 10 按数字顺序而不是 "1,10,2"
    private static string NaturalSortKey(string path)
        => Regex.Replace(path, @"\d+", m => m.Value.PadLeft(8, '0'));
}
