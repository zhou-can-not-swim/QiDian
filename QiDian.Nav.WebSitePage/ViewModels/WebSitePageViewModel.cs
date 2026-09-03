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

        #region 新增网站弹窗（AddWebSite 打开，确认后写回 WebSiteItems.json）

        /// <summary>新增弹窗是否打开</summary>
        [Reactive]
        public bool IsAddOpen { get; set; }

        /// <summary>弹窗表单：新网站名称</summary>
        [Reactive]
        public string NewName { get; set; } = "";

        /// <summary>弹窗表单：新网站网址</summary>
        [Reactive]
        public string NewUrl { get; set; } = "";

        /// <summary>弹窗表单：简介（可为空）</summary>
        [Reactive]
        public string NewDescription { get; set; } = "";

        /// <summary>弹窗表单：图标（emoji，可为空，为空落库时默认 🌐）</summary>
        [Reactive]
        public string NewIcon { get; set; } = "";

        /// <summary>确认新增（名称与网址都不为空时可用）</summary>
        public ReactiveCommand<Unit, Unit> ConfirmAddCommand { get; }

        /// <summary>取消/关闭新增弹窗</summary>
        public ReactiveCommand<Unit, Unit> CancelAddCommand { get; }

        #endregion

        public WebSitePageViewModel()
        {
            Detail = new WebSiteDetailViewModel();
            InitDatas();

            this.WhenAnyValue(x => x.SelectedSite)
               .Where(site => site != null)
               .ObserveOn(RxApp.MainThreadScheduler)
               .Subscribe(site => OpenDrawer(site!));

            AddWebSiteItem = ReactiveCommand.Create(() => AddWebSite());

            var canConfirm = this.WhenAnyValue(x => x.NewName, x => x.NewUrl,
                (name, url) => !string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(url));
            ConfirmAddCommand = ReactiveCommand.Create(ConfirmAddSite, canConfirm);
            CancelAddCommand = ReactiveCommand.Create(() => { IsAddOpen = false; });
        }

        /// <summary>打开新增弹窗（重置表单 + 置可见）</summary>
        private void AddWebSite()
        {
            ResetAddForm();
            IsAddOpen = true;
        }

        /// <summary>确认：构造 WebSiteItem 加入列表并写回 json</summary>
        private void ConfirmAddSite()
        {
            var site = new WebSiteItem(
                NewName?.Trim() ?? "",
                NewUrl?.Trim() ?? "",
                NewDescription?.Trim() ?? "",
                string.IsNullOrWhiteSpace(NewIcon) ? "🌐" : NewIcon.Trim());
            Sites.Add(site);
            SaveWebSiteItems();
            IsAddOpen = false;
        }

        /// <summary>重置弹窗表单为空白值（供每次打开时调用）</summary>
        private void ResetAddForm()
        {
            NewName = "";
            NewUrl = "";
            NewDescription = "";
            NewIcon = "🌐";
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

        /// <summary>把当前列表整体写回 WebSiteItems.json（新增站点后调用，目录缺失时自动创建）</summary>
        public void SaveWebSiteItems()
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QiDian");
            Directory.CreateDirectory(dir);
            var fPath = Path.Combine(dir, "WebSiteItems.json");
            string json = System.Text.Json.JsonSerializer.Serialize(Sites.ToList(),
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(fPath, json);
        }
        #endregion
    }
}
