using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QiDian.Nav.WebSitePage.Models
{
    /// <summary>站点的「快捷直达」标签：点按后跳转到 站点Url + 关键词 拼接出的地址。</summary>
    public sealed class WebSiteTag
    {
        /// <summary>界面上显示的标签文字</summary>
        public string Name { get; }

        /// <summary>参与拼接的关键词；为空/空白时回退用 <see cref="Name"/> 拼接</summary>
        public string? Keyword { get; }

        public WebSiteTag(string name, string? keyword = null)
        {
            Name = name;
            Keyword = keyword;
        }
    }
}
