using System.Collections.ObjectModel;

namespace QiDian.Nav.DM.Model.Dtos;

/// <summary>
/// 首页上的一个栏目（横向卡片区）。栏目可动态添加：
/// <c>var s = new DMSection("热门番剧"); homeVm.Sections.Add(s); s.Items.Add(...)</c>
/// </summary>
public class DMSection
{
    public DMSection(string title)
    {
        Title = title;
    }

    /// <summary>栏目标题，如"热门番剧" / "今日更新"</summary>
    public string Title { get; }

    /// <summary>栏目内的番剧卡片</summary>
    public ObservableCollection<AnimeItem> Items { get; } = new();
}
