using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reactive;
using System.Windows;
using QiDian.Models;

namespace QiDian
{
    public class SearchViewModel : ReactiveObject, IDisposable
    {
        private readonly EverythingSearchService _everything = new();
        private CancellationTokenSource? _searchCts;
        private System.Timers.Timer? _searchTimer;
        private string _searchKeyword = "";

        // 属性
        public string SearchKeyword
        {
            get => _searchKeyword;
            set
            {
                this.RaiseAndSetIfChanged(ref _searchKeyword, value);
                ScheduleSearch();
            }
        }

        [Reactive]
        public ObservableCollection<FileEntry> SearchResults { get; set; } = new();

        [Reactive]
        public string StatusText { get; set; } = "就绪";

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

            if (EverythingSearchService.IsAvailable())
            {
                EngineBadge = "Everything";
                StatusText = "Everything 引擎就绪，输入即搜";
            }
            else
            {
                EngineBadge = "Everything 不可用";
                StatusText = "请安装并运行 Everything";
            }
        }

        private void ScheduleSearch()
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
            var keyword = _searchKeyword;

            if (string.IsNullOrWhiteSpace(keyword))
            {
                SearchResults.Clear();
                StatusText = "⚡ Everything 就绪，输入即搜";
                return;
            }

            try
            {
                var sw = Stopwatch.StartNew();
                var results = await Task.Run(() => _everything.Search(keyword, 500), token);
                sw.Stop();

                if (!token.IsCancellationRequested)
                {
                    SearchResults.Clear();
                    foreach (var item in results)
                    {
                        SearchResults.Add(item);
                    }
                    StatusText = $"找到 {results.Count} 个结果 ({sw.ElapsedMilliseconds} ms)";
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
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