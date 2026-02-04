using System;

namespace CleanCast.Models;

public enum MediaType
{
    YouTube,
    File
}

public class MediaItem
{
    public string Title { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public MediaType Type { get; set; }
    public string? DisplaySource => Type == MediaType.YouTube ? "YouTube" : "Local File";

    public override string ToString() => $"[{DisplaySource}] {Title}";
}
