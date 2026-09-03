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

        /// <summary>快捷直达标签（普通站点为空，抽屉不展示该区块）</summary>
        public List<WebSiteTag> Tags { get; set; } = new List<WebSiteTag>();

        /// <summary>站点类型（暂不影响布局，仅作数据标记保留）</summary>
        public WebType Type { get; set; } = WebType.普通;

        public WebSiteItem(string name, string url, string description, string icon)
        {
            Name = name;
            Url = url;
            Description = description;
            Icon = icon;
        }
    }

    public enum WebType
    {
        特殊 = 1,
        普通 = 2
    }
}
