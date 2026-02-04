using System;
using Avalonia;
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

        var close = this.FindControl<Button>("CloseBtn");
        if (close != null) close.Click += (_, __) => Close();

        var fullscreen = this.FindControl<Button>("FullscreenBtn");
        if (fullscreen != null)
        {
            fullscreen.Click += (_, __) =>
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
            };
        }

        Opened += CastWindow_Opened;
        Closed += CastWindow_Closed;
    }

    private void CastWindow_Opened(object? sender, EventArgs e)
    {
        Core.Initialize();
        _libVlc = new LibVLC();
        _mediaPlayer = new MediaPlayer(_libVlc);
        
        if (_videoView != null)
        {
            _videoView.MediaPlayer = _mediaPlayer;
        }

        _mediaPlayer.EndReached += MediaPlayer_EndReached;

        if (DataContext is MainWindowViewModel vm)
        {
            vm.PlayRequested += Vm_PlayRequested;
        }
    }

    private void Vm_PlayRequested(object? sender, Models.MediaItem item)
    {
        if (_libVlc == null || _mediaPlayer == null) return;

        Dispatcher.UIThread.Post(() =>
        {
            var media = new Media(_libVlc, new Uri(item.Source));
            _mediaPlayer.Play(media);
        });
    }

    private void MediaPlayer_EndReached(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.CurrentItem = null;
            }
            _mediaPlayer?.Stop();
        });
    }

    private void CastWindow_Closed(object? sender, EventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.PlayRequested -= Vm_PlayRequested;
        }

        _mediaPlayer?.Stop();
        _mediaPlayer?.Dispose();
        _libVlc?.Dispose();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}