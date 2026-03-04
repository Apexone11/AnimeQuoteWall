using System;
using System.Text.Json.Serialization;

namespace AnimeQuoteWall.Core.Models;

/// <summary>
/// Represents a wallpaper history entry with metadata.
/// </summary>
public class WallpaperHistoryEntry
{
    /// <summary>
    /// Gets or sets the path to the wallpaper image file.
    /// </summary>
    [JsonPropertyName("imagePath")]
    public string ImagePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the wallpaper was generated.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the quote used in the wallpaper.
    /// </summary>
    [JsonPropertyName("quote")]
    public Quote? Quote { get; set; }

    /// <summary>
    /// Gets or sets the background image path used.
    /// </summary>
    [JsonPropertyName("backgroundPath")]
    public string? BackgroundPath { get; set; }

    /// <summary>
    /// Gets or sets the wallpaper settings used.
    /// </summary>
    [JsonPropertyName("settings")]
    public WallpaperSettings? Settings { get; set; }
}

