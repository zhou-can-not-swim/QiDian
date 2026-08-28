using DynamicData;
using QiDian.Models;
using QiDian.Services;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Shapes;

namespace QiDian
{
    public class SearchViewModel : ReactiveObject, IDisposable
    {
        private EverythingSearchService _everything;
        private CancellationTokenSource? _searchCts;
        private System.Timers.Timer? _searchTimer;

        [Reactive]
        public string SearchKeyword { get; set; } = "";

        [Reactive]
        public ObservableCollection<FileEntry> RecentItems { get; set; } = new();

        //预留
        [Reactive]
        public ObservableCollection<FixedEntry> FixedItems { get; set; } = new();

        /// <summary>第一层是否展开（默认折叠）</summary>
        [Reactive]
        public bool IsRecentExpanded { get; set; }

        /// <summary>第二层是否展开（默认折叠）</summary>
        [Reactive]
        public bool IsFixedExpanded { get; set; }

        /// <summary>是否已执行过搜索（区分「未搜索」与「搜索无结果」两种界面状态）</summary>
        [Reactive]
        public bool HasSearched { get; set; }

        /// <summary>是否有搜索正在进行（搜索期间不显示"未找到"，避免闪烁）</summary>
        [Reactive]
        public bool IsSearching { get; set; }

        [Reactive]
        public FileEntry? SelectedFile { get; set; }

        // 命令
        public ReactiveCommand<Unit, Unit> OpenFileCommand { get; }
        public ReactiveCommand<Unit, Unit> OpenFileLocationCommand { get; }
        public ReactiveCommand<Unit, Unit> CopyPathCommand { get; }
        public ReactiveCommand<Unit, Unit> RunAsAdminCommand { get; }

        /// <summary>第二层预留入口的占位模型</summary>
        public sealed class FixedEntry
        {
            public FixedEntry(string name, string emoji, string description)
            {
                Name = name;
                Emoji = emoji;
                Description = description;
            }

            public string Name { get; }
            public string Emoji { get; }
            public string Description { get; }
        }


        /// <summary>折叠时第一行可容纳的图标数量（窗口 800px 宽，约 8 个）</summary>
        private const int RecentRowCapacity = 9;

        /// <summary>当前搜索的全部结果（折叠时界面只显示第一行）</summary>
        private List<FileEntry> _recentAll = new();

        /// <summary>全部结果数量（标题栏计数显示）</summary>
        [Reactive]
        public int RecentTotalCount { get; set; }

        public SearchViewModel(EverythingSearchService everything)
        {
            _everything = everything;
            // 第二层：预留固定内容（演示占位，后续自行替换）
            FixedItems = new ObservableCollection<FixedEntry>
            {
                new("设置", "⚙️", "打开应用设置"),
                new("剪贴板历史", "📋", "最近复制的文本与图片"),
                new("翻译", "🌐", "输入文字快速翻译"),
                new("取色器", "🎨", "拾取屏幕任意颜色"),
                new("插件中心", "🧩", "浏览并安装插件"),
                new("文件快传", "📤", "跨设备传输文件"),
                new("备忘录", "📝", "随手记录想法"),
                new("二维码", "🔳", "生成/识别二维码"),
            };

            // 初始化命令
            OpenFileCommand = ReactiveCommand.Create(OpenSelectedFile);
            //OpenFileLocationCommand = ReactiveCommand.Create(OpenSelectedFileLocation);
            //CopyPathCommand = ReactiveCommand.Create(CopySelectedPath);
            //RunAsAdminCommand = ReactiveCommand.Create(RunSelectedAsAdmin);

            // 预加载第一层数据：窗口显示前 RecentItems 已就绪，避免打开后有空白等待
            SearchRecent("");
            RebuildRecentItems();

            this.WhenAnyValue(x => x.SearchKeyword)
               .Throttle(new TimeSpan(100))
               .ObserveOn(RxApp.TaskpoolScheduler)
               .Subscribe(ScheduleSearch); //开始搜索

            this.WhenAnyValue(x => x.SearchKeyword)
               .Select(kw => !string.IsNullOrWhiteSpace(kw))
               .ObserveOn(RxApp.MainThreadScheduler)
               .Subscribe(searching => IsSearching = searching);

        }

        private void OpenSelectedFile()
        {
            if (SelectedFile == null) return;
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = SelectedFile.FullPath,
                    UseShellExecute = true  // 使用系统默认方式打开
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开失败: {ex.Message}", "错误");
            }
        }
        public void OpenFile()
        {
            Task.Run(()=> OpenSelectedFile());
        }
        #region search
        private void ScheduleSearch(string obj)
        {
            _searchTimer?.Stop();
            _searchTimer?.Dispose();
            _searchTimer = new System.Timers.Timer(150) { AutoReset = false };
            _searchTimer.Elapsed += (_, _) =>
                Application.Current?.Dispatcher.Invoke(PerformSearch);
            _searchTimer.Start();
        }

        private async void PerformSearch()
        {
            _searchCts?.Cancel();
            _searchCts?.Dispose();
            _searchCts = new CancellationTokenSource();
            var token = _searchCts.Token;
            var keyword = SearchKeyword;

            if (string.IsNullOrWhiteSpace(keyword))
            {
                RxApp.MainThreadScheduler.Schedule(() =>
                {
                    SearchRecent("");
                    RebuildRecentItems();
                    HasSearched = false;
                    IsSearching = false;
                });
                return;
            }

            IsSearching = true;
            try
            {
                var sw = Stopwatch.StartNew();
                if (keyword.EndsWith("-d"))
                {
                    await Task.Run(() => { SearhByEveryThing(keyword); }, token);
                }
                else
                {
                    await Task.Run(() => { SearchRecent(keyword); }, token);
                }


                sw.Stop();

                if (!token.IsCancellationRequested)
                {
                    RxApp.MainThreadScheduler?.Schedule(() =>
                    {
                        RebuildRecentItems();
                        HasSearched = true;
                        IsSearching = false;
                    });
                }
            }
            catch (OperationCanceledException)
            {
                IsSearching = false;
            }
            catch (Exception ex)
            {
                IsSearching = false;
            }
        }

        private void SearchRecent(string keyword)
        {
            string startMenu = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu);
            string programsDir = System.IO.Path.Combine(startMenu, "Programs");

            var result = Directory.EnumerateFiles(programsDir, "*lnk", SearchOption.AllDirectories)
                .Where(f => string.IsNullOrWhiteSpace(keyword)
                            || System.IO.Path.GetFileNameWithoutExtension(f)
                                .Contains(keyword, StringComparison.OrdinalIgnoreCase))
                .Select(f => new FileEntry { FullPath = f })
                .ToList();

            _recentAll = result;
            RecentTotalCount = result.Count;
        }

        private void SearhByEveryThing(string keyword)
        {
            string k = keyword.Split(" ")[0];
            var results = _everything.SearchByEveryThing(k,100);

            _recentAll = results;
        }
        #endregion

        public void Reset()
        {

            _searchCts?.Cancel();
            _searchTimer?.Stop();
            SearchKeyword = "";
            SearchRecent("");
            RebuildRecentItems();
            HasSearched = false;
            IsSearching = false;
            // 每次打开回到默认状态：第一层只显示第一行，第二层折叠
            IsRecentExpanded = false;
            IsFixedExpanded = false;
        }

        private void RebuildRecentItems()
        {
            RecentItems = new ObservableCollection<FileEntry>(
                IsRecentExpanded ? _recentAll : _recentAll.Take(RecentRowCapacity));

            // 自动选中第一项，保证 Enter / 双击可直接打开（否则 SelectedItem 为 null 时无反应）
            if (SelectedFile!=null)
            {

            }
            else
            {
                SelectedFile = RecentItems.FirstOrDefault();
            }

        }

        public void ToggleRecentExpanded()
        {
            IsRecentExpanded = !IsRecentExpanded;
            RebuildRecentItems();
        }




       

        public void Dispose()
        {
            _searchTimer?.Dispose();
            _searchCts?.Dispose();
        }


    }
}
