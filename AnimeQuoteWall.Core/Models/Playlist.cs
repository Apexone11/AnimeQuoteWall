using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AnimeQuoteWall.Core.Models;

/// <summary>
/// Represents a playlist that can automatically rotate wallpapers.
/// A playlist contains multiple wallpaper entries and can be scheduled to change at intervals or specific times.
/// </summary>
public class Playlist
{
    /// <summary>
    /// Gets or sets the unique identifier for this playlist.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the name of the playlist.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of wallpaper entries in this playlist.
    /// Each entry represents a quote+background combination.
    /// </summary>
    [JsonPropertyName("wallpaperEntries")]
    public List<PlaylistWallpaperEntry> WallpaperEntries { get; set; } = new();

    /// <summary>
    /// Gets or sets the interval in seconds between wallpaper changes when using interval-based scheduling.
    /// Default: 300 seconds (5 minutes).
    /// </summary>
    [JsonPropertyName("intervalSeconds")]
    public int IntervalSeconds { get; set; } = 300;

    /// <summary>
    /// Gets or sets whether to shuffle the playlist order.
    /// When true, wallpapers are played in random order.
    /// </summary>
    [JsonPropertyName("shuffleMode")]
    public bool ShuffleMode { get; set; } = false;

    /// <summary>
    /// Gets or sets whether this playlist is enabled and should be executed.
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Gets or sets the schedule type for this playlist.
    /// Valid values: "Interval", "Hourly", "Daily", "OnLaunch", "Custom"
    /// </summary>
    [JsonPropertyName("scheduleType")]
    public string ScheduleType { get; set; } = "Interval";

    /// <summary>
    /// Gets or sets the schedule time for daily/custom schedules.
    /// Format: "HH:mm" (e.g., "14:30" for 2:30 PM).
    /// </summary>
    [JsonPropertyName("scheduleTime")]
    public string? ScheduleTime { get; set; }

    /// <summary>
    /// Gets or sets the days of the week when the playlist should run (for weekly schedules).
    /// Values: 0=Sunday, 1=Monday, ..., 6=Saturday
    /// </summary>
    [JsonPropertyName("daysOfWeek")]
    public List<int> DaysOfWeek { get; set; } = new();

    /// <summary>
    /// Gets or sets the timestamp when this playlist was created.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Gets or sets the timestamp when this playlist was last modified.
    /// </summary>
    [JsonPropertyName("modifiedAt")]
    public DateTime ModifiedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Gets or sets the index of the current wallpaper being played.
    /// Used to track position in the playlist.
    /// </summary>
    [JsonPropertyName("currentIndex")]
    public int CurrentIndex { get; set; } = 0;

    /// <summary>
    /// Gets the next wallpaper entry in the playlist, advancing the current index.
    /// Respects shuffle mode if enabled.
    /// </summary>
    /// <returns>The next wallpaper entry, or null if playlist is empty.</returns>
    public PlaylistWallpaperEntry? GetNextEntry()
    {
        if (WallpaperEntries == null || WallpaperEntries.Count == 0)
            return null;

        if (ShuffleMode)
        {
            // Random selection for shuffle mode
            var random = new Random();
            return WallpaperEntries[random.Next(WallpaperEntries.Count)];
        }
        else
        {
            // Sequential playback
            var entry = WallpaperEntries[CurrentIndex];
            CurrentIndex = (CurrentIndex + 1) % WallpaperEntries.Count;
            return entry;
        }
    }

    /// <summary>
    /// Validates that the playlist has valid configuration.
    /// </summary>
    /// <returns>True if the playlist is valid and can be executed.</returns>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Name) &&
               WallpaperEntries != null &&
               WallpaperEntries.Count > 0 &&
               IntervalSeconds > 0 &&
               IsScheduleTypeValid();
    }

    /// <summary>
    /// Validates that the schedule type is valid.
    /// </summary>
    private bool IsScheduleTypeValid()
    {
        return ScheduleType switch
        {
            "Interval" => true,
            "Hourly" => true,
            "Daily" => ScheduleTime != null && IsValidTimeFormat(ScheduleTime),
            "OnLaunch" => true,
            "Custom" => ScheduleTime != null && IsValidTimeFormat(ScheduleTime),
            _ => false
        };
    }

    /// <summary>
    /// Validates that the time string is in HH:mm format.
    /// </summary>
    private static bool IsValidTimeFormat(string time)
    {
        if (string.IsNullOrWhiteSpace(time))
            return false;

        var parts = time.Split(':');
        if (parts.Length != 2)
            return false;

        if (!int.TryParse(parts[0], out var hour) || hour < 0 || hour > 23)
            return false;

        if (!int.TryParse(parts[1], out var minute) || minute < 0 || minute > 59)
            return false;

        return true;
    }
}

/// <summary>
/// Represents a single wallpaper entry in a playlist.
/// Contains the quote, background, and settings needed to generate the wallpaper.
/// </summary>
public class PlaylistWallpaperEntry
{
    /// <summary>
    /// Gets or sets the quote to use for this wallpaper.
    /// </summary>
    [JsonPropertyName("quote")]
    public Quote? Quote { get; set; }

    /// <summary>
    /// Gets or sets the path to the background image (optional).
    /// </summary>
    [JsonPropertyName("backgroundPath")]
    public string? BackgroundPath { get; set; }

    /// <summary>
    /// Gets or sets the wallpaper settings to use for generation.
    /// If null, default settings will be used.
    /// </summary>
    [JsonPropertyName("settings")]
    public WallpaperSettings? Settings { get; set; }

    /// <summary>
    /// Gets or sets an optional name/description for this entry.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when this entry was added to the playlist.
    /// </summary>
    [JsonPropertyName("addedAt")]
    public DateTime AddedAt { get; set; } = DateTime.Now;
}

