using QiDian.Models;
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

namespace QiDian
{
    public class SearchViewModel : ReactiveObject, IDisposable
    {
        private readonly EverythingSearchService _everything = new();
        private CancellationTokenSource? _searchCts;
        private System.Timers.Timer? _searchTimer;

        [Reactive]
        public string SearchKeyword { get; set; } = "";

        // ==================== 界面两层结构 ====================

        /// <summary>
        /// 第一层：最近使用（横向卡片、可折叠、默认折叠）。
        /// 搜索关键词变化时会动态过滤（当前为模拟数据，接入真实搜索逻辑后替换数据源即可）。
        /// </summary>
        [Reactive]
        public ObservableCollection<FileEntry> RecentItems { get; set; } = new();

        /// <summary>
        /// 第二层：预留的固定内容（不随搜索变化，与第一层互不相通）。
        /// 目前用模拟的「功能区」入口占位，后续可自行替换为任意内容。
        /// </summary>
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
        public string StatusText { get; set; } = "";

        [Reactive]
        public string EngineBadge { get; set; } = "";

        [Reactive]
        public FileEntry? SelectedFile { get; set; }

        // 命令
        public ReactiveCommand<Unit, Unit> OpenFileCommand { get; }
        public ReactiveCommand<Unit, Unit> OpenFileLocationCommand { get; }
        public ReactiveCommand<Unit, Unit> CopyPathCommand { get; }
        public ReactiveCommand<Unit, Unit> RunAsAdminCommand { get; }

        // ==================== 模拟数据（仅用于界面演示） ====================
        // TODO(真实搜索)：接入真实搜索逻辑后，删除本区域及 FilterMockRecent，
        // 并把 PerformSearch 的数据源换回 _everything.Search(keyword, 500)。

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

        /// <summary>模拟的「最近使用」数据池（用系统真实存在的程序，图标能正常提取）</summary>
        private static readonly string[] MockRecentPaths =
        {
            @"C:\Windows\System32\notepad.exe",
            @"C:\Windows\System32\mspaint.exe",
            @"C:\Windows\System32\calc.exe",
            @"C:\Windows\System32\cmd.exe",
            @"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe",
            @"C:\Windows\System32\regedit.exe",
            @"C:\Windows\System32\control.exe",
            @"C:\Windows\System32\taskmgr.exe",
            @"C:\Windows\System32\explorer.exe",
            @"C:\Windows\System32\charmap.exe",
            @"C:\Windows\System32\dxdiag.exe",
            @"C:\Windows\System32\msconfig.exe",
            @"C:\Windows\System32\osk.exe",
            @"C:\Windows\System32\winver.exe",
            @"C:\Windows\System32\write.exe",
        };

        private readonly List<FileEntry> _mockRecentPool;

        /// <summary>折叠时第一行可容纳的图标数量（窗口 800px 宽，约 8 个）</summary>
        private const int RecentRowCapacity = 8;

        /// <summary>当前搜索的全部结果（折叠时界面只显示第一行）</summary>
        private List<FileEntry> _recentAll = new();

        /// <summary>全部结果数量（标题栏计数显示）</summary>
        [Reactive]
        public int RecentTotalCount { get; set; }

        public SearchViewModel()
        {
            _mockRecentPool = BuildMockRecentPool();

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
            OpenFileLocationCommand = ReactiveCommand.Create(OpenSelectedFileLocation);
            CopyPathCommand = ReactiveCommand.Create(CopySelectedPath);
            RunAsAdminCommand = ReactiveCommand.Create(RunSelectedAsAdmin);

            EngineBadge = "演示模式 · 模拟数据";
            StatusText = "输入关键词，动态过滤「最近使用」（模拟数据）";

            this.WhenAnyValue(x => x.SearchKeyword)
               .Throttle(TimeSpan.FromMilliseconds(300))
               .ObserveOn(RxApp.TaskpoolScheduler)
               .Subscribe(ScheduleSearch);

            // 关键词一变非空就立即标记"搜索中"，避免上一轮"未找到"在等待搜索期间闪现
            this.WhenAnyValue(x => x.SearchKeyword)
               .Select(kw => !string.IsNullOrWhiteSpace(kw))
               .ObserveOn(RxApp.MainThreadScheduler)
               .Subscribe(searching => IsSearching = searching);
        }

        /// <summary>
        /// 重置到初始状态（窗口每次打开时调用）：只显示搜索栏 + 两层折叠标题栏。
        /// 折叠/展开状态保留用户上次的选择。
        /// </summary>
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
            StatusText = "输入关键词，动态过滤「最近使用」（模拟数据）";
        }

        // ==================== 模拟搜索逻辑 ====================

        private static List<FileEntry> BuildMockRecentPool()
        {
            var pool = new List<FileEntry>();
            for (int i = 0; i < MockRecentPaths.Length; i++)
            {
                var path = MockRecentPaths[i];
                if (File.Exists(path))
                {
                    pool.Add(CreateMockEntry(path, i));
                }
            }

            // 兜底：万一系统没有上述文件，也保证界面有内容可展示（图标回退为默认）
            if (pool.Count == 0)
            {
                for (int i = 0; i < MockRecentPaths.Length; i++)
                {
                    pool.Add(CreateMockEntry(MockRecentPaths[i], i));
                }
            }

            return pool;
        }

        private static FileEntry CreateMockEntry(string path, int index)
        {
            var info = new FileInfo(path);
            return new FileEntry
            {
                FullPath = path,
                Size = info.Exists ? info.Length : 0,
                LastModified = info.Exists ? info.LastWriteTime : DateTime.Now,
                UsageCount = 15 - index,               // 模拟使用频率，越靠前越高
                LastUsed = DateTime.Now.AddMinutes(-(index + 1) * 7),
            };
        }

        /// <summary>
        /// 模拟过滤：把全部匹配结果写入 _recentAll（关键词为空时展示最近的前 12 个）。
        /// TODO(真实搜索)：接入真实搜索逻辑时，把此处替换为 Everything 等引擎的查询。
        /// 本方法只更新数据源，界面显示由 RebuildRecentItems 在 UI 线程刷新。
        /// </summary>
        private void SearchRecent(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                _recentAll = _mockRecentPool.Take(12).ToList();
            else
                _recentAll = _mockRecentPool
                    .Where(f => f.FileName.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                                || f.FullPath.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    .Take(50)
                    .ToList();

            RecentTotalCount = _recentAll.Count;
        }

        private void RebuildRecentItems()
        {
            RecentItems = new ObservableCollection<FileEntry>(
                IsRecentExpanded ? _recentAll : _recentAll.Take(RecentRowCapacity));
        }

        public void ToggleRecentExpanded()
        {
            IsRecentExpanded = !IsRecentExpanded;
            RebuildRecentItems();
        }

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
                    StatusText = "输入关键词，动态过滤「最近使用」（模拟数据）";
                });
                return;
            }

            IsSearching = true;
            try
            {
                var sw = Stopwatch.StartNew();
                // TODO(真实搜索)：把数据源从 SearchRecent 换回 _everything.Search(keyword, 500)
                await Task.Run(() => SearchRecent(keyword), token);
                sw.Stop();

                if (!token.IsCancellationRequested)
                {
                    RxApp.MainThreadScheduler?.Schedule(() =>
                    {
                        RebuildRecentItems();
                        HasSearched = true;
                        IsSearching = false;
                        StatusText = $"找到 {_recentAll.Count} 个结果 ({sw.ElapsedMilliseconds} ms)";
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
                StatusText = $"搜索出错: {ex.Message}";
            }
        }

        // ==================== 打开动作（当前为模拟，接入真实逻辑后替换） ====================

        /// <summary>模拟打开选中项：只更新状态栏提示，不真正启动程序。</summary>
        public void OpenSelectedFileMock()
        {
            if (SelectedFile == null) return;
            StatusText = $"（模拟）打开: {SelectedFile.FullPath}";
        }

        private void OpenSelectedFile()
        {
            if (SelectedFile == null) return;
            RunFile(SelectedFile, false);
        }

        private void OpenSelectedFileLocation()
        {
            if (SelectedFile == null) return;
            OpenInExplorer(SelectedFile);
        }

        private void CopySelectedPath()
        {
            if (SelectedFile == null) return;
            CopyToClipboard(SelectedFile);
        }

        private void RunSelectedAsAdmin()
        {
            if (SelectedFile == null) return;
            RunFile(SelectedFile, true);
        }

        private void OpenInExplorer(FileEntry file)
        {
            try
            {
                Process.Start("explorer.exe", $"/select,\"{file.FullPath}\"");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "错误");
            }
        }

        private void CopyToClipboard(FileEntry file)
        {
            Clipboard.SetText(file.FullPath);
            StatusText = $"已复制: {file.FullPath}";
        }

        private void RunFile(FileEntry file, bool runAsAdmin)
        {
            try
            {
                var info = new ProcessStartInfo
                {
                    FileName = file.FullPath,
                    UseShellExecute = true
                };
                if (runAsAdmin) info.Verb = "runas";
                Process.Start(info);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "错误");
            }
        }

        public void Dispose()
        {
            _searchTimer?.Dispose();
            _searchCts?.Dispose();
        }
    }
}
