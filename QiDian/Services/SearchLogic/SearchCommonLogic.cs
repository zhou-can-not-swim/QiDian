using IWshRuntimeLibrary;
using QiDian.Models;
using System;
using System.Collections.Generic;
using System.IO;
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
                if (System.IO.File.Exists(targetPath))
                {
                    return targetPath; 
                }
                else
                {

                    return "";
                }
            }
            catch(Exception ex)
            {
                return "";
            }

        }

        //每一个文件的打分规则简化版
        public static double CalculateScore(FileEntry file, string keyword)
        {
            if (string.IsNullOrEmpty(keyword)) return 0;

            string keywordLower = keyword.ToLower();
            string fileName = file.FileName.ToLower();
            string fullPath = file.FullPath.ToLower();

            var matchScore = 0.0;

            //完全文件名匹/开头匹配/软件名模糊匹配
            var score1 = 0.0;
            if (fileName == keywordLower) score1 = 100.0;
            else if (fileName.StartsWith(keywordLower)) score1 = 80.0;
            else if (FuzzyMatch(fileName, keywordLower)) score1 = 70.0;
            else score1 = 0.0;
            matchScore += score1 * 0.6;
            //路径匹配
            double score2 = CalculateScoreByPathMatch(file, keywordLower);
            matchScore += score2 * 0.3;

            return matchScore;
        }

        private static double CalculateScoreByPathMatch(FileEntry file, string keyword)
        {
            string keywordLower = keyword.ToLower();

            // 按 \ 拆分路径
            string[] parts = file.FullPath.Split('\\');
            string fileName = parts[^1].ToLower();           // 最后一段：1.exe
            string fileNameNoExt = Path.GetFileNameWithoutExtension(parts[^1]).ToLower();
            string parentDir = parts.Length >= 2 ? parts[^2].ToLower() : "";  // 倒数第二段
            string grandParent = parts.Length >= 3 ? parts[^3].ToLower() : ""; // 倒数第三段

            double score = 0;

            if (FuzzyMatch(parentDir,keywordLower)) { score = 40; return score; }
            else if (FuzzyMatch(grandParent, keywordLower)) { score = 30;return score; }
            else return score;
        }
    }
}
