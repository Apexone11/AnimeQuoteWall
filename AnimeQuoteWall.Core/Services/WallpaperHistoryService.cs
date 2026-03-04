using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AnimeQuoteWall.Core.Configuration;
using AnimeQuoteWall.Core.Models;

namespace AnimeQuoteWall.Core.Services;

/// <summary>
/// Service for managing wallpaper history.
/// 
/// This service handles saving generated wallpapers to a history folder along with
/// metadata (quote, background path, settings) so users can view and restore
/// previously generated wallpapers.
/// 
/// History Structure:
/// - HistoryDirectory/wallpaper_YYYYMMDD_HHMMSS.png (image files)
/// - HistoryDirectory/metadata.json (metadata for all wallpapers)
/// </summary>
public class WallpaperHistoryService
{
    /// <summary>
    /// Directory where wallpaper history is stored.
    /// Default: %LOCALAPPDATA%\AnimeQuotes\history\
    /// </summary>
    private static readonly string HistoryDirectory = AppConfiguration.HistoryDirectory;
    
    /// <summary>
    /// Path to the metadata JSON file containing wallpaper information.
    /// </summary>
    private static readonly string MetadataFile = Path.Combine(HistoryDirectory, "metadata.json");

    /// <summary>
    /// Ensures the history directory exists.
    /// Creates the directory if it doesn't exist.
    /// </summary>
    public void EnsureHistoryDirectory()
    {
        Directory.CreateDirectory(HistoryDirectory);
    }

    /// <summary>
    /// Saves a wallpaper to history with metadata.
    /// 
    /// Process:
    /// 1. Copy wallpaper image to history folder with timestamp filename
    /// 2. Create history entry with metadata
    /// 3. Add entry to metadata file
    /// 
    /// </summary>
    /// <param name="wallpaperPath">Path to the generated wallpaper image</param>
    /// <param name="quote">The quote used in the wallpaper</param>
    /// <param name="backgroundPath">Path to the background image used (if any)</param>
    /// <param name="settings">Wallpaper settings used for generation</param>
    public async Task SaveToHistoryAsync(string wallpaperPath, Quote quote, string? backgroundPath, WallpaperSettings settings)
    {
        // Ensure history directory exists
        EnsureHistoryDirectory();

        // Generate unique filename with timestamp to prevent conflicts
        var timestamp = DateTime.Now;
        var fileName = $"wallpaper_{timestamp:yyyyMMdd_HHmmss}.png";
        var historyPath = Path.Combine(HistoryDirectory, fileName);

        // Copy wallpaper image to history folder
        if (File.Exists(wallpaperPath))
        {
            File.Copy(wallpaperPath, historyPath, overwrite: true);
        }

        // Create history entry with all metadata
        var entry = new WallpaperHistoryEntry
        {
            ImagePath = historyPath,
            Timestamp = timestamp,
            Quote = quote,
            BackgroundPath = backgroundPath,
            Settings = settings
        };

        // Load existing entries and add new one
        var entries = await LoadHistoryEntriesAsync().ConfigureAwait(false);
        entries.Add(entry);

        // Save updated metadata to JSON file
        await SaveHistoryEntriesAsync(entries).ConfigureAwait(false);
    }

    /// <summary>
    /// Loads all history entries.
    /// </summary>
    public async Task<List<WallpaperHistoryEntry>> LoadHistoryEntriesAsync()
    {
        if (!File.Exists(MetadataFile))
        {
            return new List<WallpaperHistoryEntry>();
        }

        try
        {
            var json = await File.ReadAllTextAsync(MetadataFile).ConfigureAwait(false);
            var entries = JsonSerializer.Deserialize<List<WallpaperHistoryEntry>>(json) ?? new List<WallpaperHistoryEntry>();
            
            // Filter out entries where image file no longer exists
            return entries.Where(e => File.Exists(e.ImagePath)).ToList();
        }
        catch
        {
            return new List<WallpaperHistoryEntry>();
        }
    }

    /// <summary>
    /// Saves history entries to metadata file.
    /// </summary>
    private async Task SaveHistoryEntriesAsync(List<WallpaperHistoryEntry> entries)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(entries, options);
        await File.WriteAllTextAsync(MetadataFile, json).ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes a wallpaper from history.
    /// </summary>
    public async Task DeleteFromHistoryAsync(WallpaperHistoryEntry entry)
    {
        // Delete image file
        if (File.Exists(entry.ImagePath))
        {
            try
            {
                File.Delete(entry.ImagePath);
            }
            catch { /* ignore */ }
        }

        // Remove from metadata
        var entries = await LoadHistoryEntriesAsync().ConfigureAwait(false);
        entries.RemoveAll(e => e.ImagePath == entry.ImagePath);
        await SaveHistoryEntriesAsync(entries).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the history directory path.
    /// </summary>
    public static string GetHistoryDirectory() => HistoryDirectory;
}

