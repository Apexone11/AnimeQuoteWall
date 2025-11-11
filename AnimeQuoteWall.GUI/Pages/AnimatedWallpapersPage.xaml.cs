using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
    /// Currently selected border element for visual feedback.
    /// </summary>
    private System.Windows.Controls.Border? _selectedBorder;

    /// <summary>
    /// Video thumbnail service for generating MP4 thumbnails.
    /// </summary>
    private readonly VideoThumbnailService _thumbnailService;

    /// <summary>
    /// Service for detecting monitors.
    /// </summary>
    private readonly MonitorService _monitorService = new MonitorService();

    /// <summary>
    /// Cancellation token source for async operations.
    /// </summary>
    private CancellationTokenSource? _cancellationTokenSource;

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
        Loaded += async (s, e) =>
        {
            InitializeMonitorSelection();
            UpdateAnimatedApplyButtonState();
            await LoadAnimatedWallpapersAsync();
        };
        
        Unloaded += (s, e) => _cancellationTokenSource?.Cancel();
    }

    /// <summary>
    /// Loads all animated wallpapers from the directory and displays them asynchronously.
    /// </summary>
    private async Task LoadAnimatedWallpapersAsync()
    {
        // Cancel any previous loading operation
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = _cancellationTokenSource.Token;

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
                await Dispatcher.InvokeAsync(() =>
                {
                    AnimatedWallpapersItemsControl.ItemsSource = new List<AnimatedWallpaperItem>();
                    UpdateCount(0);
                    if (LoadingIndicator != null)
                        LoadingIndicator.Visibility = Visibility.Collapsed;
                    if (ContentScrollViewer != null)
                        ContentScrollViewer.Visibility = Visibility.Visible;
                });
                return;
            }

            // Load file list on background thread
            List<string> files;
            try
            {
                files = await Task.Run(() =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return Directory.GetFiles(_animatedWallpapersDirectory)
                        .Where(f => SupportedAnimatedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                        .ToList();
                }, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return; // User navigated away
            }

            if (cancellationToken.IsCancellationRequested)
                return;

            // Create items with thumbnails (process in batches for better performance)
            var items = new List<AnimatedWallpaperItem>();
            
            // Process files in parallel batches
            const int batchSize = 5;
            for (int i = 0; i < files.Count; i += batchSize)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                var batch = files.Skip(i).Take(batchSize);
                List<AnimatedWallpaperItem> batchItems;
                try
                {
                    batchItems = await Task.Run(() =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        return batch.Select(path =>
                        {
                            try
                            {
                                return CreateWallpaperItem(path);
                            }
                            catch
                            {
                                // Skip files that can't be processed
                                return null;
                            }
                        }).Where(item => item != null).Cast<AnimatedWallpaperItem>().ToList();
                    }, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                items.AddRange(batchItems);
                
                // Update UI incrementally for better perceived performance
                if (!cancellationToken.IsCancellationRequested)
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        if (cancellationToken.IsCancellationRequested)
                            return;
                        AnimatedWallpapersItemsControl.ItemsSource = items.ToList();
                        UpdateCount(items.Count);
                    });
                }
            }

            if (cancellationToken.IsCancellationRequested)
                return;

            // Final update and hide loading indicator
            await Dispatcher.InvokeAsync(() =>
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                AnimatedWallpapersItemsControl.ItemsSource = items;
                UpdateCount(items.Count);
                
                // Update grid columns based on available width
                UpdateAnimatedGridColumns();
                
                if (LoadingIndicator != null)
                    LoadingIndicator.Visibility = Visibility.Collapsed;
                if (ContentScrollViewer != null)
                    ContentScrollViewer.Visibility = Visibility.Visible;
                
                // Reset selection
                _selectedItem = null;
                _selectedBorder = null;
                if (ApplyAnimatedButton != null)
                    ApplyAnimatedButton.IsEnabled = false;
                if (DeleteAnimatedButton != null)
                    DeleteAnimatedButton.IsEnabled = false;
            });
        }
        catch (OperationCanceledException)
        {
            // User navigated away - ignore
        }
        catch (Exception ex)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                if (LoadingIndicator != null)
                    LoadingIndicator.Visibility = Visibility.Collapsed;
                if (ContentScrollViewer != null)
                    ContentScrollViewer.Visibility = Visibility.Visible;
                
                System.Windows.MessageBox.Show(
                    $"Failed to load animated wallpapers: {ex.Message}\n\nPlease check that the animated wallpapers folder exists and is accessible.",
                    "Error Loading Animated Wallpapers",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
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
    /// Temporarily disabled - library is read-only.
    /// </summary>
    private async void AddAnimatedButton_Click(object sender, RoutedEventArgs e)
    {
        System.Windows.MessageBox.Show(
            "Adding animated wallpapers is temporarily disabled.\n\n" +
            "This feature is read-only for stability and will return in a future update.",
            "Feature Temporarily Disabled",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
        return;
        
        /* Temporarily disabled
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
        */
    }

    /// <summary>
    /// Handles the Delete Animated button click event.
    /// Temporarily disabled - library is read-only.
    /// </summary>
    private async void DeleteAnimatedButton_Click(object sender, RoutedEventArgs e)
    {
        System.Windows.MessageBox.Show(
            "Deleting animated wallpapers is temporarily disabled.\n\n" +
            "This feature is read-only for stability and will return in a future update.",
            "Feature Temporarily Disabled",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
        return;
        
        /* Temporarily disabled
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

                    // Reset selection
                    _selectedItem = null;
                    _selectedBorder = null;

                    System.Windows.MessageBox.Show("Animated wallpaper deleted successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to delete animated wallpaper: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        */
    }

    /// <summary>
    /// Handles mouse enter on animated item.
    /// </summary>
    private void AnimatedItem_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (sender is System.Windows.Controls.Border border && border != _selectedBorder)
        {
            // Only change background if not selected
            var cardBg = System.Windows.Application.Current.Resources["CardBackground"];
            if (cardBg is System.Windows.Media.SolidColorBrush brush)
            {
                var color = brush.Color;
                border.Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromArgb(255, 
                        (byte)Math.Min(255, color.R + 10), 
                        (byte)Math.Min(255, color.G + 10), 
                        (byte)Math.Min(255, color.B + 10)));
            }
        }
    }

    /// <summary>
    /// Handles mouse leave on animated item.
    /// </summary>
    private void AnimatedItem_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (sender is System.Windows.Controls.Border border && border != _selectedBorder)
        {
            // Reset to default background if not selected
            var cardBg = System.Windows.Application.Current.Resources["CardBackground"];
            if (cardBg is System.Windows.Media.SolidColorBrush)
            {
                border.SetResourceReference(System.Windows.Controls.Border.BackgroundProperty, "CardBackground");
            }
        }
    }

    /// <summary>
    /// Handles mouse click on animated item.
    /// </summary>
    private void AnimatedItem_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        // Deselect previous item
        if (_selectedBorder != null)
        {
            _selectedBorder.BorderThickness = new Thickness(2);
            _selectedBorder.BorderBrush = (System.Windows.Media.Brush)System.Windows.Application.Current.Resources["BorderColor"];
            var cardBg = System.Windows.Application.Current.Resources["CardBackground"];
            if (cardBg is System.Windows.Media.SolidColorBrush)
            {
                _selectedBorder.SetResourceReference(System.Windows.Controls.Border.BackgroundProperty, "CardBackground");
            }
        }

        // Select the new item
        if (sender is System.Windows.Controls.Border border && border.DataContext is AnimatedWallpaperItem item)
        {
            _selectedItem = item;
            _selectedBorder = border;
            
            // Visual feedback for selection
            border.BorderThickness = new Thickness(3);
            border.BorderBrush = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(129, 140, 248)); // Purple accent
            border.Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromArgb(30, 129, 140, 248)); // Light purple tint
            
            // Update button states
            if (ApplyAnimatedButton != null)
            {
                // Only enable if feature flag is enabled
                ApplyAnimatedButton.IsEnabled = AppConfiguration.EnableAnimatedApply && _selectedItem != null;
            }
            if (DeleteAnimatedButton != null)
                DeleteAnimatedButton.IsEnabled = true;
        }
    }

    /// <summary>
    /// Handles the Remove Animated Wallpaper button click event.
    /// Clears the animated wallpaper and reverts to the previous static wallpaper.
    /// </summary>
    private async void RemoveAnimatedButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var monitor = GetSelectedMonitor();
            var animatedService = new AnimatedWallpaperService();
            
            // First, clear the animated wallpaper from Wallpaper Engine
            var cleared = animatedService.ClearAnimatedWallpaper(monitor);
            
            if (!cleared && await animatedService.IsWallpaperEngineAvailableAsync().ConfigureAwait(true))
            {
                // If clearing failed but Wallpaper Engine is available, warn the user
                var result = System.Windows.MessageBox.Show(
                    "Could not automatically clear the animated wallpaper from Wallpaper Engine.\n\n" +
                    "You may need to manually stop it in Wallpaper Engine, or the wallpaper may still be active.\n\n" +
                    "Do you want to continue and try to apply the static wallpaper anyway?",
                    "Warning",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                
                if (result == MessageBoxResult.No)
                {
                    return;
                }
            }
            
            // Small delay to ensure Wallpaper Engine processes the clear command
            await Task.Delay(500).ConfigureAwait(true);
            
            // Now try to apply the previous static wallpaper
            var prev = AppConfiguration.PreviousWallpaperPath;
            
            if (File.Exists(prev))
            {
                if (WallpaperSettingHelper.SetWallpaper(prev, monitor))
                {
                    System.Windows.MessageBox.Show(
                        "Animated wallpaper cleared and reverted to previous static wallpaper.",
                        "Success",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    System.Windows.MessageBox.Show(
                        "Animated wallpaper cleared, but failed to apply static wallpaper.\n\n" +
                        "Try applying a static wallpaper manually.",
                        "Partial Success",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
            else
            {
                if (cleared)
                {
                    System.Windows.MessageBox.Show(
                        "Animated wallpaper cleared.\n\n" +
                        "No previous static wallpaper found. Generate a static wallpaper to apply.",
                        "Info",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    System.Windows.MessageBox.Show(
                        "No previous wallpaper found. Generate a static wallpaper first.",
                        "Info",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Failed to remove animated wallpaper: {ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Updates the animated apply button state based on feature flag.
    /// </summary>
    private void UpdateAnimatedApplyButtonState()
    {
        try
        {
            if (ApplyAnimatedButton == null) return;
            
            var isEnabled = AppConfiguration.EnableAnimatedApply;
            ApplyAnimatedButton.IsEnabled = isEnabled;
            
            if (!isEnabled)
            {
                ApplyAnimatedButton.ToolTip = "Animated wallpaper apply is temporarily disabled for stability.\nThis feature will return in a future update.";
            }
            else
            {
                ApplyAnimatedButton.ToolTip = "Apply the selected animated wallpaper to your desktop";
            }
        }
        catch { /* ignore */ }
    }

    /// <summary>
    /// Handles the Apply Animated Wallpaper button click event.
    /// </summary>
    private void ApplyAnimatedButton_Click(object sender, RoutedEventArgs e)
    {
        // Check feature flag first
        if (!AppConfiguration.EnableAnimatedApply)
        {
            System.Windows.MessageBox.Show(
                "Animated wallpaper apply is temporarily disabled for stability.\n\n" +
                "This feature will return in a future update.\n\n" +
                "You can still manage your animated wallpaper library and view thumbnails.",
                "Feature Temporarily Disabled",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

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
                    var monitorIndex = GetSelectedMonitor();
                    var success = service.SetAnimatedWallpaper(_selectedItem.FilePath, monitorIndex);
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

            // Get selected monitor
            var selectedMonitor = GetSelectedMonitor();
            
            // Try to apply animated wallpaper with monitor selection
            var wallpaperSuccess = service.SetAnimatedWallpaper(_selectedItem.FilePath, selectedMonitor);

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
                    var monitorName = GetSelectedMonitorName();
                    var message = string.IsNullOrEmpty(monitorName)
                        ? $"Animated wallpaper '{_selectedItem.FileName}' applied successfully!\n\n" +
                          "If the wallpaper is not animating, ensure Wallpaper Engine is running."
                        : $"Animated wallpaper '{_selectedItem.FileName}' applied to {monitorName}!\n\n" +
                          "If the wallpaper is not animating, ensure Wallpaper Engine is running.";
                    
                    System.Windows.MessageBox.Show(
                        message,
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

    /// <summary>
    /// Initializes the monitor selection combo box.
    /// </summary>
    private void InitializeMonitorSelection()
    {
        try
        {
            if (MonitorSelectionComboBox == null) return;

            MonitorSelectionComboBox.Items.Clear();
            
            // Add default option
            var defaultItem = new ComboBoxItem
            {
                Content = "Use Settings Default",
                Tag = "Default"
            };
            MonitorSelectionComboBox.Items.Add(defaultItem);

            // Add monitor options
            var monitors = _monitorService.GetAllMonitors();
            foreach (var monitor in monitors)
            {
                // Create a more compact display name
                var displayName = monitor.Name;
                if (displayName.Length > 20)
                {
                    // Shorten long display names
                    displayName = displayName.Substring(0, 17) + "...";
                }
                
                var item = new ComboBoxItem
                {
                    Content = $"{displayName} ({monitor.Width}x{monitor.Height}){(monitor.IsPrimary ? " [Primary]" : "")}",
                    Tag = monitor.Index,
                    ToolTip = $"{monitor.Name} - {monitor.Width}x{monitor.Height}{(monitor.IsPrimary ? " (Primary Monitor)" : "")}"
                };
                MonitorSelectionComboBox.Items.Add(item);
            }

            // Select default option
            MonitorSelectionComboBox.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error initializing monitor selection: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the selected monitor index from the combo box, or null if using default.
    /// </summary>
    private int? GetSelectedMonitor()
    {
        try
        {
            if (MonitorSelectionComboBox?.SelectedItem is ComboBoxItem item)
            {
                if (item.Tag is int index)
                    return index;
                if (item.Tag is string tag && tag == "Default")
                    return null;
            }
        }
        catch { /* ignore */ }
        return null;
    }

    /// <summary>
    /// Gets the selected monitor name for display purposes.
    /// </summary>
    private string GetSelectedMonitorName()
    {
        try
        {
            var selectedMonitor = GetSelectedMonitor();
            if (selectedMonitor.HasValue)
            {
                var monitor = _monitorService.GetMonitorByIndex(selectedMonitor.Value);
                return monitor?.Name ?? "";
            }
        }
        catch { /* ignore */ }
        return "";
    }

    /// <summary>
    /// Updates the animated wallpapers grid columns based on available width.
    /// </summary>
    private void UpdateAnimatedGridColumns()
    {
        try
        {
            if (AnimatedWallpapersItemsControl == null || ContentScrollViewer == null)
                return;

            var itemCount = AnimatedWallpapersItemsControl.Items.Count;
            if (itemCount == 0)
                return;

            // Get available width
            var availableWidth = ContentScrollViewer.ActualWidth;
            if (availableWidth <= 0)
                availableWidth = ContentScrollViewer.Width;
            if (availableWidth <= 0)
                availableWidth = 800; // Fallback

            // Calculate optimal columns based on width
            // Each animated wallpaper card needs ~280-320px width (including margins)
            int columns;
            if (availableWidth >= 1400)
                columns = 5; // 5 columns for very wide screens
            else if (availableWidth >= 1200)
                columns = 4; // 4 columns for wide screens
            else if (availableWidth >= 900)
                columns = 3; // 3 columns for medium screens
            else if (availableWidth >= 600)
                columns = 2; // 2 columns for narrow screens
            else
                columns = 1; // 1 column for very narrow screens

            // Apply the updated panel
            var itemsPanelTemplate = new ItemsPanelTemplate();
            var factory = new FrameworkElementFactory(typeof(UniformGrid));
            factory.SetValue(UniformGrid.ColumnsProperty, columns);
            factory.SetValue(UniformGrid.RowsProperty, 0);
            itemsPanelTemplate.VisualTree = factory;
            AnimatedWallpapersItemsControl.ItemsPanel = itemsPanelTemplate;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"UpdateAnimatedGridColumns error: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles ScrollViewer size changes to update grid columns.
    /// </summary>
    private void ContentScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateAnimatedGridColumns();
    }
}

