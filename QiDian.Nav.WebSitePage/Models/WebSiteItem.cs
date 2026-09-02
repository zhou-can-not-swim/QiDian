using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QiDian.Nav.WebSitePage.Models
{
    /// <summary>单个精选网站条目（父 → 抽屉子控件之间传递的数据单元）</summary>
    public sealed class WebSiteItem
    {
        public string Name { get; }
        public string Url { get; }
        public string Description { get; }
        public string Icon { get; }

        public WebSiteItem(string name, string url, string description, string icon)
        {
            Name = name;
            Url = url;
            Description = description;
            Icon = icon;
        }
    }
}
