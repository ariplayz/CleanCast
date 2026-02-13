# CleanCast YouTube Downloader API Integration

## Overview

CleanCast now integrates with your self-hosted YouTube downloader API at `https://ytdl.delphigamerz.xyz`. This integration allows the application to fetch direct streaming URLs from YouTube videos, which are then played directly through VLC, providing:

- **Direct streaming** from YouTube without using the embedded YouTube player
- **Ad-free playback** through direct stream URL
- **Better control** over playback quality and format
- **Offline compatibility** with local media files alongside YouTube content

## Architecture

### Service Layer: YoutubeDownloadService

The `YoutubeDownloadService` class (located in `CleanCast/Services/YoutubeDownloadService.cs`) handles all communication with the self-hosted API.

#### Key Methods

**GetDownloadUrlAsync(youtubeUrl: string)**
- Fetches download streams for a YouTube video
- Returns a `YoutubeDownloadResponse` containing:
  - Title of the video
  - Multiple stream URLs (video_url, url, or list of streams)
  - Thumbnail information
  - Duration
  - Available formats/quality options
- Used for direct playback through VLC

**GetVideoInfoAsync(youtubeUrl: string)**
- Fetches metadata about a video without downloading
- Returns a `YoutubeVideoInfo` containing:
  - Title, URL, duration, thumbnail
  - Channel information
  - Upload date and description
  - Available format options
- Useful for displaying video information before playback

### Data Models

#### YoutubeDownloadResponse
```csharp
public class YoutubeDownloadResponse
{
    public string Title { get; set; }
    public string Url { get; set; }
    public string VideoUrl { get; set; }  // Preferred for direct playback
    public string AudioUrl { get; set; }
    public List<StreamInfo> Streams { get; set; }
    public string Thumbnail { get; set; }
    public int Duration { get; set; }
    public List<FormatInfo> Formats { get; set; }
}
```

#### StreamInfo
```csharp
public class StreamInfo
{
    public string FormatId { get; set; }
    public string Format { get; set; }
    public string Url { get; set; }
    public string Quality { get; set; }
    public string Extension { get; set; }
}
```

#### FormatInfo
```csharp
public class FormatInfo
{
    public string FormatId { get; set; }
    public string Format { get; set; }
    public string Extension { get; set; }
    public long? FileSize { get; set; }
    public double? BitRate { get; set; }
    public int? Height { get; set; }
    public int? Width { get; set; }
    public string VideoCodec { get; set; }
    public string AudioCodec { get; set; }
}
```

## Playback Flow

### YouTube Video Playback

1. **User adds YouTube URL**: User enters a YouTube URL in the UI
2. **API Fetch**: `GetDownloadUrlAsync()` is called with the URL
3. **Stream Selection**: The service selects the best available stream:
   - Priority: `video_url` > `url` > first stream in `Streams` list
4. **VLC Playback**: Selected stream URL is passed to LibVLC for direct playback
5. **Error Handling**: If API fails or no streams available, user is notified

### Local File Playback

- Local files are played directly through VLC without API involvement
- Existing functionality unchanged

## API Configuration

The API URL is currently hardcoded as `https://ytdl.delphigamerz.xyz` in `CastWindow.axaml.cs`:

```csharp
_youtubeDownloadService = new YoutubeDownloadService("https://ytdl.delphigamerz.xyz");
```

### To Change the API URL

Edit `CastWindow.axaml.cs` (line ~42) and modify the URL:

```csharp
_youtubeDownloadService = new YoutubeDownloadService("YOUR_NEW_API_URL");
```

Alternatively, make it configurable through AppSettings:

```csharp
_youtubeDownloadService = new YoutubeDownloadService(_settings.YoutubeApiUrl);
```

## Error Handling

The service includes comprehensive error handling:

- **HTTP Errors**: If the API returns a non-success status code, the error is logged and null is returned
- **JSON Parse Errors**: If the response cannot be parsed, an error is logged
- **Network Timeouts**: Default 30-second timeout prevents hanging requests
- **User Feedback**: All errors are displayed to the user through `ErrorMessage` property

## Logging

All operations are logged to the console with the prefix `[YoutubeDownloadService]`:

```
[YoutubeDownloadService] Fetching download info for: https://www.youtube.com/watch?v=...
[YoutubeDownloadService] Successfully fetched: Video Title
[YoutubeDownloadService] HTTP Request Error: Connection timeout
```

## Expected API Response Format

Based on the Simatwa/youtube-downloader-api, the expected response format is:

### Download Endpoint (`/api/download?url=...`)

```json
{
  "title": "Video Title",
  "url": "https://...",
  "video_url": "https://...",  // Preferred stream URL
  "audio_url": "https://...",
  "thumbnail": "https://...",
  "duration": 300,
  "streams": [
    {
      "format_id": "18",
      "format": "18 - 360p",
      "url": "https://...",
      "quality": "360p",
      "ext": "mp4"
    }
  ],
  "formats": [...]
}
```

## Future Improvements

1. **Quality Selection UI**: Add UI to select video quality before playback
2. **Download Support**: Implement video downloading instead of just streaming
3. **Subtitle Support**: Handle subtitle tracks from the API response
4. **Caching**: Cache metadata to reduce API calls
5. **Format Selection**: Allow users to choose preferred format
6. **Resumable Streaming**: Handle interrupted streams gracefully
7. **API Configuration UI**: Make API URL configurable in settings

## Troubleshooting

### Videos Not Playing
1. Verify the API URL is correct and accessible
2. Check network connectivity to `ytdl.delphigamerz.xyz`
3. Verify the YouTube URL format is correct
4. Check console logs for specific error messages

### API Timeout
- Increase the timeout in `YoutubeDownloadService` constructor (currently 30 seconds)
- Check if the API server is responding

### Stream URL Unavailable
- The API may not support the video (geo-restricted, private, deleted, etc.)
- Try with a different video
- Check API documentation for limitations

## References

- **Self-Hosted API**: https://github.com/Simatwa/youtube-downloader-api
- **API URL**: https://ytdl.delphigamerz.xyz
- **LibVLCSharp**: https://github.com/videolan/LibVLCSharp
- **CleanCast Repository**: (Your repo here)

