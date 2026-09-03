using QiDian.Contracts;
using QiDian.Nav.WebSitePage.Models;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Diagnostics;
using System.Reactive;
using System.Windows;
using ZhouLib;

namespace QiDian.Nav.WebSitePage.ViewModels
{

    public class WebSiteDetailViewModel : ViewModelBase
    {
        [Reactive]
        public WebSiteItem? Site { get; set; }
        public bool HasTags => Site?.Tags.Count>0?true:false;

        public ReactiveCommand<Unit, Unit> OpenSiteCommand { get; }//打开网页
        public ReactiveCommand<WebSiteTag, Unit> OpenTagCommand { get; }//网页+标签

        public WebSiteDetailViewModel()
        {
            //通知HasTags属性变化，刷新UI
            this.WhenAnyValue(x => x.Site)
                .Subscribe(_ => this.RaisePropertyChanged(nameof(HasTags)));

            var canOpenSite = this.WhenAnyValue(
                x => x.Site,
                s => s != null && !string.IsNullOrWhiteSpace(s.Url));

            OpenSiteCommand = ReactiveCommand.Create(() => OpenUrl(Site?.Url), canOpenSite);
            OpenTagCommand = ReactiveCommand.Create<WebSiteTag>(OpenTag);
        }

        private void OpenTag(WebSiteTag tag)
        {
            var b = new BrowserMethod();
            b.OpenBrowserInPrivateMode(Site?.SearchUrl + tag.Name);
        }

        private void OpenUrl(string? url)
        {
            var b = new BrowserMethod();
            b.OpenBrowserInPrivateMode(url);
        }
    }
}
