using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CleanCast.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace CleanCast.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly YoutubeClient _youtube = new();
    private readonly AppSettings _settings;
    private Timer? _errorClearTimer;
    
    [ObservableProperty]
    private ObservableCollection<MediaItem> _queue = new();

    [ObservableProperty]
    private MediaItem? _currentItem;

    [ObservableProperty]
    private string _youtubeUrl = string.Empty;

    [ObservableProperty]
    private bool _isCastWindowOpen;

    [ObservableProperty]
    private bool _isFullscreen;

    // Replaced generated ObservableProperty with explicit implementation for diagnostics
    private string _errorMessage = string.Empty;
    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            if (SetProperty(ref _errorMessage, value))
            {
                // If set, schedule auto-clear after 8 seconds.
                _errorClearTimer?.Dispose();
                _errorClearTimer = null;
                if (!string.IsNullOrEmpty(_errorMessage))
                {
                    _errorClearTimer = new Timer(_ =>
                    {
                        ErrorMessage = string.Empty;
                    }, null, TimeSpan.FromSeconds(8), Timeout.InfiniteTimeSpan);
                }
            }
        }
    }

    public bool UseHardwareDecoding
    {
        get => _settings.UseHardwareDecoding;
        set
        {
            if (_settings.UseHardwareDecoding != value)
            {
                _settings.UseHardwareDecoding = value;
                _settings.Save();
                OnPropertyChanged(nameof(UseHardwareDecoding));
            }
        }
    }

    public event EventHandler? ToggleFullscreenRequested;
    public event EventHandler? CloseCastWindowRequested;

    public event EventHandler<MediaItem>? PlayRequested;

    public MainWindowViewModel()
    {
        _settings = AppSettings.Load();
    }

    [RelayCommand]
    private void ToggleFullscreen()
    {
        IsFullscreen = !IsFullscreen;
        ToggleFullscreenRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void CloseCastWindow()
    {
        CloseCastWindowRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private async Task AddYoutubeAction()
    {
        var url = YoutubeUrl?.Trim();
        if (string.IsNullOrWhiteSpace(url)) return;

        try
        {
            // Resolve video metadata. Instead of trying to get a direct stream URL (which
            // can sometimes return 403 from YouTube), store the video page URL and let
            // LibVLC resolve playback (it has youtube scripts bundled with libvlc).
            var video = await _youtube.Videos.GetAsync(url);

            var item = new MediaItem
            {
                Title = video.Title,
                // Use canonical YouTube watch URL so libVLC's youtube.lua can handle it.
                Source = $"https://www.youtube.com/watch?v={video.Id}",
                Type = MediaType.YouTube
            };

            Queue.Add(item);
            YoutubeUrl = string.Empty;

            // Clear previous errors on successful add
            ErrorMessage = string.Empty;
        }
        catch (Exception ex)
        {
            var message = ex.Message ?? "Unknown YouTube error";
            Console.WriteLine($"[DEBUG_LOG] YouTube error: {message}");
            // Surface the error to the UI
            ErrorMessage = $"YouTube error: {message}";
        }
    }

    [RelayCommand]
    private void AddFileAction(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;

        var item = new MediaItem
        {
            Title = Path.GetFileName(path),
            Source = path,
            Type = MediaType.File
        };
        Queue.Add(item);
    }

    [RelayCommand]
    private void PlayNext()
    {
        if (Queue.Count > 0)
        {
            CurrentItem = Queue[0];
            Queue.RemoveAt(0);
            PlayRequested?.Invoke(this, CurrentItem);
        }
        else
        {
            CurrentItem = null;
        }
    }

    [RelayCommand]
    private void ClearQueue()
    {
        Queue.Clear();
    }
}