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
using System.Windows.Shapes;
using Zhou.Security;

namespace QiDian.Nav.WebSitePage.ViewModels
{
    public class WebSitePageViewModel : ViewModelBase
    { 
        public string fileName1Path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QiDian", "WebSiteItems.enc");
        public string fileName2Path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QiDian", "WebSiteTags.enc");

        public ObservableCollection<WebSiteItem> Sites { get; } = new();
        public ObservableCollection<WebSiteTag> Tags { get; } = new();

        public WebSiteDetailViewModel Detail { get; }

        [Reactive]
        public WebSiteItem? SelectedSite { get; set; }

        [Reactive]
        public bool IsDrawerOpen { get; set; }

        public ReactiveCommand<Unit, Unit> AddWebSiteItem { get; }//打开网页
        public ReactiveCommand<Unit, Unit> AddWebSiteTag { get; }//打开新增标签弹窗

        private IJsonEncryptionService _json;


        public WebSitePageViewModel(IJsonEncryptionService jsonED)
        {
            Detail = new WebSiteDetailViewModel();
            _json = jsonED;
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
            
            if (!File.Exists(fileName1Path))
            {
                File.Create(fileName1Path).Close();
            }


            if (!File.Exists(fileName2Path))
            {
                File.Create(fileName2Path).Close();
            }

            List<WebSiteItem> ws = ReadWebSiteItems() ?? new List<WebSiteItem>();
            List<WebSiteTag> wts = ReadWebSiteTags() ?? new List<WebSiteTag>();
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

            var fPath = fileName1Path;
            var webSiteItems = _json.DecryptFromFile<List<WebSiteItem>>(fPath);
            return webSiteItems;
        }

        public List<WebSiteTag> ReadWebSiteTags()
        {

            var fPath = fileName2Path;
            
            var webSiteTags = _json.DecryptFromFile<List<WebSiteTag>>(fPath);
            return webSiteTags;
        }
        #endregion

        #region 写文件
        public void SaveWebSiteItems()
        {
            var fPath = fileName1Path;
            _json.EncryptToFile(Sites.ToList(), fPath);
        }

        public void SaveWebSiteTags()
        {
            var fPath = fileName2Path;
            _json.EncryptToFile(Tags.ToList(), fPath);
        }
        #endregion

    }
}
