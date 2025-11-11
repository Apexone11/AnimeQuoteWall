using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using AnimeQuoteWall.Core.Configuration;
using AnimeQuoteWall.Core.Interfaces;
using AnimeQuoteWall.Core.Models;
using AnimeQuoteWall.Core.Services;

namespace AnimeQuoteWall.GUI.Pages;

/// <summary>
/// Page for generating, previewing, and applying wallpapers.
/// 
/// Features:
/// - Generate new wallpapers with random quotes and backgrounds
/// - Preview wallpapers with zoom and pan controls
/// - Compare current wallpaper with previous version
/// - Apply wallpapers to desktop
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

            // Select random quote and background for variety
            var randomQuote = _quoteService.GetRandomQuote(_quotes);
            var randomBackground = _backgroundService.GetRandomBackgroundImage(AppConfiguration.BackgroundsDirectory);

            // Use compatibility helper for screen resolution (works across Windows versions)
            var (screenWidth, screenHeight) = AnimeQuoteWall.Core.Services.WindowsCompatibilityHelper.GetPrimaryScreenResolution();

            var settings = new WallpaperSettings
            {
                Width = screenWidth,
                Height = screenHeight,
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

            // Save current wallpaper as previous before generating new one
            var currentPath = AppConfiguration.CurrentWallpaperPath;
            var previousPath = AppConfiguration.PreviousWallpaperPath;
            if (File.Exists(currentPath))
            {
                try
                {
                    File.Copy(currentPath, previousPath, overwrite: true);
                }
                catch { /* ignore if copy fails */ }
            }

            await Task.Run(() =>
            {
                using var bitmap = _wallpaperService.CreateWallpaperImage(randomBackground, randomQuote, settings);
                _wallpaperService.SaveImageAsync(bitmap, AppConfiguration.CurrentWallpaperPath).Wait();
            }).ConfigureAwait(false);

            // Save to history
            try
            {
                await _historyService.SaveToHistoryAsync(AppConfiguration.CurrentWallpaperPath, randomQuote, randomBackground, settings).ConfigureAwait(false);
            }
            catch { /* ignore history save errors */ }

            // Update UI on UI thread
            Dispatcher.Invoke(() =>
            {
                LoadCurrentWallpaper();
                GenerateButton.IsEnabled = true;
                System.Windows.MessageBox.Show($"Wallpaper generated!\n\nQuote: \"{randomQuote.Text}\"\nBy: {randomQuote.Character}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
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

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        LoadCurrentWallpaper();
            System.Windows.MessageBox.Show("Preview refreshed!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!File.Exists(AppConfiguration.CurrentWallpaperPath))
            {
                System.Windows.MessageBox.Show("No wallpaper to apply. Generate one first!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            SetWallpaper(AppConfiguration.CurrentWallpaperPath);
            System.Windows.MessageBox.Show("Wallpaper applied successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to apply wallpaper: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoadCurrentWallpaper()
    {
        try
        {
            if (WallpaperPreview == null || NoPreviewBorder == null || NoPreviewText == null)
                return;

            var path = AppConfiguration.CurrentWallpaperPath;
            if (File.Exists(path))
            {
                try
                {
                    WallpaperPreview.Source = null;
                    GC.Collect();
                    GC.WaitForPendingFinalizers();

                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                    bitmap.UriSource = new Uri(path, UriKind.Absolute);
                    bitmap.EndInit();
                    bitmap.Freeze();

                    WallpaperPreview.Source = bitmap;
                    NoPreviewBorder.Visibility = Visibility.Collapsed;
                }
                catch (Exception ex)
                {
                    NoPreviewText.Text = $"Error loading preview: {ex.Message}";
                    NoPreviewBorder.Visibility = Visibility.Visible;
                }
            }
            else
            {
                WallpaperPreview.Source = null;
                NoPreviewBorder.Visibility = Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            // Silently fail if controls aren't ready
            System.Diagnostics.Debug.WriteLine($"LoadCurrentWallpaper error: {ex.Message}");
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
    /// Sets the desktop wallpaper using Windows API.
    /// Includes compatibility handling for different Windows versions and error scenarios.
    /// </summary>
    /// <param name="path">Path to the wallpaper image file</param>
    private void SetWallpaper(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            System.Windows.MessageBox.Show("Wallpaper file not found or invalid path.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            // Use the compatibility helper for better error handling
            var success = AnimeQuoteWall.Core.Services.WallpaperSettingHelper.SetWallpaper(path);
            
            if (!success)
            {
                // Failed to set wallpaper - could be permissions, group policy, etc.
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
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Failed to set wallpaper: {ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Handles zoom in button click.
    /// Increases zoom level by 20% (up to maximum 500%).
    /// </summary>
    private void ZoomInButton_Click(object sender, RoutedEventArgs e)
    {
        if (PreviewScrollViewer == null || ZoomLevelText == null) return;
        _currentZoom = Math.Min(_currentZoom * 1.2, 5.0); // Max 500%
        UpdateZoom();
    }

    /// <summary>
    /// Handles zoom out button click.
    /// Decreases zoom level by 20% (down to minimum 10%).
    /// </summary>
    private void ZoomOutButton_Click(object sender, RoutedEventArgs e)
    {
        if (PreviewScrollViewer == null || ZoomLevelText == null) return;
        _currentZoom = Math.Max(_currentZoom / 1.2, 0.1); // Min 10%
        UpdateZoom();
    }

    /// <summary>
    /// Handles zoom reset button click.
    /// Resets zoom level to 100%.
    /// </summary>
    private void ZoomResetButton_Click(object sender, RoutedEventArgs e)
    {
        if (PreviewScrollViewer == null || ZoomLevelText == null) return;
        _currentZoom = 1.0; // 100%
        UpdateZoom();
    }

    /// <summary>
    /// Updates the zoom level by applying scale transform and updating zoom indicator.
    /// </summary>
    private void UpdateZoom()
    {
        if (PreviewScrollViewer == null || PreviewScaleTransform == null || ZoomLevelText == null) return;
        
        // Apply zoom scale to image transform
        PreviewScaleTransform.ScaleX = _currentZoom;
        PreviewScaleTransform.ScaleY = _currentZoom;
        
        // Update zoom level text (e.g., "100%", "150%")
        ZoomLevelText.Text = $"{(_currentZoom * 100):F0}%";
    }

    /// <summary>
    /// Handles mouse wheel events for zooming.
    /// Zoom is triggered when Ctrl key is held while scrolling.
    /// </summary>
    private void PreviewScrollViewer_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
    {
        if (PreviewScrollViewer == null || ZoomLevelText == null) return;
        
        // Only zoom if Ctrl key is pressed (prevents accidental zooming)
        if (System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.Control)
        {
            e.Handled = true; // Prevent default scrolling
            var delta = e.Delta > 0 ? 1.1 : 0.9; // Zoom in or out based on scroll direction
            _currentZoom = Math.Max(0.1, Math.Min(5.0, _currentZoom * delta)); // Clamp between 10% and 500%
            UpdateZoom();
        }
    }

    private void CompareButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_isShowingPrevious)
            {
                // Switch back to current
                LoadCurrentWallpaper();
                _isShowingPrevious = false;
                if (CompareButton != null)
                    CompareButton.Content = "🔄 Compare";
            }
            else
            {
                // Store current path as previous and load previous
                var currentPath = AppConfiguration.CurrentWallpaperPath;
                if (File.Exists(currentPath))
                {
                    _previousWallpaperPath = currentPath;
                }

                // Try to load previous wallpaper
                var previousPath = AppConfiguration.PreviousWallpaperPath;
                if (File.Exists(previousPath))
                {
                    LoadWallpaper(previousPath);
                    _isShowingPrevious = true;
                    if (CompareButton != null)
                        CompareButton.Content = "🔄 Show Current";
                }
                else
                {
                    System.Windows.MessageBox.Show("No previous wallpaper found.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to compare: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoadWallpaper(string path)
    {
        try
        {
            if (WallpaperPreview == null || NoPreviewBorder == null || NoPreviewText == null)
                return;

            if (File.Exists(path))
            {
                try
                {
                    WallpaperPreview.Source = null;
                    GC.Collect();
                    GC.WaitForPendingFinalizers();

                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                    bitmap.UriSource = new Uri(path, UriKind.Absolute);
                    bitmap.EndInit();
                    bitmap.Freeze();

                    WallpaperPreview.Source = bitmap;
                    NoPreviewBorder.Visibility = Visibility.Collapsed;
                }
                catch (Exception ex)
                {
                    NoPreviewText.Text = $"Error loading preview: {ex.Message}";
                    NoPreviewBorder.Visibility = Visibility.Visible;
                }
            }
            else
            {
                WallpaperPreview.Source = null;
                NoPreviewBorder.Visibility = Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoadWallpaper error: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles ScrollViewer size changes to ensure image fits properly.
    /// </summary>
    private void PreviewScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        // Ensure image resizes to fit available space
        if (WallpaperPreview != null && WallpaperPreview.Source != null)
        {
            // Force layout update
            WallpaperPreview.InvalidateVisual();
        }
    }
}

