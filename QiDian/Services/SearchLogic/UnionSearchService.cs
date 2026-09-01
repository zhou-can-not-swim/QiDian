using Microsoft.Extensions.DependencyInjection;
using QiDian.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using Zhou.LevelDB.Services;

namespace QiDian.Services.SearchLogic
{
    /// <summary>
    /// start Menu 和 LevelDB数据并集 
    /// </summary>
    public class UnionSearchService
    {
        public static Dictionary<string, string> CommonStartMenuFiles;
        public static Dictionary<string, string> UserStartMenuFiles;
        public static Dictionary<string, FileEntry> UnionFiles;
        public const string pre = "st";
        public UnionSearchService()
        {
        }

        public static Task InitStartMenuFiles()
        {
            var startMenuDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs");

            var commonStartMenuDir = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu), "Programs"
            );


            UserStartMenuFiles = Directory.EnumerateFiles(startMenuDir, "*lnk", SearchOption.AllDirectories)
                .Select(f => new FileEntry { FullPath = f })
                .GroupBy(f => f.FileName)
                .Select(g => g.First())
                .ToDictionary(u => u.FileName, u => u.FullPath);

            CommonStartMenuFiles = Directory.EnumerateFiles(commonStartMenuDir, "*lnk", SearchOption.AllDirectories)
                .Select(f => new FileEntry { FullPath = f })
                .GroupBy(f => f.FileName)
                .Select(g=>g.First())
                .ToDictionary(u => u.FileName, u => u.FullPath);


            UnionFiles = UserStartMenuFiles
                .Concat(CommonStartMenuFiles)
                .GroupBy(u => u.Key)
                .Select(u => u.First())
                .ToDictionary(kv => kv.Key, kv => new FileEntry() { FileName1 = kv.Key, FullPath = kv.Value });
            UnionSearchService.UnionMenuWithDB();

            return Task.CompletedTask;
        }

        /// <summary>
        /// 只做新增项，只要u中有的key levelDB中没有，就添加
        /// </summary>
        public static void UnionMenuWithDB()
        {
            using (var scope = AppServiceLocator.ServiceProvider!.CreateScope())
            {
                var _levelDb = scope.ServiceProvider.GetRequiredService<ILevelDBService>();
                var exist = _levelDb.GetByPrefix(pre);

                var peddings = UnionFiles.Where(kvp => !exist.ContainsKey(kvp.Key))
                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                _levelDb.Batch(batch =>
                {
                    foreach (var kvp in peddings)
                    {
                        var jsonValue = JsonSerializer.Serialize(kvp.Value);
                        batch.Put($"st_{kvp.Key}", jsonValue);
                    }
                });
            }
        }
    }
}
