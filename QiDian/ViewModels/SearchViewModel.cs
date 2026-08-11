using QiDian.Models;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows;
using System.Reactive.Concurrency;

namespace QiDian
{
    public class SearchViewModel : ReactiveObject, IDisposable
    {
        private readonly EverythingSearchService _everything = new();
        private CancellationTokenSource? _searchCts;
        private System.Timers.Timer? _searchTimer;

        [Reactive]
        public string SearchKeyword { get; set; } = "";

        [Reactive]
        public ObservableCollection<FileEntry> Results { get; set; } = new();

        /// <summary>是否已执行过搜索（区分「未搜索」与「搜索无结果」两种界面状态）</summary>
        [Reactive]
        public bool HasSearched { get; set; }

        /// <summary>是否有搜索正在进行（搜索期间不显示"未找到"，避免闪烁）</summary>
        [Reactive]
        public bool IsSearching { get; set; }

        [Reactive]
        public string StatusText { get; set; } = "";//keyword

        [Reactive]
        public string EngineBadge { get; set; } = "";

        [Reactive]
        public FileEntry? SelectedFile { get; set; }

        // 命令
        public ReactiveCommand<Unit, Unit> OpenFileCommand { get; }
        public ReactiveCommand<Unit, Unit> OpenFileLocationCommand { get; }
        public ReactiveCommand<Unit, Unit> CopyPathCommand { get; }
        public ReactiveCommand<Unit, Unit> RunAsAdminCommand { get; }

        public SearchViewModel()
        {
            // 初始化命令
            OpenFileCommand = ReactiveCommand.Create(OpenSelectedFile);
            OpenFileLocationCommand = ReactiveCommand.Create(OpenSelectedFileLocation);
            CopyPathCommand = ReactiveCommand.Create(CopySelectedPath);
            RunAsAdminCommand = ReactiveCommand.Create(RunSelectedAsAdmin);

            EngineBadge = EverythingSearchService.IsAvailable() ? "Everything" : "Everything 不可用";
            StatusText = EverythingSearchService.IsAvailable() ? "Everything 引擎就绪，输入即搜" : "请安装并运行 Everything";

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
        /// 重置到初始状态（窗口每次打开时调用）：只显示搜索栏，不显示结果列表/空状态。
        /// </summary>
        public void Reset()
        {
            _searchCts?.Cancel();
            _searchTimer?.Stop();
            SearchKeyword = "";
            Results = new ObservableCollection<FileEntry>();
            HasSearched = false;
            IsSearching = false;
            StatusText = EverythingSearchService.IsAvailable()
                ? "Everything 引擎就绪，输入即搜"
                : "请安装并运行 Everything";
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
                    Results = new ObservableCollection<FileEntry>();
                    HasSearched = false;
                    IsSearching = false;
                    StatusText = "⚡ Everything 就绪，输入即搜";
                });
                return;

            }

            IsSearching = true;
            try
            {
                var sw = Stopwatch.StartNew();
                var results = await Task.Run(() => _everything.Search(keyword, 500), token);
                sw.Stop();

                if (!token.IsCancellationRequested)
                {
                    RxApp.MainThreadScheduler?.Schedule(() =>
                    {
                        Results = new ObservableCollection<FileEntry>(results);
                        HasSearched = true;
                        IsSearching = false;
                        StatusText = $"找到 {results.Count} 个结果 ({sw.ElapsedMilliseconds} ms)";
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