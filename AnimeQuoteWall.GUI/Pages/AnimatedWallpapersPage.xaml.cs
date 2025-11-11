using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media.Imaging;
using AnimeQuoteWall.Core.Configuration;
using AnimeQuoteWall.Core.Services;

namespace AnimeQuoteWall.GUI.Pages;

/// <summary>
/// Model for displaying animated wallpaper information.
/// </summary>
public class AnimatedWallpaperItem
{
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string PreviewPath { get; set; } = string.Empty;
    public string FileSize { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
}

/// <summary>
/// Page for managing animated wallpapers library (GIFs, MP4s, etc.).
/// 
/// Features:
/// - Display library of animated wallpapers
/// - Add new animated wallpapers (GIF, MP4)
/// - Delete animated wallpapers
/// - Preview animated wallpapers
/// - View animated wallpaper count
/// </summary>
public partial class AnimatedWallpapersPage : Page
{
    /// <summary>
    /// Directory for storing animated wallpapers.
    /// </summary>
    private readonly string _animatedWallpapersDirectory;

    /// <summary>
    /// Currently selected animated wallpaper item.
    /// </summary>
    private AnimatedWallpaperItem? _selectedItem;

    /// <summary>
    /// Video thumbnail service for generating MP4 thumbnails.
    /// </summary>
    private readonly VideoThumbnailService _thumbnailService;

    /// <summary>
    /// Supported animated file extensions.
    /// </summary>
    private static readonly string[] SupportedAnimatedExtensions = { ".gif", ".mp4", ".webm", ".mov" };

    /// <summary>
    /// Initializes a new instance of the AnimatedWallpapersPage.
    /// </summary>
    public AnimatedWallpapersPage()
    {
        InitializeComponent();
        _animatedWallpapersDirectory = Path.Combine(AppConfiguration.DefaultBaseDirectory, "animated_wallpapers");
        Directory.CreateDirectory(_animatedWallpapersDirectory);
        _thumbnailService = new VideoThumbnailService();
        
        // Load wallpapers asynchronously to improve startup performance
        Loaded += async (s, e) => await LoadAnimatedWallpapersAsync();
    }

    /// <summary>
    /// Loads all animated wallpapers from the directory and displays them asynchronously.
    /// </summary>
    private async System.Threading.Tasks.Task LoadAnimatedWallpapersAsync()
    {
        try
        {
            // Show loading indicator
            await Dispatcher.InvokeAsync(() =>
            {
                if (LoadingIndicator != null)
                    LoadingIndicator.Visibility = Visibility.Visible;
                if (ContentScrollViewer != null)
                    ContentScrollViewer.Visibility = Visibility.Collapsed;
                AnimatedWallpapersItemsControl.ItemsSource = null;
                UpdateCount(0);
            });

            if (!Directory.Exists(_animatedWallpapersDirectory))
            {
                Directory.CreateDirectory(_animatedWallpapersDirectory);
                AnimatedWallpapersItemsControl.ItemsSource = new List<AnimatedWallpaperItem>();
                UpdateCount(0);
                return;
            }

            // Load file list on background thread
            var files = await System.Threading.Tasks.Task.Run(() =>
            {
                return Directory.GetFiles(_animatedWallpapersDirectory)
                    .Where(f => SupportedAnimatedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                    .ToList();
            });

            // Create items with thumbnails (process in batches for better performance)
            var items = new List<AnimatedWallpaperItem>();
            
            // Process files in parallel batches
            const int batchSize = 5;
            for (int i = 0; i < files.Count; i += batchSize)
            {
                var batch = files.Skip(i).Take(batchSize);
                var batchItems = await System.Threading.Tasks.Task.Run(() =>
                {
                    return batch.Select(path => CreateWallpaperItem(path)).ToList();
                });
                items.AddRange(batchItems);
                
                // Update UI incrementally for better perceived performance
                await Dispatcher.InvokeAsync(() =>
                {
                    AnimatedWallpapersItemsControl.ItemsSource = items.ToList();
                    UpdateCount(items.Count);
                });
            }

            // Final update and hide loading indicator
            await Dispatcher.InvokeAsync(() =>
            {
                AnimatedWallpapersItemsControl.ItemsSource = items;
                UpdateCount(items.Count);
                if (LoadingIndicator != null)
                    LoadingIndicator.Visibility = Visibility.Collapsed;
                if (ContentScrollViewer != null)
                    ContentScrollViewer.Visibility = Visibility.Visible;
            });
        }
        catch (Exception ex)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                if (LoadingIndicator != null)
                    LoadingIndicator.Visibility = Visibility.Collapsed;
                if (ContentScrollViewer != null)
                    ContentScrollViewer.Visibility = Visibility.Visible;
                System.Windows.MessageBox.Show($"Failed to load animated wallpapers: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }
    }

    /// <summary>
    /// Creates an AnimatedWallpaperItem with thumbnail generation.
    /// </summary>
    private AnimatedWallpaperItem CreateWallpaperItem(string path)
    {
        var fileInfo = new FileInfo(path);
        var sizeInMB = fileInfo.Length / (1024.0 * 1024.0);
        var extension = Path.GetExtension(path).ToLowerInvariant();
        
        // Generate preview path
        string previewPath;
        if (extension == ".gif")
        {
            // GIFs can be displayed directly
            previewPath = path;
        }
        else
        {
            // For videos, generate or get cached thumbnail
            previewPath = _thumbnailService.GetOrCreateThumbnail(path) ?? path;
        }
        
        return new AnimatedWallpaperItem
        {
            FileName = Path.GetFileName(path),
            FilePath = path,
            PreviewPath = previewPath,
            FileSize = sizeInMB < 1 
                ? $"{(fileInfo.Length / 1024.0):F1} KB" 
                : $"{sizeInMB:F2} MB",
            Format = extension.ToUpperInvariant().Substring(1),
            Duration = GetDurationInfo(path, extension)
        };
    }

    /// <summary>
    /// Loads all animated wallpapers synchronously (for compatibility).
    /// </summary>
    private void LoadAnimatedWallpapers()
    {
        // Redirect to async version
        _ = LoadAnimatedWallpapersAsync();
    }

    /// <summary>
    /// Gets duration information for the animated file.
    /// </summary>
    private string GetDurationInfo(string filePath, string extension)
    {
        // For GIFs, we can't easily determine duration without parsing
        // For videos, we'd need FFmpeg or similar
        // For now, return a placeholder
        return extension == ".gif" ? "Animated GIF" : "Video File";
    }

    /// <summary>
    /// Updates the count display.
    /// </summary>
    private void UpdateCount(int count)
    {
        var countRun = AnimatedCountText.Inlines.FirstOrDefault(i => i is Run r && r.Name == "AnimatedCountRun") as Run;
        if (countRun != null)
        {
            countRun.Text = $"{count}";
        }
        else
        {
            AnimatedCountText.Text = $"Total Animated Wallpapers: {count}";
        }
    }

    /// <summary>
    /// Handles the Add Animated button click event.
    /// Opens a file dialog to select animated files and copies them to the directory.
    /// </summary>
    private async void AddAnimatedButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select Animated Wallpapers",
            Filter = "Animated Files (*.gif;*.mp4;*.webm;*.mov)|*.gif;*.mp4;*.webm;*.mov|GIF Files (*.gif)|*.gif|Video Files (*.mp4;*.webm;*.mov)|*.mp4;*.webm;*.mov",
            Multiselect = true
        };

        if (dialog.ShowDialog() == true)
        {
            int copiedCount = 0;

            foreach (var file in dialog.FileNames)
            {
                try
                {
                    var fileName = Path.GetFileName(file);
                    var destPath = Path.Combine(_animatedWallpapersDirectory, fileName);

                    if (!File.Exists(destPath))
                    {
                        File.Copy(file, destPath);
                        copiedCount++;
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Failed to copy {Path.GetFileName(file)}: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            await LoadAnimatedWallpapersAsync();

            if (copiedCount > 0)
            {
                System.Windows.MessageBox.Show($"{copiedCount} animated wallpaper(s) added!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }

    /// <summary>
    /// Handles the Delete Animated button click event.
    /// </summary>
    private async void DeleteAnimatedButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedItem == null || string.IsNullOrWhiteSpace(_selectedItem.FilePath))
        {
            System.Windows.MessageBox.Show("Please select an animated wallpaper to delete.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var result = System.Windows.MessageBox.Show(
            $"Are you sure you want to delete '{_selectedItem.FileName}'?",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                if (File.Exists(_selectedItem.FilePath))
                {
                    File.Delete(_selectedItem.FilePath);
                    
                    // Also delete thumbnail if it exists
                    try
                    {
                        var thumbnailPath = _thumbnailService.GetOrCreateThumbnail(_selectedItem.FilePath);
                        if (!string.IsNullOrEmpty(thumbnailPath) && File.Exists(thumbnailPath))
                        {
                            File.Delete(thumbnailPath);
                        }
                    }
                    catch { }
                    
                    _selectedItem = null;
                    await LoadAnimatedWallpapersAsync();
                    
                    if (ApplyAnimatedButton != null)
                        ApplyAnimatedButton.IsEnabled = false;
                    if (DeleteAnimatedButton != null)
                        DeleteAnimatedButton.IsEnabled = false;

                    System.Windows.MessageBox.Show("Animated wallpaper deleted successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to delete animated wallpaper: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    /// <summary>
    /// Handles mouse enter on animated item.
    /// </summary>
    private void AnimatedItem_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (sender is Border border)
        {
            border.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(20, 99, 102, 241));
        }
    }

    /// <summary>
    /// Handles mouse leave on animated item.
    /// </summary>
    private void AnimatedItem_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (sender is Border border)
        {
            border.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 30, 41, 59)); // CardBackground color
        }
    }

    /// <summary>
    /// Handles mouse click on animated item.
    /// </summary>
    private void AnimatedItem_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        // Select the item
        if (sender is Border border && border.DataContext is AnimatedWallpaperItem item)
        {
            _selectedItem = item;
            // Update button states
            if (ApplyAnimatedButton != null)
                ApplyAnimatedButton.IsEnabled = true;
            if (DeleteAnimatedButton != null)
                DeleteAnimatedButton.IsEnabled = true;
        }
    }

    /// <summary>
    /// Handles the Apply Animated Wallpaper button click event.
    /// </summary>
    private void ApplyAnimatedButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedItem == null || string.IsNullOrWhiteSpace(_selectedItem.FilePath))
        {
            System.Windows.MessageBox.Show("Please select an animated wallpaper first.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var service = new AnimatedWallpaperService();
            var extension = System.IO.Path.GetExtension(_selectedItem.FilePath).ToLowerInvariant();
            
            // Check if Wallpaper Engine is available for video files
            if ((extension == ".mp4" || extension == ".webm" || extension == ".mov") && !service.IsWallpaperEngineAvailable())
            {
                var status = service.GetWallpaperEngineStatus();
                var result = System.Windows.MessageBox.Show(
                    $"Wallpaper Engine is not available.\n\nStatus: {status}\n\n" +
                    "Animated wallpapers (MP4/WebM/MOV) require Wallpaper Engine to work.\n\n" +
                    "Options:\n" +
                    "1. Install Wallpaper Engine from Steam (recommended)\n" +
                    "2. Extract first frame as static wallpaper\n\n" +
                    "Would you like to extract the first frame as a static wallpaper instead?",
                    "Wallpaper Engine Required",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    // Extract first frame and set as static
                    var success = service.SetAnimatedWallpaper(_selectedItem.FilePath);
                    if (success)
                    {
                        System.Windows.MessageBox.Show(
                            "First frame extracted and set as static wallpaper.\n\n" +
                            "Note: To have animated wallpapers, please install Wallpaper Engine from Steam.",
                            "Static Wallpaper Applied",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("Failed to extract frame. Please ensure the file is valid.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                return;
            }

            // Try to apply animated wallpaper
            var wallpaperSuccess = service.SetAnimatedWallpaper(_selectedItem.FilePath);

            if (wallpaperSuccess)
            {
                if (extension == ".gif" && !service.IsWallpaperEngineAvailable())
                {
                    System.Windows.MessageBox.Show(
                        $"First frame of '{_selectedItem.FileName}' applied as static wallpaper.\n\n" +
                        "Note: GIF animations require Wallpaper Engine to display as animated wallpapers. " +
                        "Install Wallpaper Engine from Steam for full animated wallpaper support.",
                        "Static Wallpaper Applied",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    System.Windows.MessageBox.Show(
                        $"Animated wallpaper '{_selectedItem.FileName}' applied successfully!\n\n" +
                        "If the wallpaper is not animating, ensure Wallpaper Engine is running.",
                        "Success",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            else
            {
                var errorMessage = extension == ".gif"
                    ? "Failed to apply animated wallpaper. The GIF file may be corrupted or Wallpaper Engine is not running."
                    : "Failed to apply animated wallpaper. Please ensure:\n" +
                      "1. Wallpaper Engine is installed and running\n" +
                      "2. The file format is supported (MP4, WebM, MOV)\n" +
                      "3. Wallpaper Engine has permission to access the file";

                System.Windows.MessageBox.Show(errorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to apply animated wallpaper: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

