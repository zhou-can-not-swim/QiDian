using QiDian.Models;
using System.IO;

namespace QiDian.Services.SearchLogic
{
    public class StaticStartMenuFiles
    {
        public static Dictionary<string,string> CommonStartMenuFiles;
        public static Dictionary<string, string> UserStartMenuFiles;
        public static List<FileEntry> UnionFiles;

        public static Task InitStartMenuFiles()
        {
            var startMenuDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs");

            var commonStartMenuDir = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu), "Programs"
            );


            UserStartMenuFiles = Directory.EnumerateFiles(startMenuDir, "*lnk", SearchOption.AllDirectories)
                .Select(f => new FileEntry { FullPath = f })
                .ToDictionary(u => u.FileName, u => u.FullPath);

            CommonStartMenuFiles = Directory.EnumerateFiles(commonStartMenuDir, "*lnk", SearchOption.AllDirectories)
                .Select(f => new FileEntry { FullPath = f })
                .GroupBy(f => f.FileName)
                .Where(g => g.Count() == 1)
                .SelectMany(g => g)
                .ToDictionary(u => u.FileName, u => u.FullPath);

            UnionFiles = UserStartMenuFiles
                .UnionBy(CommonStartMenuFiles, kvp => kvp.Key)
                //.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                .Select(f => new FileEntry
                {
                    FullPath = f.Value
                })
                .ToList();



            return Task.CompletedTask;
        }
    }
}
