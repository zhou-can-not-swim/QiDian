using QiDian.Contracts;
using QiDian.Nav.DM.Model.Entities;
using ReactiveUI.Fody.Helpers;

namespace QiDian.Nav.DM.ViewModels;

/// <summary>
/// DM 导航页的宿主 ViewModel。本身不承载业务，只负责在一个内容区里切换内部页面：
/// 首页（热门番剧/今日更新）→ 搜索页 → 番剧详情页 → 全屏播放页。
/// 子页面由 <see cref="DMNavigator"/> 统一创建和调度。
/// </summary>
public class DMViewModel : ViewModelBase
{
    public DMNavigator Navigator { get; }

    /// <summary>当前正在展示的内部页面（绑定宿主 ContentControl）</summary>
    [Reactive]
    public ViewModelBase? CurrentPage { get; set; }

    public DMViewModel()
    {
        Navigator = new DMNavigator(this);
        Navigator.GoHome(); // 默认进入首页
    }

    /// <summary>宿主展示指定的子页面（由 DMNavigator 调用）</summary>
    public void ShowPage(ViewModelBase page) => CurrentPage = page;
}
