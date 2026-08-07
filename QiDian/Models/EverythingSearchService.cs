using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using QiDian.Helpers.LevelDBHelper;

namespace QiDian.Models
{
    /// <summary>
    /// 智能 Everything 搜索服务 - 支持动态匹配、缩写展开、拼音首字母
    /// </summary>
    public class EverythingSearchService
    {
        private const int REQUEST_FULL_PATH = 0x00000004;
        private const int REQUEST_SIZE = 0x00000010;
        private const int REQUEST_DATE = 0x00000040;
        private const int SORT_NAME_ASC = 1;

        [DllImport("Everything64.dll", CharSet = CharSet.Unicode)]
        private static extern int Everything_SetSearchW(string lpSearchString);
        [DllImport("Everything64.dll")]
        private static extern void Everything_SetRequestFlags(int dwRequestFlags);
        [DllImport("Everything64.dll")]
        private static extern void Everything_SetSort(int dwSortType);
        [DllImport("Everything64.dll")]
        private static extern void Everything_SetMax(int dwMax);
        [DllImport("Everything64.dll")]
        private static extern bool Everything_QueryW(bool bWait);
        [DllImport("Everything64.dll")]
        private static extern int Everything_GetNumResults();
        [DllImport("Everything64.dll", CharSet = CharSet.Unicode)]
        private static extern void Everything_GetResultFullPathNameW(int nIndex, StringBuilder lpString, int nMaxCount);
        [DllImport("Everything64.dll")]
        private static extern bool Everything_GetResultSize(int nIndex, out long lpFileSize);
        [DllImport("Everything64.dll")]
        private static extern bool Everything_GetResultDateModified(int nIndex, out long lpDateModified);

        private static readonly LevelDbStore _store = new LevelDbStore();
        public static void RecordUserChoice(string key, string fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath)) return;
            _store.RecordUsage(key, fullPath);
        }

        private static bool? _available;

        /// <summary>检测 Everything 是否可用</summary>
        public static bool IsAvailable()
        {
            if (_available.HasValue) return _available.Value;
            try
            {
                Everything_SetSearchW("");
                // 必须检查 QueryW 返回值：DLL 存在 ≠ Everything 服务在运行
                _available = Everything_QueryW(true);
            }
            catch (DllNotFoundException)
            {
                _available = false;
            }
            return _available.Value;
        }

        public List<FileEntry> Search(string keyword, int maxResults = 10000)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return new List<FileEntry>();

            var keywordLower = keyword.Trim().ToLower();

            var everythingResults = LowSearch(BuildKey(keyword), maxResults);

            // ── 将 LevelDB 使用记录映射到 FileEntry（供 CalculateScore 使用） ──
            var dbRecords = _store.GetRecordsByKeyword(keywordLower);
            //if (dbRecords.Count > 0 && everythingResults.Count > 0)
            //{
            //    var usageLookup = dbRecords
            //        .GroupBy(r => r.fullPath, StringComparer.OrdinalIgnoreCase)
            //        .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            //    foreach (var f in everythingResults)
            //    {
            //        if (usageLookup.TryGetValue(f.FullPath, out var record))//如果path对的上，复制count
            //        {
            //            f.UsageCount = record.Count;
            //            if (record.Timestamp > 0)
            //                f.LastUsed = DateTimeOffset.FromUnixTimeMilliseconds(record.Timestamp).DateTime;
            //        }
            //    }
            //}

            // ── 过滤：只保留 .exe / .lnk ──
            var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".exe",
                ".lnk"
            };
            var excludePattern = new Regex(
                @"\\Windows\\Temp\\|
                  \\\$Recycle\.Bin\\|
                  \\AppData\\Local\\Temp\\|
                  \\System32\\|\\Program Files\\WindowsApps\\|
                  \\Program Files \(x86\)\\",
                RegexOptions.IgnoreCase | RegexOptions.Compiled
            );
            var dbDict = dbRecords.ToDictionary(//构建成字典
                r => r.fullPath,
                r => r,
                StringComparer.OrdinalIgnoreCase
            );
            var scored = everythingResults
                .Where(f => allowedExtensions.Contains(Path.GetExtension(f.FullPath)))
                .Where(f => !excludePattern.IsMatch(f.FullPath))
                .Select(f => new { File = f, Score = CalculateScore(f, keywordLower, dbDict) })
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.File.FullPath)
                .Select(x => x.File)
                .Take(maxResults)
                .ToList();

            return scored;
        }

        /// <summary>
        /// 直接委托给 FuzzyMatchScore 统一算法。
        /// </summary>
        private double SmartMatchScore(string name, string keyword)
        {
            return FuzzyMatchScore(keyword, name);
        }

        public List<FileEntry> LowSearch(string keyword, int maxResults)
        {
            var results = new List<FileEntry>();
            if (string.IsNullOrWhiteSpace(keyword)) return results;

            try
            {
                // keyword要经过改动，比如vs搜不到Visual Studio Code 只能是*v*s*c*这样的
                // 但用 Everything 自己的结果来加速
                int err = Everything_SetSearchW($"*{keyword.Trim()}*");
                if (err != 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[Everything] SetSearchW error: {err}");
                    return results;
                }
                Everything_SetRequestFlags(REQUEST_FULL_PATH | REQUEST_SIZE | REQUEST_DATE);
                Everything_SetSort(SORT_NAME_ASC);
                //Everything_SetMax(maxResults * 2);

                if (!Everything_QueryW(true))
                {
                    System.Diagnostics.Debug.WriteLine("[Everything] QueryW returned false — Everything 服务可能未运行");
                    return results;
                }

                int count = Everything_GetNumResults();
                var sb = new StringBuilder(260);

                for (int i = 0; i < count; i++)
                {
                    sb.Clear();
                    Everything_GetResultFullPathNameW(i, sb, sb.Capacity);
                    Everything_GetResultSize(i, out long size);
                    Everything_GetResultDateModified(i, out long fileTime);

                    results.Add(new FileEntry
                    {
                        FullPath = sb.ToString(),
                        Size = size,
                        LastModified = fileTime > 0 ? DateTime.FromFileTime(fileTime) : DateTime.MinValue
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Everything] LowSearch exception: {ex.Message}");
            }

            return results;
        }

        /// <summary>
        /// 综合打分
        /// </summary>
        private double CalculateScore(FileEntry file, string keyword, Dictionary<string, MatchRecord> dbDic)
        {
            string? fileName = Path.GetFileNameWithoutExtension(file.FullPath);
            string? fileNameExt = Path.GetFileName(file.FullPath);

            double matchScore = Math.Max(
                FuzzyMatchScore(keyword, fileName ?? ""),
                FuzzyMatchScore(keyword, fileNameExt ?? ""));

            // ── 软件名关联加分（比例融合，而非简单叠加） ──
            if (!string.IsNullOrEmpty(file.SoftwareName))
            {
                double softScore = FuzzyMatchScore(keyword, file.SoftwareName);
                if (softScore > matchScore)
                    matchScore += (softScore - matchScore) * 0.6;

            }

            // ── 路径匹配加分 ──
            double pathScore = FuzzyMatchScore(keyword, file.FullPath.ToLower());
            if (pathScore > matchScore)
                matchScore += (pathScore - matchScore) * 0.3;

            //外部加分（使用频率）
            double extrinsicBonus = 0;
            bool v = dbDic.TryGetValue(file.FullPath, out var record);
            if (v)
            {
                file.UsageCount = record.Count;
            }
            else
            {
                file.UsageCount = 0;
            }

            if (file.UsageCount > 0)
            {
                extrinsicBonus += Math.Min(file.UsageCount * 10.0, 50);   // 次数加分，封顶 15

                //double ageDays = (DateTime.Now - file.LastUsed).TotalDays;
                //if (ageDays < 1)
                //    extrinsicBonus += 10;                                 // 今天用过
                //else if (ageDays < 7)
                //    extrinsicBonus += 7;                                  // 一周内
                //else if (ageDays < 30)
                //    extrinsicBonus += 3;                                  // 一月内
            }

            //if (file.LastModified > DateTime.Now.AddDays(-7))
            //    extrinsicBonus += 10;                            // 最近 7 天
            //else if (file.LastModified > DateTime.Now.AddDays(-30))
            //    extrinsicBonus += 5;                             // 最近 30 天

            ////扩展名加分
            //string ext = Path.GetExtension(file.FullPath)?.ToLower() ?? "";
            //if (ext == ".exe" || ext == ".lnk")
            //    extrinsicBonus += 5;                             // 可执行文件

            // 衰减权重：匹配分 0 → 权重 50%，匹配分 90 → 权重 5%
            double bonusWeight = Math.Max(0, (100 - Math.Min(matchScore, 100)) / 100.0) * 0.5;
            double totalScore = matchScore + extrinsicBonus * bonusWeight;

            return Math.Min(totalScore, 110);
        }


        /// <summary>
        /// </summary>
        /// <param name="query">用户输入的关键词</param>
        /// <param name="target">被匹配的目标字符串（文件名或软件名）</param>
        /// <returns>0=不匹配, 100=完全匹配, 95=前缀匹配, 1~94.9=模糊匹配分数</returns>
        internal static double FuzzyMatchScore(string query, string target)
        {
            if (string.IsNullOrEmpty(target) || string.IsNullOrEmpty(query))
                return 0;

            string q = query.Trim().ToLower();
            string tOrig = target;          // 保留原始大小写，用于判断驼峰等边界
            string t = target.ToLower();

            if (q.Length == 0 || q.Length > t.Length)
                return 0;

            if (t == q) return 100;
            if (t.StartsWith(q)) return 95;

            var allPos = new List<List<int>>(q.Length);
            for (int qi = 0; qi < q.Length; qi++)
            {
                var positions = new List<int>();
                char qc = q[qi];
                for (int ti = 0; ti < t.Length; ti++)
                {
                    if (t[ti] == qc)
                        positions.Add(ti);
                }
                if (positions.Count == 0)
                    return 0;
                allPos.Add(positions);
            }

            var prev = new Dictionary<int, (double Score, int FirstPos)>();

            foreach (int pos in allPos[0])
            {
                double s;
                if (pos == 0)
                    s = 20;                // 匹配在串首，最高加分
                else if (IsWordBoundary(tOrig, pos))
                    s = 15;                // 匹配在单词边界，次高加分
                else
                    s = 0;
                prev[pos] = (s, pos);
            }

            // ── 逐字符 DP，选最优前驱 ──
            for (int qi = 1; qi < q.Length; qi++)//q代表query
            {
                var cur = new Dictionary<int, (double Score, int FirstPos)>();

                foreach (int pos in allPos[qi])
                {
                    double bestScore = double.MinValue;
                    int bestFirst = 0;

                    foreach (var (prevPos, (prevScore, firstPos)) in prev)
                    {
                        if (prevPos >= pos) continue;   // 必须严格递增

                        int gap = pos - prevPos - 1;
                        double score = prevScore;

                        if (gap == 0)
                        {
                            // 连续匹配
                            score += 5;
                        }
                        else
                        {
                            // 间隔惩罚：用开方避免长间隔过度惩罚
                            score -= Math.Min(Math.Sqrt(gap) * 3.0, 18);

                            // 位置加分
                            if (IsWordBoundary(tOrig, pos))
                                score += 10;
                            else if (pos > 0 && char.IsLower(tOrig[pos - 1]) && char.IsUpper(tOrig[pos]))
                                score += 6;     // 驼峰边界
                            else
                                score += 2;     // 普通匹配
                        }

                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestFirst = firstPos;
                        }
                    }

                    if (bestScore > double.MinValue)
                        cur[pos] = (bestScore, bestFirst);
                }

                if (cur.Count == 0) return 0;   // 无合法递增路径
                prev = cur;
            }

            // ── 取最佳最终分 ──
            double bestFinal = double.MinValue;
            int bestFirstPos = 0, bestLastPos = 0;
            foreach (var (pos, (score, firstPos)) in prev)
            {
                if (score > bestFinal)
                {
                    bestFinal = score;
                    bestFirstPos = firstPos;
                    bestLastPos = pos;
                }
            }

            // ── 密度因子：匹配越紧凑，得分越高 ──
            int span = bestLastPos - bestFirstPos + 1;
            if (span > 0 && q.Length > 0)
            {
                double density = (double)q.Length / span;
                // 用 sqrt 让密度影响更平滑
                bestFinal = bestFinal * (0.5 + 0.5 * Math.Sqrt(density));
            }

            // 从串首开始的匹配额外加成
            if (bestFirstPos == 0)
                bestFinal *= 1.1;

            return Math.Max(0.5, Math.Min(94.9, bestFinal));
        }

        /// <summary>判断 target[pos] 是否位于「单词边界」。</summary>
        private static bool IsWordBoundary(string target, int pos)
        {
            if (pos <= 0) return true;
            char prev = target[pos - 1];
            char curr = target[pos];

            // 分隔符之后
            if (prev == ' ' || prev == '.' || prev == '_' || prev == '-' ||
                prev == '/' || prev == '\\' || prev == '(' || prev == '[' || prev == '{')
                return true;
            // 驼峰边界（小写 → 大写）
            if (char.IsLower(prev) && char.IsUpper(curr))
                return true;
            // 数字 → 非数字
            if (char.IsDigit(prev) && !char.IsDigit(curr))
                return true;

            return false;
        }

        private string BuildKey(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return keyword;

            keyword = keyword.Trim();

            var sb = new StringBuilder(keyword.Length * 2 + 1);
            sb.Append('*');
            foreach (char c in keyword)
            {
                sb.Append(c);
                sb.Append('*');
            }

            return sb.ToString();
        }
    }
}