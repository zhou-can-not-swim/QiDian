using QiDian.Contracts;
using QiDian.Nav.WebSitePage.Models;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Security.Policy;

namespace QiDian.Nav.WebSitePage.ViewModels
{
    /// <summary>父 ViewModel：管理网站列表与选中项，把选中站点喂给抽屉详情 VM 并控制抽屉开关</summary>
    public class WebSitePageViewModel : ViewModelBase
    { 
        public ObservableCollection<WebSiteItem> Sites { get; } = new();
        public ObservableCollection<WebSiteTag> Tags { get; } = new();

        /// <summary>抽屉详情 VM（Drawer 内 WebSiteDetailView 绑定它）</summary>
        public WebSiteDetailViewModel Detail { get; }

        [Reactive]
        public WebSiteItem? SelectedSite { get; set; }

        [Reactive]
        public bool IsDrawerOpen { get; set; }

        public ReactiveCommand<Unit, Unit> AddWebSiteItem { get; }//打开网页



        public WebSitePageViewModel()
        {
            Detail = new WebSiteDetailViewModel();
            InitDatas();

            this.WhenAnyValue(x => x.SelectedSite)
               .Where(site => site != null)
               .ObserveOn(RxApp.MainThreadScheduler)
               .Subscribe(site => OpenDrawer(site!));

            AddWebSiteItem = ReactiveCommand.Create(() => AddWebSite());
        }

        private void AddWebSite()
        {
            throw new NotImplementedException();
        }

        public void OpenDrawer(WebSiteItem site)
        {
            Detail.Site = site;
            IsDrawerOpen = true;
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
            OpenDrawer(site);
        }

        #region 初始化数据
        private void InitDatas()
        {
            List<WebSiteItem> ws = ReadWebSiteItems();
            List<WebSiteTag> wts = ReadWebSiteTags();
            wts.ForEach(tag =>
            {
                Tags.Add(tag);
            });
            ws.ForEach(site =>
            {
                site.Tags = wts;
                Sites.Add(site);
            });
        }

        public List<WebSiteItem> ReadWebSiteItems()
        {

            var fPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QiDian", "WebSiteItems.json");
            if (!File.Exists(fPath))
            {
                throw new FileNotFoundException($"JSON file not found: {fPath}");
            }
            string jsonContent = File.ReadAllText(fPath);
            if (string.IsNullOrEmpty(jsonContent))
            {
                return new List<WebSiteItem>();
            }
            var webSiteItems = System.Text.Json.JsonSerializer.Deserialize<List<WebSiteItem>>(jsonContent);
            return webSiteItems;
        }

        public List<WebSiteTag> ReadWebSiteTags()
        {

            var fPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QiDian", "WebSiteTag.json");
            if (!File.Exists(fPath))
            {
                throw new FileNotFoundException($"JSON file not found: {fPath}");
            }
            string jsonContent = File.ReadAllText(fPath);
            if (string.IsNullOrEmpty(jsonContent))
            {
                return new List<WebSiteTag>();
            }
            var webSiteTags = System.Text.Json.JsonSerializer.Deserialize<List<WebSiteTag>>(jsonContent);
            return webSiteTags;
        }
        #endregion
    }
}
