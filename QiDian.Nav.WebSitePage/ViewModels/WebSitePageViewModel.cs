using QiDian.Contracts;
using QiDian.Nav.WebSitePage.Models;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System.Collections.ObjectModel;

namespace QiDian.Nav.WebSitePage.ViewModels
{
    /// <summary>父 ViewModel：管理网站列表与选中项，抽屉开关随选中自动打开</summary>
    public class WebSitePageViewModel : ViewModelBase
    {
        /// <summary>网站列表（后续可改为从配置/数据库加载）</summary>
        public ObservableCollection<WebSiteItem> Sites { get; } = new();

        private WebSiteItem? _selectedSite;

        /// <summary>当前选中的网站（父 View 通过绑定传给抽屉子控件）</summary>
        public WebSiteItem? SelectedSite
        {
            get => _selectedSite;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedSite, value);
                if (value != null) IsDrawerOpen = true; // 选中即滑出抽屉
            }
        }

        /// <summary>抽屉是否打开（View 监听此属性执行滑入/滑出动画）</summary>
        [Reactive]
        public bool IsDrawerOpen { get; set; }

        public WebSitePageViewModel()
        {
            Sites.Add(new WebSiteItem("GitHub", "https://github.com", "全球最大的代码托管与开源社区", "🐙"));
            Sites.Add(new WebSiteItem("Gitee", "https://gitee.com", "国内代码托管平台（码云）", "🦋"));
            Sites.Add(new WebSiteItem("Stack Overflow", "https://stackoverflow.com", "程序员问答社区", "🧑‍💻"));
            Sites.Add(new WebSiteItem(".NET 文档", "https://learn.microsoft.com/zh-cn/dotnet/", "微软官方 .NET 技术文档", "📘"));
            Sites.Add(new WebSiteItem("MDN Web Docs", "https://developer.mozilla.org/zh-CN/", "Web 前端权威参考", "🌐"));
            Sites.Add(new WebSiteItem("掘金", "https://juejin.cn", "中文技术社区与博客", "⛏️"));
            Sites.Add(new WebSiteItem("菜鸟教程", "https://www.runoob.com", "编程入门教程", "🐤"));
            Sites.Add(new WebSiteItem("LeetCode", "https://leetcode.cn", "算法刷题与面试准备", "💻"));
            Sites.Add(new WebSiteItem("ProcessOn", "https://www.processon.com", "在线流程图 / 思维导图", "🧩"));
            Sites.Add(new WebSiteItem("腾讯文档", "https://docs.qq.com", "在线协作文档与表格", "📄"));
            Sites.Add(new WebSiteItem("百度翻译", "https://fanyi.baidu.com", "在线翻译", "🌏"));
            Sites.Add(new WebSiteItem("DeepL", "https://www.deepl.com/translator", "高质量在线翻译", "🪄"));
        }

        /// <summary>关闭抽屉</summary>
        public void CloseDrawer()
        {
            IsDrawerOpen = false;
        }

        /// <summary>选中网站并强制滑出抽屉（重复点击已选行也生效）</summary>
        public void SelectSite(WebSiteItem site)
        {
            SelectedSite = site;
            IsDrawerOpen = true;
        }
    }
}
