using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Imaging;
using AnimeQuoteWall.Core.Configuration;
using AnimeQuoteWall.Core.Interfaces;
using AnimeQuoteWall.Core.Models;
using AnimeQuoteWall.Core.Services;

namespace AnimeQuoteWall.GUI.Pages;

/// <summary>
/// Model for displaying monitor preview information.
/// </summary>
public class MonitorPreviewItem
{
    public int MonitorIndex { get; set; }
    public string MonitorName { get; set; } = string.Empty;
    public BitmapImage? PreviewImage { get; set; }
    public string QuoteText { get; set; } = string.Empty;
    public string WallpaperPath { get; set; } = string.Empty;
}

/// <summary>
/// Page for generating, previewing, and applying wallpapers.
/// 
/// Features:
/// - Generate new wallpapers with random quotes and backgrounds
/// - Generate different wallpapers for each monitor
/// - Multi-monitor preview showing all monitors
/// - Preview wallpapers with zoom and pan controls
/// - Compare current wallpaper with previous version
/// - Apply wallpapers to desktop (per-monitor support)
/// - Automatic history saving
/// </summary>
public partial class WallpaperPage : Page
{
    /// <summary>
    /// Service for managing quotes.
    /// </summary>
    private readonly IQuoteService _quoteService;
    
    /// <summary>
    /// Service for managing background images.
    /// </summary>
    private readonly IBackgroundService _backgroundService;
    
    /// <summary>
    /// Service for generating wallpaper images.
    /// </summary>
    private readonly IWallpaperService _wallpaperService;
    
    /// <summary>
    /// Service for managing wallpaper history.
    /// </summary>
    private readonly WallpaperHistoryService _historyService;

    /// <summary>
    /// Service for detecting monitors.
    /// </summary>
    private readonly MonitorService _monitorService = new MonitorService();
    
    /// <summary>
    /// List of available quotes loaded from file.
    /// </summary>
    private System.Collections.Generic.List<Quote> _quotes = new();
    
    /// <summary>
    /// Current zoom level for preview (1.0 = 100%).
    /// </summary>
    private double _currentZoom = 1.0;
    
    /// <summary>
    /// Whether currently showing previous wallpaper in comparison mode.
    /// </summary>
    private bool _isShowingPrevious = false;
    
    /// <summary>
    /// Path to the previous wallpaper (for comparison).
    /// </summary>
    private string? _previousWallpaperPath;
    
    /// <summary>
    /// Last quote used for generation (for history).
    /// </summary>
    private Quote? _lastGeneratedQuote;
    
    /// <summary>
    /// Last background path used for generation (for history).
    /// </summary>
    private string? _lastBackgroundPath;
    
    /// <summary>
    /// Last settings used for generation (for history).
    /// </summary>
    private WallpaperSettings? _lastSettings;

    /// <summary>
    /// Initializes a new instance of the WallpaperPage.
    /// </summary>
    public WallpaperPage()
    {
        InitializeComponent();
        _quoteService = new QuoteService();
        _backgroundService = new BackgroundService();
        _wallpaperService = new WallpaperService();
        _historyService = new WallpaperHistoryService();
        Loaded += async (s, e) => await InitializeAsync();
    }

    /// <summary>
    /// Initializes the page by loading quotes and displaying current wallpaper.
    /// </summary>
    private async Task InitializeAsync()
    {
        try
        {
            // Ensure quotes file exists and load quotes
            await _quoteService.EnsureQuotesFileAsync(AppConfiguration.QuotesFilePath).ConfigureAwait(false);
            _quotes = await _quoteService.LoadQuotesAsync(AppConfiguration.QuotesFilePath).ConfigureAwait(false);
        }
        catch { /* ignore errors during initialization */ }
        
        // Defer LoadCurrentWallpaper until UI is fully ready
        // This prevents crashes if UI elements aren't initialized yet
        Dispatcher.BeginInvoke(new Action(() =>
        {
            LoadCurrentWallpaper();
            InitializeMonitorSelection();
        }), System.Windows.Threading.DispatcherPriority.Loaded);
    }

    /// <summary>
    /// Refreshes the quotes list from the file.
    /// Called when quotes are updated in other parts of the application.
    /// </summary>
    public async Task RefreshQuotesAsync()
    {
        try
        {
            _quotes = await _quoteService.LoadQuotesAsync(AppConfiguration.QuotesFilePath).ConfigureAwait(false);
        }
        catch { /* ignore errors */ }
    }

    /// <summary>
    /// Handles the Generate New Wallpaper button click event.
    /// 
    /// Process:
    /// 1. Validate quotes and backgrounds are available
    /// 2. Select random quote and background
    /// 3. Save current wallpaper as previous (for comparison)
    /// 4. Generate new wallpaper
    /// 5. Save to history
    /// 6. Update preview
    /// 
    /// </summary>
    private async void GenerateButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Safety check: ensure button exists
            if (GenerateButton == null) return;
            GenerateButton.IsEnabled = false;

            // Get available backgrounds
            var backgrounds = _backgroundService.GetAllBackgroundImages(AppConfiguration.BackgroundsDirectory);

            // Validate quotes are available
            if (_quotes.Count == 0)
            {
                System.Windows.MessageBox.Show("No quotes available. Please add quotes first.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Validate backgrounds are available
            if (backgrounds.Count == 0)
            {
                System.Windows.MessageBox.Show("No background images available. Please add backgrounds first.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Get selected monitor or use primary
            var selectedMonitor = GetSelectedMonitor();
            var targetMonitor = selectedMonitor ?? _monitorService.GetPrimaryMonitor()?.Index ?? 0;
            var monitor = _monitorService.GetMonitorByIndex(targetMonitor);
            
            // Get monitor-specific resolution
            var monitorWidth = monitor?.Width ?? 1920;
            var monitorHeight = monitor?.Height ?? 1080;

            // Select random quote and background for variety
            var randomQuote = _quoteService.GetRandomQuote(_quotes);
            var randomBackground = _backgroundService.GetRandomBackgroundImage(AppConfiguration.BackgroundsDirectory);

            var settings = new WallpaperSettings
            {
                Width = monitorWidth,
                Height = monitorHeight,
                BackgroundColor = "#141414",
                FontFamily = "Segoe UI",
                TextColor = "#FFFFFF",
                OutlineColor = "#000000",
                PanelColor = "#1A1A1A",
                PanelOpacity = 0.85f,
                MaxPanelWidthPercent = 0.7f,
                FontSizeFactor = 25f,
                MinFontSize = 32
            };

            // Store for history
            _lastGeneratedQuote = randomQuote;
            _lastBackgroundPath = randomBackground;
            _lastSettings = settings;

            // Get monitor-specific path
            var monitorPath = AppConfiguration.GetMonitorWallpaperFilePath(targetMonitor);
            
            // Save current wallpaper as previous before generating new one (per-monitor)
            if (File.Exists(monitorPath))
            {
                try
                {
                    var previousPath = AppConfiguration.GetMonitorPreviousWallpaperFilePath(targetMonitor);
                    File.Copy(monitorPath, previousPath, overwrite: true);
                    
                    // Also save to global previous path for backward compatibility (primary monitor only)
                    if (monitor?.IsPrimary == true)
                    {
                        File.Copy(monitorPath, AppConfiguration.PreviousWallpaperPath, overwrite: true);
                    }
                }
                catch { /* ignore if copy fails */ }
            }

            await Task.Run(() =>
            {
                using var bitmap = _wallpaperService.CreateWallpaperImage(randomBackground, randomQuote, settings);
                _wallpaperService.SaveImageAsync(bitmap, monitorPath).Wait();
            }).ConfigureAwait(false);

            // Save to per-monitor path configuration
            AppConfiguration.SetMonitorWallpaperPath(targetMonitor, monitorPath);
            
            // Also save to current path for backward compatibility (primary monitor only)
            if (monitor?.IsPrimary == true)
            {
                File.Copy(monitorPath, AppConfiguration.CurrentWallpaperPath, overwrite: true);
            }

            // Save to history
            try
            {
                await _historyService.SaveToHistoryAsync(monitorPath, randomQuote, randomBackground, settings).ConfigureAwait(false);
            }
            catch { /* ignore history save errors */ }

            // Update UI on UI thread
            Dispatcher.Invoke(() =>
            {
                LoadCurrentWallpaper();
                GenerateButton.IsEnabled = true;
                var monitorName = monitor?.Name ?? $"Monitor {targetMonitor}";
                System.Windows.MessageBox.Show($"Wallpaper generated for {monitorName}!\n\nQuote: \"{randomQuote.Text}\"\nBy: {randomQuote.Character}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            });
        }
        catch (Exception ex)
        {
            // Update UI on UI thread
            Dispatcher.Invoke(() =>
            {
                GenerateButton.IsEnabled = true;
                System.Windows.MessageBox.Show($"Failed to generate wallpaper: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }
    }

    /// <summary>
    /// Handles the Generate All Monitors button click event.
    /// Temporarily disabled - multi-monitor feature is not working properly.
    /// </summary>
    private async void GenerateAllMonitorsButton_Click(object sender, RoutedEventArgs e)
    {
        System.Windows.MessageBox.Show(
            "Multi-monitor wallpaper generation is temporarily disabled.\n\n" +
            "This feature will return in a future update once stability issues are resolved.\n\n" +
            "You can still generate and apply wallpapers to individual monitors.",
            "Feature Temporarily Disabled",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        LoadCurrentWallpaper();
            System.Windows.MessageBox.Show("Preview refreshed!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    /// <summary>
    /// Handles the Remove Generated button click event.
    /// Deletes the current generated wallpaper for the selected monitor and restores the previous one if available.
    /// </summary>
    private void RemoveGeneratedButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Get selected monitor or use primary monitor as default
            var selectedMonitor = GetSelectedMonitor();
            var targetMonitor = selectedMonitor ?? _monitorService.GetPrimaryMonitor()?.Index ?? 0;
            var monitor = _monitorService.GetMonitorByIndex(targetMonitor);
            
            // Get per-monitor wallpaper path for the selected monitor
            var currentPath = AppConfiguration.GetMonitorWallpaperPath(targetMonitor);
            
            // Fallback to CurrentWallpaperPath for primary monitor (backward compatibility)
            if (string.IsNullOrEmpty(currentPath) && monitor?.IsPrimary == true)
            {
                currentPath = AppConfiguration.CurrentWallpaperPath;
            }
            
            // Get per-monitor previous wallpaper path
            var previousPath = AppConfiguration.GetMonitorPreviousWallpaperFilePath(targetMonitor);
            
            // Fallback to global PreviousWallpaperPath for primary monitor (backward compatibility)
            if (!File.Exists(previousPath) && monitor?.IsPrimary == true)
            {
                previousPath = AppConfiguration.PreviousWallpaperPath;
            }
            
            // Delete current wallpaper if it exists
            if (!string.IsNullOrEmpty(currentPath) && File.Exists(currentPath))
            {
                try
                {
                    File.Delete(currentPath);
                    
                    // Clear the per-monitor path from configuration
                    AppConfiguration.ClearMonitorWallpaperPath(targetMonitor);
                    
                    // Also clear CurrentWallpaperPath if this is the primary monitor
                    if (monitor?.IsPrimary == true && File.Exists(AppConfiguration.CurrentWallpaperPath))
                    {
                        try
                        {
                            File.Delete(AppConfiguration.CurrentWallpaperPath);
                        }
                        catch { /* ignore if deletion fails */ }
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(
                        $"Failed to delete current wallpaper: {ex.Message}",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }
            }
            else if (string.IsNullOrEmpty(currentPath))
            {
                System.Windows.MessageBox.Show(
                    $"No wallpaper found for {monitor?.Name ?? $"Monitor {targetMonitor}"}.",
                    "Info",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }
            
            // Restore previous wallpaper if available (apply to the same monitor)
            if (!string.IsNullOrEmpty(previousPath) && File.Exists(previousPath))
            {
                // Copy the previous wallpaper back to the current wallpaper path
                var restoredPath = AppConfiguration.GetMonitorWallpaperFilePath(targetMonitor);
                try
                {
                    File.Copy(previousPath, restoredPath, overwrite: true);
                    
                    // Apply the restored wallpaper to the monitor
                    if (SetWallpaper(restoredPath, targetMonitor))
                    {
                        // Update the per-monitor path configuration with the restored wallpaper
                        AppConfiguration.SetMonitorWallpaperPath(targetMonitor, restoredPath);
                        
                        // Also update CurrentWallpaperPath if this is the primary monitor
                        if (monitor?.IsPrimary == true)
                        {
                            try
                            {
                                File.Copy(restoredPath, AppConfiguration.CurrentWallpaperPath, overwrite: true);
                            }
                            catch { /* ignore if copy fails */ }
                        }
                        
                        LoadCurrentWallpaper();
                        System.Windows.MessageBox.Show(
                            $"Removed generated wallpaper and restored previous wallpaper for {monitor?.Name ?? $"Monitor {targetMonitor}"}.",
                            "Info",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                    else
                    {
                        LoadCurrentWallpaper();
                        System.Windows.MessageBox.Show(
                            $"Removed generated wallpaper, but failed to restore previous wallpaper for {monitor?.Name ?? $"Monitor {targetMonitor}"}.",
                            "Warning",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    LoadCurrentWallpaper();
                    System.Windows.MessageBox.Show(
                        $"Removed generated wallpaper, but failed to restore previous wallpaper: {ex.Message}",
                        "Warning",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
            else
            {
                // Clear preview and show placeholder
                LoadCurrentWallpaper();
                System.Windows.MessageBox.Show(
                    $"Removed generated wallpaper for {monitor?.Name ?? $"Monitor {targetMonitor}"}.",
                    "Info",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Failed to remove generated wallpaper: {ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Handles the Apply button click event.
    /// Applies wallpapers to monitors based on selection or all monitors.
    /// When a specific monitor is selected, applies only that monitor's wallpaper.
    /// When "Use Settings Default" is selected, applies each monitor's own wallpaper (per-monitor mode).
    /// </summary>
    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var selectedMonitor = GetSelectedMonitor();
            var monitors = _monitorService.GetAllMonitors();
            
            if (selectedMonitor.HasValue)
            {
                // Apply to specific monitor only - use that monitor's wallpaper
                var monitor = _monitorService.GetMonitorByIndex(selectedMonitor.Value);
                var monitorPath = AppConfiguration.GetMonitorWallpaperPath(selectedMonitor.Value);
                
                if (string.IsNullOrEmpty(monitorPath) || !File.Exists(monitorPath))
                {
                    // Fallback to current path for primary monitor
                    if (monitor?.IsPrimary == true)
                    {
                        monitorPath = AppConfiguration.CurrentWallpaperPath;
                    }
                    
                    if (string.IsNullOrEmpty(monitorPath) || !File.Exists(monitorPath))
                    {
                        System.Windows.MessageBox.Show($"No wallpaper found for {monitor?.Name ?? $"Monitor {selectedMonitor.Value}"}. Generate one first!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                
                // Apply this monitor's wallpaper ONLY to this monitor
                if (SetWallpaper(monitorPath, selectedMonitor.Value))
                {
                    System.Windows.MessageBox.Show($"Wallpaper applied to {monitor?.Name ?? $"Monitor {selectedMonitor.Value}"}!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    System.Windows.MessageBox.Show($"Failed to apply wallpaper to {monitor?.Name ?? $"Monitor {selectedMonitor.Value}"}.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                // "Use Settings Default" selected - apply to primary monitor only
                // Multi-monitor per-monitor application is temporarily disabled
                var primaryMonitor = _monitorService.GetPrimaryMonitor();
                if (primaryMonitor == null)
                {
                    System.Windows.MessageBox.Show("No primary monitor detected.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                var wallpaperPath = AppConfiguration.CurrentWallpaperPath;
                if (string.IsNullOrEmpty(wallpaperPath) || !File.Exists(wallpaperPath))
                {
                    System.Windows.MessageBox.Show("No wallpaper found. Generate one first!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                // Apply to primary monitor
                if (SetWallpaper(wallpaperPath, primaryMonitor.Index))
                {
                    System.Windows.MessageBox.Show($"Wallpaper applied to {primaryMonitor.Name}!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    System.Windows.MessageBox.Show($"Failed to apply wallpaper to {primaryMonitor.Name}.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to apply wallpaper: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Loads and displays previews for all monitors.
    /// </summary>
    private void LoadCurrentWallpaper()
    {
        try
        {
            if (MonitorPreviewsItemsControl == null || NoPreviewBorder == null || NoPreviewText == null)
                return;

            var monitors = _monitorService.GetAllMonitors();
            var previewItems = new List<MonitorPreviewItem>();

            foreach (var monitor in monitors)
            {
                var previewItem = new MonitorPreviewItem
                {
                    MonitorIndex = monitor.Index,
                    MonitorName = $"{monitor.Name}{(monitor.IsPrimary ? " (Primary)" : "")}",
                    QuoteText = "No wallpaper",
                    WallpaperPath = string.Empty
                };

                // Try to get per-monitor wallpaper path
                var monitorPath = AppConfiguration.GetMonitorWallpaperPath(monitor.Index);
                if (string.IsNullOrEmpty(monitorPath))
                {
                    // Fallback to current wallpaper path for primary monitor
                    if (monitor.IsPrimary)
                    {
                        monitorPath = AppConfiguration.CurrentWallpaperPath;
                    }
                }

                if (!string.IsNullOrEmpty(monitorPath) && File.Exists(monitorPath))
                {
                    try
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                        bitmap.UriSource = new Uri(monitorPath, UriKind.Absolute);
                        bitmap.EndInit();
                        bitmap.Freeze();

                        previewItem.PreviewImage = bitmap;
                        previewItem.WallpaperPath = monitorPath;
                        
                        // Show monitor resolution as quote text placeholder
                        // Quote text can be enhanced later to load from history metadata
                        previewItem.QuoteText = $"{monitor.Width}x{monitor.Height}";
                    }
                    catch
                    {
                        previewItem.QuoteText = "Error loading preview";
                    }
                }

                previewItems.Add(previewItem);
            }

            if (previewItems.Count > 0)
            {
                MonitorPreviewsItemsControl.ItemsSource = previewItems;
                
                // Update grid columns based on available width
                UpdateMonitorPreviewGridColumns();
                
                if (previewItems.Any(p => p.PreviewImage != null))
                {
                    NoPreviewBorder.Visibility = Visibility.Collapsed;
                }
                else
                {
                    NoPreviewBorder.Visibility = Visibility.Visible;
                }
            }
            else
            {
                MonitorPreviewsItemsControl.ItemsSource = null;
                NoPreviewBorder.Visibility = Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoadCurrentWallpaper error: {ex.Message}");
            if (NoPreviewBorder != null)
                NoPreviewBorder.Visibility = Visibility.Visible;
        }
    }

    /// <summary>
    /// Updates the monitor preview grid columns based on available width.
    /// </summary>
    private void UpdateMonitorPreviewGridColumns()
    {
        try
        {
            if (MonitorPreviewsItemsControl == null || PreviewScrollViewer == null)
                return;

            var itemsPanel = MonitorPreviewsItemsControl.ItemsPanel?.LoadContent() as UniformGrid;
            if (itemsPanel == null)
                return;

            var itemCount = MonitorPreviewsItemsControl.Items.Count;
            if (itemCount == 0)
                return;

            // Get available width (accounting for padding and margins)
            var availableWidth = PreviewScrollViewer.ActualWidth;
            if (availableWidth <= 0)
                availableWidth = PreviewScrollViewer.Width;
            if (availableWidth <= 0)
                availableWidth = 800; // Fallback

            // Calculate optimal columns based on width
            // Each preview card needs ~350-400px width (including margins)
            int columns;
            if (availableWidth >= 1200)
                columns = Math.Min(itemCount, 3); // 3 columns for wide screens
            else if (availableWidth >= 800)
                columns = Math.Min(itemCount, 2); // 2 columns for medium screens
            else
                columns = 1; // 1 column for narrow screens

            itemsPanel.Columns = columns;
            itemsPanel.Rows = 0; // Auto-calculate rows

            // Apply the updated panel
            var itemsPanelTemplate = new ItemsPanelTemplate();
            var factory = new FrameworkElementFactory(typeof(UniformGrid));
            factory.SetValue(UniformGrid.ColumnsProperty, columns);
            factory.SetValue(UniformGrid.RowsProperty, 0);
            itemsPanelTemplate.VisualTree = factory;
            MonitorPreviewsItemsControl.ItemsPanel = itemsPanelTemplate;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"UpdateMonitorPreviewGridColumns error: {ex.Message}");
        }
    }

    /// <summary>
    /// Windows API function for setting desktop wallpaper.
    /// P/Invoke declaration for SystemParametersInfo.
    /// </summary>
    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
    private static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

    /// <summary>
    /// Windows API constant: Set desktop wallpaper action.
    /// </summary>
    private const int SPI_SETDESKWALLPAPER = 20;
    
    /// <summary>
    /// Windows API constant: Update INI file flag.
    /// </summary>
    private const int SPIF_UPDATEINIFILE = 0x01;
    
    /// <summary>
    /// Windows API constant: Send change notification flag.
    /// </summary>
    private const int SPIF_SENDCHANGE = 0x02;

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
    /// Sets the desktop wallpaper using Windows API.
    /// Includes compatibility handling for different Windows versions and error scenarios.
    /// Supports multi-monitor configurations.
    /// Automatically clears animated wallpapers before applying static ones.
    /// </summary>
    /// <param name="path">Path to the wallpaper image file</param>
    /// <param name="monitorIndex">Optional monitor index. If null, uses settings default.</param>
    /// <returns>True if successful, false otherwise</returns>
    private bool SetWallpaper(string path, int? monitorIndex = null)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            System.Windows.MessageBox.Show("Wallpaper file not found or invalid path.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        try
        {
            // Clear any animated wallpapers before applying static wallpaper
            var animatedService = new AnimatedWallpaperService();
            if (animatedService.IsWallpaperEngineAvailable())
            {
                animatedService.ClearAnimatedWallpaper(monitorIndex);
                System.Threading.Thread.Sleep(300);
            }

            // If monitor index is specified, apply to that specific monitor ONLY
            if (monitorIndex.HasValue)
            {
                var monitor = _monitorService.GetMonitorByIndex(monitorIndex.Value);
                if (monitor != null)
                {
                    // Use per-monitor service to apply to this specific monitor only
                    return AnimeQuoteWall.Core.Services.WallpaperSettingHelper.SetWallpaper(path, monitorIndex.Value);
                }
                return false;
            }

            // No specific monitor specified - this should not happen in normal flow
            // But if it does, apply to primary monitor only to avoid applying to all monitors
            var primaryMonitor = _monitorService.GetPrimaryMonitor();
            if (primaryMonitor != null)
            {
                return AnimeQuoteWall.Core.Services.WallpaperSettingHelper.SetWallpaper(path, primaryMonitor.Index);
            }
            
            // Fallback: apply to primary monitor using standard API
            return AnimeQuoteWall.Core.Services.WallpaperSettingHelper.SetWallpaper(path);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Failed to set wallpaper: {ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return false;
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
    /// Shows a standard wallpaper error message.
    /// </summary>
    private void ShowWallpaperError()
    {
        System.Windows.MessageBox.Show(
            "Failed to set wallpaper. This may be due to:\n" +
            "- Group policy restrictions\n" +
            "- Insufficient permissions\n" +
            "- Windows slideshow mode enabled\n\n" +
            "Try disabling Windows slideshow mode in Settings > Personalization > Background.",
            "Warning",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }

    /// <summary>
    /// Handles zoom in button click.
    /// Zoom functionality disabled for multi-monitor preview (use scroll viewer zoom instead).
    /// </summary>
    private void ZoomInButton_Click(object sender, RoutedEventArgs e)
    {
        // Zoom controls disabled for multi-monitor preview
        // Users can use Ctrl+MouseWheel in the scroll viewer for zooming
        System.Windows.MessageBox.Show(
            "Use Ctrl + Mouse Wheel in the preview area to zoom.\n\n" +
            "Or use the scroll bars to navigate through monitor previews.",
            "Info",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    /// <summary>
    /// Handles zoom out button click.
    /// </summary>
    private void ZoomOutButton_Click(object sender, RoutedEventArgs e)
    {
        ZoomInButton_Click(sender, e);
    }

    /// <summary>
    /// Handles zoom reset button click.
    /// </summary>
    private void ZoomResetButton_Click(object sender, RoutedEventArgs e)
    {
        // Reset scroll viewer position
        if (PreviewScrollViewer != null)
        {
            PreviewScrollViewer.ScrollToHome();
        }
    }

    /// <summary>
    /// Handles mouse wheel events for zooming.
    /// Zoom is triggered when Ctrl key is held while scrolling.
    /// </summary>
    private void PreviewScrollViewer_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
    {
        // Allow normal scrolling for multi-monitor preview
        // Ctrl+Wheel zoom can be added later if needed
    }

    private void CompareButton_Click(object sender, RoutedEventArgs e)
    {
        // Compare feature temporarily simplified for multi-monitor preview
        // In future, could show side-by-side comparison of current vs previous
        System.Windows.MessageBox.Show(
            "Compare feature is available in History page.\n\n" +
            "Use the preview above to see all monitor wallpapers.",
            "Info",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private static StackPanel CreateButtonContent(string label)
    {
        var panel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
        var icon = new MahApps.Metro.IconPacks.PackIconMaterial
        {
            Kind = MahApps.Metro.IconPacks.PackIconMaterialKind.Compare,
            Width = 16,
            Height = 16,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 4, 0)
        };
        var text = new TextBlock
        {
            Text = label,
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 13
        };
        panel.Children.Add(icon);
        panel.Children.Add(text);
        return panel;
    }

    /// <summary>
    /// Loads a wallpaper preview (legacy method, now redirects to LoadCurrentWallpaper).
    /// </summary>
    private void LoadWallpaper(string path)
    {
        // Redirect to multi-monitor preview
        LoadCurrentWallpaper();
    }

    /// <summary>
    /// Handles ScrollViewer size changes to ensure previews fit properly.
    /// </summary>
    private void PreviewScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        // Update grid columns when size changes
        UpdateMonitorPreviewGridColumns();
        
        // Force layout update for monitor previews
        if (MonitorPreviewsItemsControl != null)
        {
            MonitorPreviewsItemsControl.InvalidateVisual();
        }
    }

    /// <summary>
    /// Handles monitor preview card click to select that monitor.
    /// </summary>
    private void MonitorPreview_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        try
        {
            if (sender is System.Windows.Controls.Border border && border.DataContext is MonitorPreviewItem item)
            {
                // Select this monitor in the combo box
                if (MonitorSelectionComboBox != null)
                {
                    var monitors = _monitorService.GetAllMonitors();
                    var monitor = monitors.FirstOrDefault(m => m.Index == item.MonitorIndex);
                    
                    if (monitor != null)
                    {
                        // Find and select the corresponding combo box item
                        foreach (ComboBoxItem comboItem in MonitorSelectionComboBox.Items)
                        {
                            if (comboItem.Tag is int index && index == monitor.Index)
                            {
                                MonitorSelectionComboBox.SelectedItem = comboItem;
                                break;
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MonitorPreview_MouseLeftButtonUp error: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles page size changes to adjust layout responsively.
    /// </summary>
    private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        try
        {
            // Update grid columns when page size changes
            UpdateMonitorPreviewGridColumns();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Page_SizeChanged error: {ex.Message}");
        }
    }
}

