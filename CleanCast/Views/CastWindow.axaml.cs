using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using CleanCast.ViewModels;
using LibVLCSharp.Shared;
using LibVLCSharp.Avalonia;

namespace CleanCast.Views;

public partial class CastWindow : Window
{
    private LibVLC? _libVlc;
    private MediaPlayer? _mediaPlayer;
    private VideoView? _videoView;

    public CastWindow()
    {
        InitializeComponent();
        
        _videoView = this.FindControl<VideoView>("VideoPlayer");

        Opened += CastWindow_Opened;
        Closed += CastWindow_Closed;
    }

    private async void CastWindow_Opened(object? sender, EventArgs e)
    {
        // Determine hardware-decoding option from VM settings (if available)
        var useHw = true; // default
        if (DataContext is MainWindowViewModel vm)
        {
            useHw = vm.UseHardwareDecoding;
        }

        // Initialize LibVLC and MediaPlayer on a background thread to prevent UI hang
        await System.Threading.Tasks.Task.Run(() =>
        {
            var options = useHw ? Array.Empty<string>() : new[] { "--avcodec-hw=none" };
            _libVlc = new LibVLC(options);
            _mediaPlayer = new MediaPlayer(_libVlc);
        });
        
        if (_videoView != null && _mediaPlayer != null)
        {
            _videoView.MediaPlayer = _mediaPlayer;
        }

        if (_mediaPlayer != null)
        {
            _mediaPlayer.EndReached += MediaPlayer_EndReached;
            _mediaPlayer.EncounteredError += MediaPlayer_EncounteredError;
            _mediaPlayer.Playing += MediaPlayer_Playing;
            _mediaPlayer.Stopped += MediaPlayer_Stopped;
        }

        if (DataContext is MainWindowViewModel vm2)
        {
            vm2.PlayRequested += Vm_PlayRequested;
            vm2.ToggleFullscreenRequested += Vm_ToggleFullscreenRequested;
            vm2.CloseCastWindowRequested += Vm_CloseCastWindowRequested;
        }
    }

    private void MediaPlayer_Stopped(object? sender, EventArgs e)
    {
        // MediaPlayer events can be raised from libVLC threads. Marshal to the UI thread
        Dispatcher.UIThread.Post(() =>
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.ErrorMessage = string.Empty;
            }
        });
    }

    private void MediaPlayer_Playing(object? sender, EventArgs e)
    {
        // MediaPlayer events can be raised from libVLC threads. Marshal to the UI thread
        Dispatcher.UIThread.Post(() =>
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.ErrorMessage = string.Empty;
            }
        });
    }

    private void MediaPlayer_EncounteredError(object? sender, EventArgs e)
    {
        // Called from libVLC thread; marshal to UI thread and surface to VM
        Dispatcher.UIThread.Post(async () =>
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.ErrorMessage = "Playback error: An error occurred during playback. Trying fallback resolver...";

                // try yt-dlp fallback if the CurrentItem is a YouTube URL
                var url = vm.CurrentItem?.Source;
                if (!string.IsNullOrEmpty(url) && url.Contains("youtube.com", StringComparison.OrdinalIgnoreCase))
                {
                    var resolved = await TryResolveWithYtDlpAsync(url);
                    if (!string.IsNullOrEmpty(resolved))
                    {
                        // play the resolved URL
                        try
                        {
                            var media = new Media(_libVlc, new Uri(resolved));
                            _mediaPlayer?.Play(media);
                            vm.ErrorMessage = string.Empty;
                            return;
                        }
                        catch (Exception ex)
                        {
                            vm.ErrorMessage = $"Fallback playback error: {ex.Message}";
                        }
                    }
                    else
                    {
                        vm.ErrorMessage = "Fallback resolver failed (yt-dlp not available or no URL).";
                    }
                }
                else
                {
                    vm.ErrorMessage = "Playback error: An error occurred during playback.";
                }
            }
        });
    }

    private async Task<string?> TryResolveWithYtDlpAsync(string pageUrl)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "yt-dlp",
                Arguments = $"-f best -g \"{pageUrl}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var proc = Process.Start(psi);
            if (proc == null) return null;

            // Wait up to 8 seconds
            var completed = await Task.Run(() => proc.WaitForExit(8000));
            if (!completed)
            {
                try { proc.Kill(true); } catch { }
                return null;
            }

            var output = await proc.StandardOutput.ReadToEndAsync();
            var err = await proc.StandardError.ReadToEndAsync();
            if (!string.IsNullOrWhiteSpace(output))
            {
                var first = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)[0];
                return first.Trim();
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    private void Vm_CloseCastWindowRequested(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() => Close());
    }

    private void Vm_ToggleFullscreenRequested(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (WindowState == WindowState.FullScreen)
            {
                WindowState = WindowState.Normal;
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
            else
            {
                WindowState = WindowState.FullScreen;
            }
        });
    }

    private void Vm_PlayRequested(object? sender, Models.MediaItem item)
    {
        if (_libVlc == null || _mediaPlayer == null) return;

        Dispatcher.UIThread.Post(async () =>
        {
            try
            {
                Media media;
                // Use different Media constructors for URLs vs local paths
                if (item.Type == Models.MediaType.YouTube || item.Source.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    media = new Media(_libVlc, new Uri(item.Source));
                }
                else
                {
                    // Use FromPath by constructor overload (omit redundant enum parameter)
                    media = new Media(_libVlc, item.Source);
                }

                // Clear previous error before attempting play
                if (DataContext is MainWindowViewModel vm)
                {
                    vm.ErrorMessage = string.Empty;
                }

                _mediaPlayer.Play(media);
            }
            catch (Exception ex)
            {
                if (DataContext is MainWindowViewModel vm)
                {
                    vm.ErrorMessage = $"Playback error: {ex.Message}";
                    // Attempt fallback for YouTube URLs
                    if (!string.IsNullOrEmpty(item.Source) && item.Source.Contains("youtube.com", StringComparison.OrdinalIgnoreCase))
                    {
                        var resolved = await TryResolveWithYtDlpAsync(item.Source);
                        if (!string.IsNullOrEmpty(resolved))
                        {
                            try
                            {
                                var media = new Media(_libVlc, new Uri(resolved));
                                _mediaPlayer.Play(media);
                                vm.ErrorMessage = string.Empty;
                            }
                            catch (Exception ex2)
                            {
                                vm.ErrorMessage = $"Fallback playback error: {ex2.Message}";
                            }
                        }
                        else
                        {
                            vm.ErrorMessage = "Fallback resolver failed (yt-dlp not available or no URL).";
                        }
                    }
                }
            }
        });
    }

    private void MediaPlayer_EndReached(object? sender, EventArgs e)
    {
        // EndReached is called from a LibVLC thread. 
        // We update the UI and stop the player safely.
        
        Dispatcher.UIThread.Post(() =>
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.CurrentItem = null;
            }
        });

        // Calling Stop() directly in EndReached or synchronously on UI thread can deadlock.
        System.Threading.Tasks.Task.Run(() => _mediaPlayer?.Stop());
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
            _mediaPlayer.EncounteredError -= MediaPlayer_EncounteredError;
            _mediaPlayer.Playing -= MediaPlayer_Playing;
            _mediaPlayer.Stopped -= MediaPlayer_Stopped;
        }

        // 1. Detach the VideoView from the MediaPlayer first
        if (_videoView != null)
        {
            _videoView.MediaPlayer = null;
        }

        // 2. Dispose of the MediaPlayer and LibVLC
        // We do this carefully to avoid race conditions with the native side
        var mp = _mediaPlayer;
        _mediaPlayer = null;
        
        var vlc = _libVlc;
        _libVlc = null;

        System.Threading.Tasks.Task.Run(() =>
        {
            try
            {
                mp?.Stop();
                mp?.Dispose();
                vlc?.Dispose();
            }
            catch
            {
                // Ignore disposal errors during shutdown
            }
        });
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

