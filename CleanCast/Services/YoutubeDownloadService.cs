using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CleanCast.Services;

/// <summary>
/// Service for interacting with the self-hosted YouTube downloader API
/// </summary>
public class YoutubeDownloadService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl;
    private const int TimeoutSeconds = 30;

    public YoutubeDownloadService(string apiBaseUrl = "https://ytdl.delphigamerz.xyz")
    {
        _apiBaseUrl = apiBaseUrl.TrimEnd('/');
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(TimeoutSeconds)
        };
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "CleanCast/1.0");
    }

    /// <summary>
    /// Get download streams for a YouTube video
    /// </summary>
    public async Task<YoutubeDownloadResponse?> GetDownloadUrlAsync(string youtubeUrl)
    {
        try
        {
            Console.WriteLine($"[YoutubeDownloadService] Fetching download info for: {youtubeUrl}");

            // The API typically expects a query parameter with the URL
            var encodedUrl = Uri.EscapeDataString(youtubeUrl);
            var requestUrl = $"{_apiBaseUrl}/api/download?url={encodedUrl}";

            var response = await _httpClient.GetAsync(requestUrl);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[YoutubeDownloadService] Error: {response.StatusCode} - {errorContent}");
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            var result = JsonSerializer.Deserialize<YoutubeDownloadResponse>(content, options);
            
            if (result != null)
            {
                Console.WriteLine($"[YoutubeDownloadService] Successfully fetched: {result.Title}");
            }

            return result;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"[YoutubeDownloadService] HTTP Request Error: {ex.Message}");
            return null;
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"[YoutubeDownloadService] JSON Parse Error: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[YoutubeDownloadService] Unexpected Error: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Get information about a YouTube video without downloading
    /// </summary>
    public async Task<YoutubeVideoInfo?> GetVideoInfoAsync(string youtubeUrl)
    {
        try
        {
            Console.WriteLine($"[YoutubeDownloadService] Fetching video info for: {youtubeUrl}");

            var encodedUrl = Uri.EscapeDataString(youtubeUrl);
            var requestUrl = $"{_apiBaseUrl}/api/info?url={encodedUrl}";

            var response = await _httpClient.GetAsync(requestUrl);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[YoutubeDownloadService] Info Error: {response.StatusCode} - {errorContent}");
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            var result = JsonSerializer.Deserialize<YoutubeVideoInfo>(content, options);
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[YoutubeDownloadService] Info Fetch Error: {ex.Message}");
            return null;
        }
    }
}

/// <summary>
/// Response model for download request
/// </summary>
public class YoutubeDownloadResponse
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("video_url")]
    public string VideoUrl { get; set; } = string.Empty;

    [JsonPropertyName("audio_url")]
    public string AudioUrl { get; set; } = string.Empty;

    [JsonPropertyName("streams")]
    public List<StreamInfo>? Streams { get; set; }

    [JsonPropertyName("thumbnail")]
    public string? Thumbnail { get; set; }

    [JsonPropertyName("duration")]
    public int Duration { get; set; }

    [JsonPropertyName("formats")]
    public List<FormatInfo>? Formats { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }
}

/// <summary>
/// Information about available streams/formats
/// </summary>
public class StreamInfo
{
    [JsonPropertyName("format_id")]
    public string FormatId { get; set; } = string.Empty;

    [JsonPropertyName("format")]
    public string Format { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("quality")]
    public string? Quality { get; set; }

    [JsonPropertyName("ext")]
    public string Extension { get; set; } = string.Empty;
}

/// <summary>
/// Format information for video quality options
/// </summary>
public class FormatInfo
{
    [JsonPropertyName("format_id")]
    public string FormatId { get; set; } = string.Empty;

    [JsonPropertyName("format")]
    public string Format { get; set; } = string.Empty;

    [JsonPropertyName("ext")]
    public string Extension { get; set; } = string.Empty;

    [JsonPropertyName("filesize")]
    public long? FileSize { get; set; }

    [JsonPropertyName("tbr")]
    public double? BitRate { get; set; }

    [JsonPropertyName("height")]
    public int? Height { get; set; }

    [JsonPropertyName("width")]
    public int? Width { get; set; }

    [JsonPropertyName("vcodec")]
    public string? VideoCodec { get; set; }

    [JsonPropertyName("acodec")]
    public string? AudioCodec { get; set; }
}

/// <summary>
/// Video information response
/// </summary>
public class YoutubeVideoInfo
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("duration")]
    public int Duration { get; set; }

    [JsonPropertyName("thumbnail")]
    public string? Thumbnail { get; set; }

    [JsonPropertyName("channel")]
    public string? Channel { get; set; }

    [JsonPropertyName("upload_date")]
    public string? UploadDate { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("formats")]
    public List<FormatInfo>? Formats { get; set; }
}

