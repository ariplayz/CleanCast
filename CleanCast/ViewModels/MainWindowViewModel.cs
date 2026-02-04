using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
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
    
    [ObservableProperty]
    private ObservableCollection<MediaItem> _queue = new();

    [ObservableProperty]
    private MediaItem? _currentItem;

    [ObservableProperty]
    private string _youtubeUrl = string.Empty;

    [ObservableProperty]
    private bool _isCastWindowOpen;

    public event EventHandler<MediaItem>? PlayRequested;

    [RelayCommand]
    private async Task AddYoutubeAction()
    {
        if (string.IsNullOrWhiteSpace(YoutubeUrl)) return;

        try
        {
            var video = await _youtube.Videos.GetAsync(YoutubeUrl);
            var streamManifest = await _youtube.Videos.Streams.GetManifestAsync(video.Id);
            var streamInfo = streamManifest.GetMuxedStreams().GetWithHighestVideoQuality();

            if (streamInfo != null)
            {
                var item = new MediaItem
                {
                    Title = video.Title,
                    Source = streamInfo.Url,
                    Type = MediaType.YouTube
                };
                Queue.Add(item);
                YoutubeUrl = string.Empty;
            }
        }
        catch (Exception)
        {
            // Handle error (e.g., invalid URL)
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