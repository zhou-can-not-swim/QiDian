using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using QiDian.Models;
using QiDian.Services;
using QiDian.Services.SearchLogic;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Zhou.LevelDB.Services;

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

        private const int RecentRowCapacity = 9;
        private const int RecentExpandedRows = 3;
        private const int RecentPageSize = RecentExpandedRows * RecentRowCapacity;

        /// <summary>图标批量更新的批次大小</summary>
        private const int BatchSize = 27;

        /// <summary>当前搜索的全部结果（折叠时界面只显示第一行）</summary>
        private List<FileEntry> _recentAll = new();

        /// <summary>展开时用于补齐 27 格、保持 3 行布局稳定的占位项（空路径，不可打开、不参与图标提取）</summary>
        private static readonly FileEntry PlaceholderEntry = new() { FullPath = "", FileName1 = "" };

        /// <summary>当前翻页页码（展开时生效，0 起；每页固定 RecentPageSize 个格子，内容整页替换）</summary>
        private int _recentPageIndex;

        /// <summary>正在预提取图标的来源集合（防重入：同一批数据只预提取一轮，数据变化后允许新一轮）</summary>
        private List<FileEntry>? _iconPrefetchSource;

        [Reactive]
        public int RecentTotalCount { get; set; }

        public SearchViewModel(EverythingSearchService everything)
        {
            _everything = everything;
            UnionSearchService.DataTransOk += UnionSearchService_DataTransOk;
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

            this.WhenAnyValue(x => x.SearchKeyword)
               .Throttle(new TimeSpan(100))
               .ObserveOn(RxApp.TaskpoolScheduler)
               .Subscribe(ScheduleSearch); //开始搜索

            this.WhenAnyValue(x => x.SearchKeyword)
               .Select(kw => !string.IsNullOrWhiteSpace(kw))
               .ObserveOn(RxApp.MainThreadScheduler)
               .Subscribe(searching => IsSearching = searching);

        }

        private void UnionSearchService_DataTransOk()
        {
            RxApp.MainThreadScheduler.Schedule(() =>
            {
                SearchRecent("");
                RebuildRecentItems();
            });
        }

        #region 打开文件
        private void OpenSelectedFile()
        {
            // 占位项（补齐 3 行布局的空格子）FullPath 为空，直接忽略
            if (SelectedFile == null || string.IsNullOrEmpty(SelectedFile.FullPath)) return;
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = SelectedFile.TruePath ?? SelectedFile.FullPath,
                    UseShellExecute = true
                });
            }
            catch (Win32Exception ex)
            {
                using (var scope = AppServiceLocator.ServiceProvider!.CreateScope())
                {
                    var ldb = scope.ServiceProvider.GetRequiredService<ILevelDBService>();
                    ldb.Delete($"st_{SelectedFile.FileName}");
                }
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开失败: {ex.Message}", "错误");
                return;
            }

            try {

                using (var scope = AppServiceLocator.ServiceProvider!.CreateScope())
                {
                    var ldb = scope.ServiceProvider.GetRequiredService<ILevelDBService>();
                    var f = new FileEntry
                    {
                        FullPath = SelectedFile.FullPath,
                        FileName1 = SelectedFile.FileName1,
                        TruePath = SelectedFile.TruePath,
                        Score = SelectedFile.Score + 10,
                        UsageCount = SelectedFile.UsageCount++,

                    };

                    ldb.Put($"st_{SelectedFile.FileName}", JsonSerializer.Serialize(f));
                }
                return;
            }
            catch(Exception ex)
            {
                MessageBox.Show($"数据库更新失败: {ex.Message}", "错误");
                return;
            }




        }
        public void OpenFile()
        {
            Task.Run(()=> OpenSelectedFile());
        }
        #endregion

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

        public void SearchRecent(string keyword)
        {
            using (var scope = AppServiceLocator.ServiceProvider!.CreateScope())
            {
                var _levelDb = scope.ServiceProvider.GetRequiredService<ILevelDBService>();
                var exist = _levelDb.GetByPrefix("st_");
                var source = exist.Where(e => !string.IsNullOrEmpty(e.Key)).ToDictionary(kv => kv.Key, kv => JsonSerializer.Deserialize<FileEntry>(kv.Value));//数据库中有的数据列表
                var result = source.Where(f => SearchCommonLogic.FuzzyMatch(f.Value!.FileName, keyword))//挑选出符合的key
                .Select(s => new FileEntry() { FileName1 = s.Key, FullPath = s.Value!.FullPath, TruePath = s.Value!.TruePath ,Score = s.Value!.Score,UsageCount = s.Value!.UsageCount })
                .OrderByDescending(s => s.Score)
                .ToList();

                _recentAll = result;
               RecentTotalCount = result.Count;
           }
  
        }

        private void SearhByEveryThing(string keyword)
        {
            string k = keyword.Split(" ")[0];
            var results = _everything.Search(k, RecentPageSize);

            _recentAll = results;
            RecentTotalCount = results.Count;

        }
        #endregion

        public void RebuildRecentItems()
        {
            _recentPageIndex = 0;
            FillCurrentPage();
            StartIconPrefetchAsync();
        }

        /// <summary>
        /// 填充当前页码的数据
        /// </summary>
        private void FillCurrentPage()
        {
            int pageSize = IsRecentExpanded ? RecentPageSize : RecentRowCapacity;  //27 或 9
            int start = IsRecentExpanded ? _recentPageIndex * RecentPageSize : 0;   //0
            int count = Math.Min(pageSize, Math.Max(0, _recentAll.Count - start)); //pageSize 27

            var page = new List<FileEntry>(pageSize);
            for (int i = 0; i < count; i++)
                page.Add(_recentAll[start + i]);

            // 展开时固定渲染 27 格（3行×9列）：数据不足用占位项补齐，
            // 保证翻到末页只剩几个文件时布局也不会塌缩成一行。
            // 无搜索结果（_recentAll 为空）时不补齐，让「未找到」空状态提示正常显示。
            if (IsRecentExpanded && _recentAll.Count > 0)
            {
                for (int i = count; i < pageSize; i++)
                    page.Add(PlaceholderEntry);
            }

            RecentItems = new ObservableCollection<FileEntry>(page);
        }


         #region 提取图标

        private void StartIconPrefetchAsync()
        {
            // 防重入：同一批数据只启动一轮预提取（数据变化后才允许新一轮）
            if (ReferenceEquals(_iconPrefetchSource, _recentAll)) return;

            var pending = _recentAll.Where(f => f.Icon == null).ToList();
            if (pending.Count == 0) return;

            _iconPrefetchSource = _recentAll;
            var dispatcher = Application.Current?.Dispatcher;

            var visiblePaths = RecentItems
                .Where(f => f.Icon == null)
                .Select(f => f.FullPath)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var firstPage = pending.Where(f => visiblePaths.Contains(f.FullPath)).ToList(); //9
            var rest = pending.Where(f => !visiblePaths.Contains(f.FullPath)).ToList();     //238个

            Task.Run(() =>
            {
                try
                {
                    ExtractIcons(dispatcher, firstPage, RecentRowCapacity);
                    ExtractIcons(dispatcher, rest, BatchSize);
                }
                finally
                {
                    dispatcher?.BeginInvoke(() => _iconPrefetchSource = null);
                }
            });
        }

        private static void ExtractIcons(Dispatcher? dispatcher, List<FileEntry> items, int batchSize)
        {
            if (items.Count == 0) return;

            Task.Run(() =>
            {
                var batch = new List<(FileEntry Item, ImageSource Icon)>(batchSize);
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                foreach (var item in items)
                {
                    batch.Add((item, QiDian.Converters.FilePathToIconConverter.ExtractIcon(item.TruePath??item.FullPath)));
                    if (batch.Count >= BatchSize)
                    {
                        FlushIconBatch(dispatcher, batch);
                        batch.Clear();
                    }
                }
                stopwatch.Stop();
                string v = stopwatch.ElapsedMilliseconds.ToString();
                if (batch.Count > 0){
                    FlushIconBatch(dispatcher, batch);
                }
            });
        }

        private static void FlushIconBatch(Dispatcher? dispatcher, List<(FileEntry Item, ImageSource Icon)> batch)
        {
            if (dispatcher == null)
            {
                foreach (var (item, icon) in batch)
                    item.Icon = icon;
                return;
            }

            try
            {
                dispatcher.Invoke(() =>
                {
                    foreach (var (item, icon) in batch)
                        item.Icon = icon;
                });
            }
            catch
            {
                // UI 线程关闭/繁忙期异常：忽略，不中断提取流程，避免漏掉本批图标
            }
        }

        #endregion

        //翻页
        public void TurnPage(int delta)
        {
            if (!IsRecentExpanded || delta == 0) return;
            if (_recentAll.Count <= RecentPageSize) return; // 一页能装下，无需翻页

            int maxPage = (_recentAll.Count - 1) / RecentPageSize; // 最后一页索引
            int newIndex = Math.Clamp(_recentPageIndex + delta, 0, maxPage);
            if (newIndex == _recentPageIndex) return; // 已在首页/末页

            _recentPageIndex = newIndex;
            FillCurrentPage();

            // 翻页后选中新页第一项，便于 Enter / 双击直接打开
            SelectedFile = RecentItems.FirstOrDefault();

            // 新页的图标异步提取（已提取过的项直接命中缓存）
            StartIconPrefetchAsync();
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
