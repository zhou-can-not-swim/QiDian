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
    public class WebSitePageViewModel : ViewModelBase
    { 
        public ObservableCollection<WebSiteItem> Sites { get; } = new();
        public ObservableCollection<WebSiteTag> Tags { get; } = new();

        public WebSiteDetailViewModel Detail { get; }

        [Reactive]
        public WebSiteItem? SelectedSite { get; set; }

        [Reactive]
        public bool IsDrawerOpen { get; set; }

        public ReactiveCommand<Unit, Unit> AddWebSiteItem { get; }//打开网页
        public ReactiveCommand<Unit, Unit> AddWebSiteTag { get; }//打开新增标签弹窗

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

            AddWebSiteTag = ReactiveCommand.Create(() => AddTag());

            var canConfirmTag = this.WhenAnyValue(x => x.NewTagName,
                name => !string.IsNullOrWhiteSpace(name));
            ConfirmTagCommand = ReactiveCommand.Create(ConfirmAddTag, canConfirmTag);
            CancelTagCommand = ReactiveCommand.Create(() => { IsTagOpen = false; });
        }

        #region 新增网站弹窗

        [Reactive]
        public bool IsAddOpen { get; set; }

        [Reactive]
        public string NewName { get; set; } = "";

        [Reactive]
        public string NewUrl { get; set; } = "";

        [Reactive]
        public string NewDescription { get; set; } = "";

        [Reactive]
        public string NewSearchUrl { get; set; } = "";

        public ReactiveCommand<Unit, Unit> ConfirmAddCommand { get; }

        public ReactiveCommand<Unit, Unit> CancelAddCommand { get; }

        #endregion

        #region 新增标签弹窗

        [Reactive]
        public bool IsTagOpen { get; set; }
        [Reactive]
        public string NewTagName { get; set; } = "";

        public ReactiveCommand<Unit, Unit> ConfirmTagCommand { get; }

        public ReactiveCommand<Unit, Unit> CancelTagCommand { get; }

        #endregion


        #region 打开新增弹窗
        private void AddWebSite()
        {
            ResetAddForm();
            IsAddOpen = true;
        }

        private void ConfirmAddSite()
        {
            var site = new WebSiteItem(
                NewName?.Trim() ?? "",
                NewUrl?.Trim() ?? "",
                NewDescription?.Trim() ?? "",
                string.IsNullOrWhiteSpace(NewSearchUrl) ? "" : NewSearchUrl.Trim());
            Sites.Add(site);
            ApplyTagsToSites(); // 新建站点也共用同一套全局快捷标签
            SaveWebSiteItems();
            IsAddOpen = false;
        }

        private void ResetAddForm()
        {
            NewName = "";
            NewUrl = "";
            NewDescription = "";
            NewSearchUrl = "";
        }
        #endregion

        #region 打开新增标签弹窗
        private void AddTag()
        {
            ResetTagForm();
            IsTagOpen = true;
        }

        private void ConfirmAddTag()
        {
            var tagName = NewTagName?.Trim() ?? "";
            if (string.IsNullOrEmpty(tagName)) return;

            var tag = new WebSiteTag() { Name = tagName };
            Tags.Add(tag);
            ApplyTagsToSites();
            SaveWebSiteTags();
            IsTagOpen = false;
        }

        private void ApplyTagsToSites()
        {
            var tags = Tags.ToList();
            foreach (var site in Sites)
                site.Tags = tags;
        }

        private void ResetTagForm()
        {
            NewTagName = "";
        }
        #endregion

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
        #region 读文件
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

        #region 写文件
        public void SaveWebSiteItems()
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QiDian");
            Directory.CreateDirectory(dir);
            var fPath = Path.Combine(dir, "WebSiteItems.json");
            string json = System.Text.Json.JsonSerializer.Serialize(Sites.ToList(),
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(fPath, json);
        }

        public void SaveWebSiteTags()
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QiDian");
            Directory.CreateDirectory(dir);
            var fPath = Path.Combine(dir, "WebSiteTag.json");
            string json = System.Text.Json.JsonSerializer.Serialize(Tags.ToList(),
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(fPath, json);
        }
        #endregion
    }
}
