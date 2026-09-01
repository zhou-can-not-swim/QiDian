using IWshRuntimeLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace QiDian.Services.SearchLogic
{
    public class SearchCommonLogic
    {
        public static bool FuzzyMatch(string input, string searchTerm, bool ignoreCase = true)
        {
            if (string.IsNullOrEmpty(searchTerm)) return true;
            if (string.IsNullOrEmpty(input)) return false;

            // 1. 把搜索词拆成单个字符，中间用 ".*" 连接
            //    例如 "VS" -> "V.*S"
            //    "abc" -> "a.*b.*c"
            string pattern = string.Join(".*", searchTerm.Select(c => Regex.Escape(c.ToString())));
            RegexOptions options = ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;
            return Regex.IsMatch(input, pattern, options);
        }

        public static string ExeFilePath(string lnkFile)
        {
            try
            {
                WshShell shell = new WshShell();
                IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(lnkFile);
                string targetPath = shortcut.TargetPath;
                return targetPath;
            }
            catch(Exception ex)
            {
                return "";
            }

        }
    }
}
