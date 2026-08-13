using LibVLCSharp.Shared;
using MediaPlayer = LibVLCSharp.Shared.MediaPlayer;
using ReactiveUI;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media;
using QiDian.Nav.DM;
using QiDian.Nav.DM.ViewModels;
using QiDian.Nav.DM.Model.Dtos;

namespace QiDian.Nav.DM.Views;

/// <summary>
/// 全屏播放页：占满整个导航内容区，右下角抽屉列出该番剧全部剧集可自由切换。
/// 支持本地 mp4 与网页播放源（m3u8 / mp4）。播放进度按剧集自动记录到
/// %AppData%\QiDian\dm_playback.json，下次打开同一集自动续播。
/// </summary>
public partial class DMPlayerView : UserControl, IViewFor<DMPlayerViewModel>
{
    private readonly LibVLC _libVLC;
    private readonly MediaPlayer _mediaPlayer;

    private string _currentSource = "";
    private long _pendingResumeMs;          // 待定位的续播位置（毫秒）
    private bool _isSeeking;                // 用户正在拖动进度条
    private bool _isUpdatingSlider;         // 正在从播放器同步进度条，避免互相触发
    private DateTime _lastSaveTime = DateTime.MinValue;
    private IDisposable? _selectionSubscription;
    private bool _disposed;                 // Unloaded 后播放器已释放，丢弃仍在队列里的事件回调

    private bool _drawerOpen;

    public DMPlayerView()
    {
        InitializeComponent();

        _libVLC = VlcProvider.LibVLC;
        _mediaPlayer = new MediaPlayer(_libVLC);
        Player.MediaPlayer = _mediaPlayer;

        // LibVLCSharp 的媒体事件在 libvlc 后台线程上触发，而处理器会操作 WPF 控件，
        // 必须切回 UI 线程再回调，否则抛"调用线程无法访问此对象，因为另一个线程拥有该对象"。
        _mediaPlayer.TimeChanged += (s, e) => DispatchToUi(() => MediaPlayer_TimeChanged(s, e));
        _mediaPlayer.Playing += (s, e) => DispatchToUi(() => MediaPlayer_Playing(s, e));
        _mediaPlayer.LengthChanged += (s, e) => DispatchToUi(() => MediaPlayer_LengthChanged(s, e));
        _mediaPlayer.EndReached += (s, e) => DispatchToUi(() => MediaPlayer_EndReached(s, e));
        _mediaPlayer.EncounteredError += (s, e) => DispatchToUi(() => MediaPlayer_EncounteredError(s, e));

        Loaded += DMPlayerView_Loaded;
        Unloaded += DMPlayerView_Unloaded;
    }

    /// <summary>把 libvlc 后台线程的事件回调切回 UI 线程执行；本身就在 UI 线程（如手动调用）则直接执行。</summary>
    private void DispatchToUi(Action action)
    {
        if (_disposed) return;

        if (Dispatcher.CheckAccess())
            action();
        else
            RxApp.MainThreadScheduler.Schedule(() => { if (!_disposed) action(); });
    }

    #region 生命周期

    private void DMPlayerView_Loaded(object sender, RoutedEventArgs e)
    {
        if (_selectionSubscription != null) return;

        // 当前剧集变化时切换播放（含抽屉里点选集）
        _selectionSubscription = this.WhenAnyValue(x => x.ViewModel!.CurrentEpisode)
            .Where(x => x != null)
            .Subscribe(ep => LoadEpisode(ep));
    }

    private void DMPlayerView_Unloaded(object sender, RoutedEventArgs e)
    {
        // 先置位：丢弃已排队等待 UI 线程执行的回调，避免访问已释放的播放器
        _disposed = true;

        _selectionSubscription?.Dispose();
        _selectionSubscription = null;

        // 离开页面时保存当前进度并释放播放器
        SaveCurrentPosition();
        _mediaPlayer.Stop();
        Player.MediaPlayer = null;
        _mediaPlayer.Dispose();
    }

    #endregion

    #region 播放控制

    private void LoadEpisode(EpisodeItem? episode)
    {
        if (episode == null) return;

        // 先保存上一集的进度
        SaveCurrentPosition();

        if (string.IsNullOrWhiteSpace(episode.Source))
        {
            // 未提供播放源（示例数据/爬虫未填）：停在提示态
            _currentSource = "";
            PlayerHint.Visibility = Visibility.Visible;
            PlayerHint.Text = "暂无播放源，接入爬虫后即可播放";
            PlayPauseButton.Content = "▶ 播放";
            if (ViewModel != null)
                ViewModel.StatusMessage = $"「{episode.Title}」暂无播放源";
            return;
        }

        _currentSource = episode.Source;
        _pendingResumeMs = (long)(PlaybackPositionStore.GetResumeSeconds(episode.Source) * 1000);
        _lastSaveTime = DateTime.MinValue;

        PlayerHint.Visibility = Visibility.Visible;
        PlayerHint.Text = $"正在加载 {episode.Title}…";
        PlayPauseButton.Content = "⏸ 暂停";

        _mediaPlayer.Stop();

        try
        {
            using var media = new Media(_libVLC, new Uri(episode.Source));
            _mediaPlayer.Play(media);
        }
        catch (Exception ex)
        {
            PlayPauseButton.Content = "▶ 播放";
            PlayerHint.Text = "无法打开视频源";
            if (ViewModel != null)
                ViewModel.StatusMessage = $"无法打开视频源：{ex.Message}";
            return;
        }

        if (ViewModel != null)
            ViewModel.StatusMessage = $"正在加载：{episode.Title}";
    }

    /// <summary>播放真正开始/暂停后隐藏提示文字</summary>
    private void MediaPlayer_Playing(object? sender, EventArgs e)
    {
        PlayerHint.Visibility = Visibility.Collapsed;
        TryApplyResume();
    }

    /// <summary>时长就绪时同步进度条；若播放开始时时长还没就绪，借此机会补定位</summary>
    private void MediaPlayer_LengthChanged(object? sender, MediaPlayerLengthChangedEventArgs e)
    {
        SeekSlider.Maximum = e.Length / 1000.0;
        TryApplyResume();
    }

    private void TryApplyResume()
    {
        if (_pendingResumeMs <= 0) return;
        if (_mediaPlayer.Length <= 0) return;
        if (!_mediaPlayer.IsPlaying) return;

        // 上次已看到结尾附近 → 从头播
        if (_mediaPlayer.Length - _pendingResumeMs <= (long)(PlaybackPositionStore.EndThresholdSeconds * 1000))
        {
            _pendingResumeMs = 0;
            return;
        }

        _mediaPlayer.Time = _pendingResumeMs;
        _pendingResumeMs = 0;

        if (ViewModel != null)
            ViewModel.StatusMessage = $"已自动续播到 {DMPlayerViewModel.FormatSeconds(_mediaPlayer.Time / 1000.0)}，继续观看";
    }

    /// <summary>播放进度更新：刷新进度条与时间文本，每 5 秒保存一次</summary>
    private void MediaPlayer_TimeChanged(object? sender, MediaPlayerTimeChangedEventArgs e)
    {
        if (ViewModel == null) return;

        var timeSec = e.Time / 1000.0;
        var lengthSec = _mediaPlayer.Length > 0 ? _mediaPlayer.Length / 1000.0 : 0;

        if (!_isSeeking && lengthSec > 0)
        {
            _isUpdatingSlider = true;
            SeekSlider.Maximum = lengthSec;
            SeekSlider.Value = timeSec;
            _isUpdatingSlider = false;
        }

        ViewModel.PositionText = $"{DMPlayerViewModel.FormatSeconds(timeSec)} / {DMPlayerViewModel.FormatSeconds(lengthSec)}";

        // 每 5 秒保存一次进度，保证异常退出也只丢几秒
        if ((DateTime.Now - _lastSaveTime).TotalSeconds >= 5)
        {
            _lastSaveTime = DateTime.Now;
            SaveCurrentPosition();
        }
    }

    private void MediaPlayer_EndReached(object? sender, EventArgs e)
    {
        if (!string.IsNullOrEmpty(_currentSource))
            PlaybackPositionStore.MarkCompleted(_currentSource);

        PlayPauseButton.Content = "▶ 播放";

        if (ViewModel != null)
        {
            ViewModel.PositionText = "00:00 / 00:00";
            ViewModel.StatusMessage = "本集已播放完毕";
            if (ViewModel.CurrentEpisode != null)
                ViewModel.CurrentEpisode.ProgressText = "已看完";
        }
    }

    private void MediaPlayer_EncounteredError(object? sender, EventArgs e)
    {
        PlayPauseButton.Content = "▶ 播放";
        PlayerHint.Visibility = Visibility.Visible;
        PlayerHint.Text = "播放失败，请检查视频源是否有效";

        if (ViewModel != null)
            ViewModel.StatusMessage = $"播放失败，请检查视频源是否有效：{_currentSource}";
    }

    private void SeekSlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        => _isSeeking = true;

    private void SeekSlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        => _isSeeking = false;

    private void SeekSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isUpdatingSlider) return;
        if (_mediaPlayer.Length <= 0) return;

        _mediaPlayer.Time = (long)(e.NewValue * 1000);
    }

    private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_currentSource)) return;

        if (_mediaPlayer.IsPlaying)
        {
            _mediaPlayer.Pause();
            PlayPauseButton.Content = "▶ 播放";
            SaveCurrentPosition();
        }
        else
        {
            _mediaPlayer.Play();
            PlayPauseButton.Content = "⏸ 暂停";
        }
    }

    /// <summary>保存当前剧集播放进度（已看到结尾附近则不保存，下次从头播）</summary>
    private void SaveCurrentPosition()
    {
        if (string.IsNullOrEmpty(_currentSource)) return;
        if (_mediaPlayer.Length <= 0) return;

        var pos = _mediaPlayer.Time / 1000.0;
        var dur = _mediaPlayer.Length / 1000.0;
        if (pos <= 0 || dur <= 0) return;
        if (pos >= dur - PlaybackPositionStore.EndThresholdSeconds) return;

        PlaybackPositionStore.SavePosition(_currentSource, pos, dur);

        if (ViewModel?.CurrentEpisode != null)
            ViewModel.RefreshProgressText(ViewModel.CurrentEpisode);
    }

    #endregion

    #region 抽屉

    private void DrawerToggle_Click(object sender, RoutedEventArgs e) => ToggleDrawer();

    private void DrawerClose_Click(object sender, RoutedEventArgs e) => CloseDrawer();

    private void Scrim_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => CloseDrawer();

    private void ToggleDrawer()
    {
        if (_drawerOpen) CloseDrawer();
        else OpenDrawer();
    }

    private void OpenDrawer()
    {
        _drawerOpen = true;
        DrawerRoot.Visibility = Visibility.Visible;

        var scrimAnim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180));
        Scrim.BeginAnimation(OpacityProperty, scrimAnim);

        var slideAnim = new DoubleAnimation(320, 0, TimeSpan.FromMilliseconds(220))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
        };
        DrawerTranslate.BeginAnimation(TranslateTransform.XProperty, slideAnim);
    }

    private void CloseDrawer()
    {
        if (!_drawerOpen) return;
        _drawerOpen = false;

        var scrimAnim = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(180));
        Scrim.BeginAnimation(OpacityProperty, scrimAnim);

        var slideAnim = new DoubleAnimation(0, 320, TimeSpan.FromMilliseconds(220))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn },
        };
        slideAnim.Completed += (_, _) => DrawerRoot.Visibility = Visibility.Collapsed;
        DrawerTranslate.BeginAnimation(TranslateTransform.XProperty, slideAnim);
    }

    #endregion

    #region 导航 / 键盘

    private void BackButton_Click(object sender, RoutedEventArgs e) => ViewModel?.Back();

    /// <summary>Esc：关闭抽屉，再按一次返回详情页；空格：播放/暂停</summary>
    private void Root_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            if (_drawerOpen)
                CloseDrawer();
            else
                ViewModel?.Back();
            e.Handled = true;
        }
        else if (e.Key == Key.Space && !_drawerOpen)
        {
            PlayPauseButton_Click(sender, e);
            e.Handled = true;
        }
    }

    #endregion

    #region ViewModel

    public DMPlayerViewModel? ViewModel
    {
        get => (DMPlayerViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    object? IViewFor.ViewModel
    {
        get => ViewModel;
        set => ViewModel = (DMPlayerViewModel?)value;
    }

    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register("ViewModel", typeof(DMPlayerViewModel), typeof(DMPlayerView),
            new PropertyMetadata(null, (d, e) =>
            {
                if (d is DMPlayerView view)
                    view.DataContext = e.NewValue;
            }));

    #endregion
}
