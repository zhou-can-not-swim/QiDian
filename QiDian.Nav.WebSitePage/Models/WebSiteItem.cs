using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QiDian.Nav.WebSitePage.Models
{
    public sealed class WebSiteItem
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public string Description { get; set; }
        public string SearchUrl { get; set; }

        public List<WebSiteTag> Tags { get; set; } = new List<WebSiteTag>();
        public WebType Type { get; set; } = WebType.普通;

        public WebSiteItem(string name, string url, string description, string searchUrl)
        {
            Name = name;
            Url = url;
            Description = description;
            SearchUrl = searchUrl;

        }
    }

    public enum WebType
    {
        特殊 = 1,
        普通 = 2
    }
}
