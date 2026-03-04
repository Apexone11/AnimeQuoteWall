using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using AnimeQuoteWall.Core.Configuration;
using AnimeQuoteWall.Core.Models;
using AnimeQuoteWall.Core.Services;

namespace AnimeQuoteWall.GUI.Pages;

/// <summary>
/// View model for history items displayed in the HistoryPage.
/// Wraps WallpaperHistoryEntry with additional display properties.
/// </summary>
public class HistoryItemViewModel
{
    /// <summary>
    /// Path to the wallpaper image file.
    /// </summary>
    public string ImagePath { get; set; } = string.Empty;
    
    /// <summary>
    /// Timestamp when the wallpaper was generated.
    /// </summary>
    public DateTime Timestamp { get; set; }
    
    /// <summary>
    /// Path to thumbnail image (same as ImagePath for now).
    /// </summary>
    public string ThumbnailPath { get; set; } = string.Empty;
    
    /// <summary>
    /// The underlying history entry with full metadata.
    /// </summary>
    public WallpaperHistoryEntry Entry { get; set; } = null!;
}

/// <summary>
/// Page for viewing and managing wallpaper history.
/// 
/// Features:
/// - Display thumbnails of previously generated wallpapers
/// - Restore previous wallpapers
/// - Delete wallpapers from history
/// - View wallpaper metadata (timestamp, quote, etc.)
/// </summary>
public partial class HistoryPage : Page
{
    /// <summary>
    /// Service for managing wallpaper history.
    /// </summary>
    private readonly WallpaperHistoryService _historyService = new();
    
    /// <summary>
    /// List of wallpaper history entries loaded from metadata.
    /// </summary>
    private List<WallpaperHistoryEntry> _historyEntries = new();

    /// <summary>
    /// Initializes a new instance of the HistoryPage.
    /// </summary>
    public HistoryPage()
    {
        InitializeComponent();
        Loaded += async (s, e) => await LoadHistoryAsync();
    }

    /// <summary>
    /// Loads wallpaper history entries and displays them in the grid.
    /// 
    /// Process:
    /// 1. Load entries from history service
    /// 2. Sort by timestamp (newest first)
    /// 3. Create view models for display
    /// 4. Update UI with history items
    /// 
    /// </summary>
    private async Task LoadHistoryAsync()
    {
        try
        {
            // Load history entries from metadata file
            _historyEntries = await _historyService.LoadHistoryEntriesAsync().ConfigureAwait(false);
            // Sort by timestamp descending (newest first)
            _historyEntries = _historyEntries.OrderByDescending(e => e.Timestamp).ToList();

            // Update UI on UI thread
            Dispatcher.Invoke(() =>
            {
                if (HistoryItemsControl != null)
                {
                    // Create view models for each entry
                    var items = _historyEntries.Select(e => new HistoryItemViewModel
                    {
                        ImagePath = e.ImagePath,
                        Timestamp = e.Timestamp,
                        ThumbnailPath = e.ImagePath, // Use same path for thumbnail (could be optimized later)
                        Entry = e
                    }).ToList();

                    // Set items source to display in grid
                    HistoryItemsControl.ItemsSource = items;
                }
            });
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to load history: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Handles the Restore button click event.
    /// Copies the selected wallpaper to the current wallpaper path.
    /// </summary>
    private void RestoreButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is System.Windows.Controls.Button button && button.Tag != null)
            {
                // Get entry from Tag property
                var entry = button.Tag as WallpaperHistoryEntry;
                if (entry != null && File.Exists(entry.ImagePath))
                {
                    // Copy wallpaper to current wallpaper path (overwrites current)
                    File.Copy(entry.ImagePath, AppConfiguration.CurrentWallpaperPath, overwrite: true);
                    System.Windows.MessageBox.Show("Wallpaper restored!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to restore: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Handles the Delete button click event.
    /// Removes the wallpaper from history after user confirmation.
    /// </summary>
    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is System.Windows.Controls.Button button && button.Tag != null)
            {
                var entry = button.Tag as WallpaperHistoryEntry;
                if (entry != null)
                {
                    // Confirm deletion with user
                    var result = System.Windows.MessageBox.Show("Are you sure you want to delete this wallpaper from history?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        // Delete from history (removes both image and metadata entry)
                        _historyService.DeleteFromHistoryAsync(entry).Wait();
                        // Reload history to update display
                        LoadHistoryAsync();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to delete: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Handles mouse click on history item.
    /// Currently unused but can be extended to show full preview.
    /// </summary>
    private void HistoryItem_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        // Could open full preview dialog here in the future
    }
}

