using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.Platform;
using CleanCast.ViewModels;
using CleanCast.Services;
using LibVLCSharp.Shared;
using LibVLCSharp.Avalonia;

namespace CleanCast.Views;

public partial class CastWindow : Window
{
    private LibVLC? _libVlc;
    private MediaPlayer? _mediaPlayer;
    private Media? _currentMedia;
    private VideoView? _videoView;
    private Panel? _youtubeContainer;
    private string? _currentYoutubeFile;

    private YoutubeDownloadService? _youtubeDownloadService;

    public CastWindow()
    {
        InitializeComponent();
        Opened += CastWindow_Opened;
        Closed += CastWindow_Closed;
    }

    private async void CastWindow_Opened(object? sender, EventArgs e)
    {
        var useHw = true;
        if (DataContext is MainWindowViewModel vm)
        {
            useHw = vm.UseHardwareDecoding;
        }

        // Initialize YouTube downloader service
        _youtubeDownloadService = new YoutubeDownloadService("https://ytdl.delphigamerz.xyz");

        await Task.Run(() =>
        {
            var options = useHw ? Array.Empty<string>() : new[] { "--avcodec-hw=none" };
            _libVlc = new LibVLC(options);
            _mediaPlayer = new MediaPlayer(_libVlc);
        });

        _videoView = this.FindControl<VideoView>("VideoPlayer");
        _youtubeContainer = this.FindControl<Panel>("YoutubeContainer");

        if (_videoView != null && _mediaPlayer != null)
        {
            _videoView.MediaPlayer = _mediaPlayer;
            _mediaPlayer.EndReached += MediaPlayer_EndReached;
        }

        if (DataContext is MainWindowViewModel vmMain)
        {
            vmMain.PlayRequested += Vm_PlayRequested;
            vmMain.ToggleFullscreenRequested += Vm_ToggleFullscreenRequested;
            vmMain.CloseCastWindowRequested += Vm_CloseCastWindowRequested;
        }
    }

    private void Vm_PlayRequested(object? sender, Models.MediaItem item)
    {
        if (item.Type == Models.MediaType.YouTube)
        {
            PlayYoutubeEmbedded(item);
        }
        else
        {
            PlayLocalFile(item);
        }
    }

    private async void PlayYoutubeEmbedded(Models.MediaItem item)
    {
        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                Console.WriteLine($"[youtube] Playing: {item.Title}");

                // Show video player, hide youtube container
                if (_videoView != null) _videoView.IsVisible = true;
                if (_youtubeContainer != null) _youtubeContainer.IsVisible = false;

                if (DataContext is MainWindowViewModel vm)
                {
                    vm.ErrorMessage = "Fetching stream info...";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[youtube] Error: {ex.Message}");
                if (DataContext is MainWindowViewModel vm)
                {
                    vm.ErrorMessage = $"YouTube error: {ex.Message}";
                }
            }
        });

        // Fetch stream info from the API
        if (_youtubeDownloadService == null)
        {
            _youtubeDownloadService = new YoutubeDownloadService("https://ytdl.delphigamerz.xyz");
        }

        var downloadInfo = await _youtubeDownloadService.GetDownloadUrlAsync(item.Source);

        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                if (downloadInfo == null)
                {
                    if (DataContext is MainWindowViewModel vm)
                    {
                        vm.ErrorMessage = "Failed to fetch YouTube video";
                    }
                    return;
                }

                // Try to use the best available stream
                string? streamUrl = null;

                // Priority: video_url > url > first stream
                if (!string.IsNullOrEmpty(downloadInfo.VideoUrl))
                {
                    streamUrl = downloadInfo.VideoUrl;
                    Console.WriteLine("[youtube] Using video_url stream");
                }
                else if (!string.IsNullOrEmpty(downloadInfo.Url))
                {
                    streamUrl = downloadInfo.Url;
                    Console.WriteLine("[youtube] Using url stream");
                }
                else if (downloadInfo.Streams?.Count > 0)
                {
                    streamUrl = downloadInfo.Streams[0].Url;
                    Console.WriteLine($"[youtube] Using stream: {downloadInfo.Streams[0].Format}");
                }

                if (string.IsNullOrEmpty(streamUrl))
                {
                    if (DataContext is MainWindowViewModel vm)
                    {
                        vm.ErrorMessage = "No playable stream found";
                    }
                    return;
                }

                // Play the stream with VLC
                if (_libVlc == null || _mediaPlayer == null)
                {
                    if (DataContext is MainWindowViewModel vm)
                    {
                        vm.ErrorMessage = "VLC not initialized";
                    }
                    return;
                }

                var media = new Media(_libVlc, streamUrl);
                _currentMedia?.Dispose();
                _currentMedia = media;
                _mediaPlayer.Play(media);

                if (DataContext is MainWindowViewModel vmPlay)
                {
                    vmPlay.ErrorMessage = string.Empty;
                }

                Console.WriteLine($"[youtube] Now playing: {downloadInfo.Title}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[youtube] Playback Error: {ex.Message}");
                if (DataContext is MainWindowViewModel vm)
                {
                    vm.ErrorMessage = $"Playback error: {ex.Message}";
                }
            }
        });
    }

    private void PlayLocalFile(Models.MediaItem item)
    {
        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                Console.WriteLine($"[vlc] Playing: {item.Source}");

                // Show video player, hide youtube container
                if (_videoView != null) _videoView.IsVisible = true;
                if (_youtubeContainer != null) _youtubeContainer.IsVisible = false;

                if (_libVlc == null || _mediaPlayer == null) return;

                var media = new Media(_libVlc, item.Source);
                _currentMedia?.Dispose();
                _currentMedia = media;
                _mediaPlayer.Play(media);

                if (DataContext is MainWindowViewModel vm)
                {
                    vm.ErrorMessage = string.Empty;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[vlc] Error: {ex.Message}");
                if (DataContext is MainWindowViewModel vm)
                {
                    vm.ErrorMessage = $"Playback error: {ex.Message}";
                }
            }
        });
    }

    private void MediaPlayer_EndReached(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            Console.WriteLine("[vlc] Playback ended");
            if (DataContext is MainWindowViewModel vm)
            {
                vm.CurrentItem = null;
            }
            try { _currentMedia?.Dispose(); } catch { }
            _currentMedia = null;
        });
        Task.Run(() => _mediaPlayer?.Stop());
    }

    private void Vm_CloseCastWindowRequested(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() => Close());
    }

    private void Vm_ToggleFullscreenRequested(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            WindowState = (WindowState == WindowState.FullScreen) ? WindowState.Normal : WindowState.FullScreen;
        });
    }

    private void CastWindow_Closed(object? sender, EventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.PlayRequested -= Vm_PlayRequested;
            vm.ToggleFullscreenRequested -= Vm_ToggleFullscreenRequested;
            vm.CloseCastWindowRequested -= Vm_CloseCastWindowRequested;
        }

        if (_mediaPlayer != null)
        {
            _mediaPlayer.EndReached -= MediaPlayer_EndReached;
        }

        if (_videoView != null)
        {
            _videoView.MediaPlayer = null;
        }

        var mp = _mediaPlayer;
        _mediaPlayer = null;
        var vlc = _libVlc;
        _libVlc = null;

        Task.Run(() =>
        {
            try
            {
                mp?.Stop();
                mp?.Dispose();
                vlc?.Dispose();
            }
            catch { }

            // Clean up temp YouTube HTML file
            try
            {
                if (!string.IsNullOrEmpty(_currentYoutubeFile) && File.Exists(_currentYoutubeFile))
                    File.Delete(_currentYoutubeFile);
            }
            catch { }
        });
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}


