using QiDian.Contracts;
using QiDian.Nav.DM.Model.Dtos;
using QiDian.Nav.DM.Model.Entities;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Reactive;
using Zhou.CrawlerAdapter.Services;

namespace QiDian.Nav.DM.ViewModels;

/// <summary>
/// DM 首页：顶部搜索栏 + 可动态添加的栏目（热门番剧 / 今日更新）。
/// 栏目数据由爬虫填充，见 <see cref="LoadSections"/> 里的 TODO。
/// </summary>
public class DMHomeViewModel : ViewModelBase
{
    private string url = "https://www.dm845.com/";
    private readonly DMNavigator _navigator;
    private readonly ICrawlerService crawler;

    /// <summary>栏目集合。调用 <see cref="AddSection"/> 或直接向此集合添加 <see cref="DMSection"/> 即可动态加栏目</summary>
    public ObservableCollection<DMSection> Sections { get; } = new();

    /// <summary>搜索栏占位提示文字</summary>
    public string SearchPlaceholder { get; set; } = "搜索番剧";

    /// <summary>点击番剧卡片 → 进入详情页（卡片模板通过 RelativeSource 绑定到此命令）</summary>
    public ReactiveCommand<AnimeItem, Unit> OpenAnimeCommand { get; }

    public DMHomeViewModel(DMNavigator navigator)
    {
        _navigator = navigator;
        OpenAnimeCommand = ReactiveCommand.Create<AnimeItem>(OpenAnime);
        LoadSections();
    }

    /// <summary>点击搜索栏 → 进入搜索页</summary>
    public void OpenSearch() => _navigator.OpenSearch();

    /// <summary>点击某部番剧卡片 → 进入详情页</summary>
    public void OpenAnime(AnimeItem anime) => _navigator.OpenAnime(anime);

    /// <summary>动态新增一个栏目（可多次调用）</summary>
    public DMSection AddSection(string title)
    {
        var section = new DMSection(title);
        Sections.Add(section);
        return section;
    }

    /// <summary>
    /// 加载首页栏目数据。
    /// TODO: 在这里接入爬虫 —— 推荐保留"热门番剧""今日更新"两个默认栏目，
    /// 向对应 <see cref="DMSection.Items"/> 里 Add 爬取到的 <see cref="AnimeItem"/>
    /// （记得填充 Cover / UpdateInfo / Episodes）。
    /// </summary>
    private void LoadSections()
    {
        Sections.Clear();

        // 默认栏目；后续接爬虫时把结果填进去，或用 AddSection 加更多栏目
        var hot = AddSection("热门番剧");
        var today = AddSection("今日更新");

        // ===== 仅用于界面预览的示例数据，接入爬虫后删除 LoadSampleData() 这一行即可 =====
        LoadSampleData(hot, today);
    }

    /// <summary>示例数据：让首页 → 详情 → 播放整条链路可以跑起来看效果</summary>
    private void LoadSampleData(DMSection hot, DMSection today)
    {
        hot.Items.Add(CreateSample("sample-railgun", "某科学的超电磁炮", "更新至 12 话", 8.9,
            "学园都市中，拥有超能力的少女们的故事。", 12));
        hot.Items.Add(CreateSample("sample-aot", "进击的巨人", "完结 全24话", 9.4,
            "人类与巨人对抗的史诗。", 24));
        hot.Items.Add(CreateSample("sample-spy", "间谍过家家", "更新至 11 话", 8.7,
            "间谍、杀手与超能力少女组成的临时家庭。", 11));

        today.Items.Add(CreateSample("sample-jjk", "咒术回战", "今日更新 第5话", 8.8,
            "咒术师与诅咒之间的战斗。", 5));
        today.Items.Add(CreateSample("sample-demon", "鬼灭之刃", "今日更新 第9话", 9.1,
            "炭治郎讨伐恶鬼的旅程。", 9));
    }

    /// <summary>生成示例番剧（含若干"第N集"占位选集，播放源留空由爬虫填充）</summary>
    private static AnimeItem CreateSample(string id, string title, string updateInfo, double score,
        string description, int episodeCount)
    {
        var anime = new AnimeItem
        {
            Id = id,
            Title = title,
            UpdateInfo = updateInfo,
            Score = score,
            Description = description,
        };
        for (int i = 1; i <= episodeCount; i++)
            anime.Episodes.Add(new EpisodeItem { Id = i.ToString(), Title = $"第{i}集" });
        return anime;
    }
}
