using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AnimeQuoteWall.Core.Configuration;
using AnimeQuoteWall.Core.Models;
using AnimeQuoteWall.Core.Protection;

namespace AnimeQuoteWall.Core.Services;

/// <summary>
/// Service for managing playlists.
/// Handles CRUD operations, JSON persistence, and playlist execution logic.
/// </summary>
public class PlaylistService
{
    /// <summary>
    /// Gets the directory where playlists are stored.
    /// </summary>
    private static string PlaylistsDirectory => AppConfiguration.PlaylistsDirectory;

    /// <summary>
    /// Ensures the playlists directory exists.
    /// </summary>
    public void EnsurePlaylistsDirectory()
    {
        Directory.CreateDirectory(PlaylistsDirectory);
    }

    /// <summary>
    /// Loads all playlists from the playlists directory.
    /// </summary>
    /// <returns>List of all playlists.</returns>
    public async Task<List<Playlist>> LoadAllPlaylistsAsync()
    {
        EnsurePlaylistsDirectory();
        var playlists = new List<Playlist>();

        if (!Directory.Exists(PlaylistsDirectory))
            return playlists;

        var jsonFiles = Directory.GetFiles(PlaylistsDirectory, "*.json");

        foreach (var filePath in jsonFiles)
        {
            try
            {
                var playlist = await LoadPlaylistAsync(filePath).ConfigureAwait(false);
                if (playlist != null)
                {
                    playlists.Add(playlist);
                }
            }
            catch
            {
                // Skip invalid playlist files
            }
        }

        return playlists.OrderBy(p => p.Name).ToList();
    }

    /// <summary>
    /// Loads a playlist from a JSON file.
    /// </summary>
    /// <param name="filePath">Path to the playlist JSON file.</param>
    /// <returns>The loaded playlist, or null if file doesn't exist or is invalid.</returns>
    public async Task<Playlist?> LoadPlaylistAsync(string filePath)
    {
        if (!File.Exists(filePath))
            return null;

        try
        {
            var json = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };

            var playlist = JsonSerializer.Deserialize<Playlist>(json, options);
            return playlist;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Loads a playlist by its ID.
    /// </summary>
    /// <param name="playlistId">The unique identifier of the playlist.</param>
    /// <returns>The playlist if found, null otherwise.</returns>
    public async Task<Playlist?> LoadPlaylistByIdAsync(string playlistId)
    {
        var allPlaylists = await LoadAllPlaylistsAsync().ConfigureAwait(false);
        return allPlaylists.FirstOrDefault(p => p.Id == playlistId);
    }

    /// <summary>
    /// Saves a playlist to a JSON file.
    /// </summary>
    /// <param name="playlist">The playlist to save.</param>
    public async Task SavePlaylistAsync(Playlist playlist)
    {
        EnsurePlaylistsDirectory();

        playlist.ModifiedAt = DateTime.Now;
        var filePath = GetPlaylistFilePath(playlist.Id);

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(playlist, options);
        await File.WriteAllTextAsync(filePath, json).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates a new playlist.
    /// </summary>
    /// <param name="name">The name of the playlist.</param>
    /// <returns>The newly created playlist.</returns>
    public async Task<Playlist> CreatePlaylistAsync(string name)
    {
        var playlist = new Playlist
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            CreatedAt = DateTime.Now,
            ModifiedAt = DateTime.Now
        };

        await SavePlaylistAsync(playlist).ConfigureAwait(false);
        return playlist;
    }

    /// <summary>
    /// Updates an existing playlist.
    /// </summary>
    /// <param name="playlist">The playlist to update.</param>
    public async Task UpdatePlaylistAsync(Playlist playlist)
    {
        playlist.ModifiedAt = DateTime.Now;
        await SavePlaylistAsync(playlist).ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes a playlist.
    /// </summary>
    /// <param name="playlistId">The ID of the playlist to delete.</param>
    public async Task DeletePlaylistAsync(string playlistId)
    {
        var filePath = GetPlaylistFilePath(playlistId);
        if (File.Exists(filePath))
        {
            try
            {
                File.Delete(filePath);
            }
            catch
            {
                // Ignore deletion errors
            }
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Adds a wallpaper entry to a playlist.
    /// </summary>
    /// <param name="playlistId">The ID of the playlist.</param>
    /// <param name="entry">The wallpaper entry to add.</param>
    public async Task AddWallpaperEntryAsync(string playlistId, PlaylistWallpaperEntry entry)
    {
        var playlist = await LoadPlaylistByIdAsync(playlistId).ConfigureAwait(false);
        if (playlist == null)
            throw new InvalidOperationException($"Playlist with ID {playlistId} not found.");

        playlist.WallpaperEntries.Add(entry);
        await UpdatePlaylistAsync(playlist).ConfigureAwait(false);
    }

    /// <summary>
    /// Removes a wallpaper entry from a playlist.
    /// </summary>
    /// <param name="playlistId">The ID of the playlist.</param>
    /// <param name="entryIndex">The index of the entry to remove.</param>
    public async Task RemoveWallpaperEntryAsync(string playlistId, int entryIndex)
    {
        var playlist = await LoadPlaylistByIdAsync(playlistId).ConfigureAwait(false);
        if (playlist == null)
            throw new InvalidOperationException($"Playlist with ID {playlistId} not found.");

        if (entryIndex >= 0 && entryIndex < playlist.WallpaperEntries.Count)
        {
            playlist.WallpaperEntries.RemoveAt(entryIndex);
            await UpdatePlaylistAsync(playlist).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Gets the enabled playlist (only one playlist can be enabled at a time).
    /// Protected method - core business logic.
    /// </summary>
    /// <returns>The enabled playlist, or null if none is enabled.</returns>
    [System.Diagnostics.DebuggerStepThrough]
    public async Task<Playlist?> GetEnabledPlaylistAsync()
    {
        // Integrity check for protected business logic
        if (!CodeProtection.ValidateDistributionIntegrity())
        {
            return null; // Fail silently if integrity check fails
        }
        
        var allPlaylists = await LoadAllPlaylistsAsync().ConfigureAwait(false);
        return allPlaylists.FirstOrDefault(p => p.Enabled);
    }

    /// <summary>
    /// Enables a playlist and disables all others.
    /// </summary>
    /// <param name="playlistId">The ID of the playlist to enable.</param>
    public async Task EnablePlaylistAsync(string playlistId)
    {
        var allPlaylists = await LoadAllPlaylistsAsync().ConfigureAwait(false);

        foreach (var playlist in allPlaylists)
        {
            playlist.Enabled = playlist.Id == playlistId;
            await SavePlaylistAsync(playlist).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Disables all playlists.
    /// </summary>
    public async Task DisableAllPlaylistsAsync()
    {
        var allPlaylists = await LoadAllPlaylistsAsync().ConfigureAwait(false);

        foreach (var playlist in allPlaylists)
        {
            playlist.Enabled = false;
            await SavePlaylistAsync(playlist).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Gets the file path for a playlist based on its ID.
    /// </summary>
    /// <param name="playlistId">The playlist ID.</param>
    /// <returns>The full file path.</returns>
    private static string GetPlaylistFilePath(string playlistId)
    {
        return Path.Combine(PlaylistsDirectory, $"{playlistId}.json");
    }
}

