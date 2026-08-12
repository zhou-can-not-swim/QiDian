using System.IO;
using System.Text.Json;

namespace QiDian.Nav.DM;

/// <summary>
/// 某剧集的播放进度记录。
/// </summary>
public sealed class PlaybackRecord
{
    /// <summary>上次播放到的秒数</summary>
    public double PositionSeconds { get; set; }

    /// <summary>总时长（秒），用于判断是否已看完</summary>
    public double DurationSeconds { get; set; }

    /// <summary>最近更新时间</summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// 剧集播放进度持久化。以每个剧集的播放地址（本地路径或网页 URL）为唯一键，
/// 记录上次播放到第几秒，下次打开同一集时自动续播。
/// 存储位置：%AppData%\QiDian\dm_playback.json（与主程序 HotkeySettingsStore 同一目录约定）
/// </summary>
public static class PlaybackPositionStore
{
    /// <summary>离结尾多少秒以内视为"已看完"，下次从头播放</summary>
    public const double EndThresholdSeconds = 30;

    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "QiDian",
        "dm_playback.json");

    private static readonly object _lock = new();

    // 内存缓存，避免每次保存/读取都访问磁盘
    private static Dictionary<string, PlaybackRecord>? _cache;

    private static Dictionary<string, PlaybackRecord> Load()
    {
        if (_cache != null) return _cache;

        lock (_lock)
        {
            if (_cache != null) return _cache;

            var dict = new Dictionary<string, PlaybackRecord>();
            try
            {
                if (File.Exists(FilePath))
                {
                    var json = File.ReadAllText(FilePath);
                    var loaded = JsonSerializer.Deserialize<Dictionary<string, PlaybackRecord>>(json);
                    if (loaded != null) dict = loaded;
                }
            }
            catch
            {
                // 配置损坏时回退到空缓存，不阻断主流程
            }
            _cache = dict;
            return dict;
        }
    }

    /// <summary>读取某剧集上次播放到的秒数，无记录返回 0</summary>
    public static double GetPositionSeconds(string source)
    {
        if (string.IsNullOrWhiteSpace(source)) return 0;
        var dict = Load();
        return dict.TryGetValue(source, out var r) ? r.PositionSeconds : 0;
    }

    /// <summary>读取某剧集记录过的总时长（秒），无记录返回 0</summary>
    public static double GetDurationSeconds(string source)
    {
        if (string.IsNullOrWhiteSpace(source)) return 0;
        var dict = Load();
        return dict.TryGetValue(source, out var r) ? r.DurationSeconds : 0;
    }

    /// <summary>
    /// 返回某剧集应从第几秒开始续播。已看完（上次位置离结尾不足 EndThresholdSeconds）则返回 0 从头播。
    /// actualDuration 有值时优先用实时总时长判断，否则用上次记录的时长。
    /// </summary>
    public static double GetResumeSeconds(string source, double? actualDuration = null)
    {
        var saved = GetPositionSeconds(source);
        if (saved <= 0) return 0;

        var dur = actualDuration ?? GetDurationSeconds(source);
        if (dur > 0 && saved >= dur - EndThresholdSeconds)
            return 0;   // 上次已看到结尾附近，从头播

        return saved;
    }

    /// <summary>保存某剧集播放进度</summary>
    public static void SavePosition(string source, double positionSeconds, double durationSeconds)
    {
        if (string.IsNullOrWhiteSpace(source) || positionSeconds <= 0 || durationSeconds <= 0) return;

        lock (_lock)
        {
            var dict = Load();
            dict[source] = new PlaybackRecord
            {
                PositionSeconds = positionSeconds,
                DurationSeconds = durationSeconds,
                UpdatedAt = DateTime.Now,
            };
            Flush(dict);
        }
    }

    /// <summary>标记某剧集已看完（删除记录，下次从头播放）</summary>
    public static void MarkCompleted(string source)
    {
        if (string.IsNullOrWhiteSpace(source)) return;

        lock (_lock)
        {
            var dict = Load();
            dict.Remove(source);
            Flush(dict);
        }
    }

    private static void Flush(Dictionary<string, PlaybackRecord> dict)
    {
        try
        {
            var dir = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
        }
        catch
        {
            // 写入失败不阻塞播放
        }
    }
}
