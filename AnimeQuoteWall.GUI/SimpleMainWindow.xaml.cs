using System;
using System.Windows;
using System.Windows.Controls;
using AnimeQuoteWall.Core.Configuration;
using AnimeQuoteWall.GUI.Pages;
using MahApps.Metro.IconPacks;

namespace AnimeQuoteWall.GUI;

public partial class SimpleMainWindow : Window
{
    private WallpaperPage? _wallpaperPage;
    private QuotesPage? _quotesPage;
    private BackgroundsPage? _backgroundsPage;
    private AnimatedWallpapersPage? _animatedWallpapersPage;
    private HistoryPage? _historyPage;
    private PlaylistsPage? _playlistsPage;
    private SettingsPage? _settingsPage;
    private System.Windows.Controls.Button? _currentNavButton;
    private string? _currentPageName;
    private bool _isSidebarCollapsed;

    public SimpleMainWindow()
    {
        InitializeComponent();
        Loaded += (s, e) => InitializeAsync();
        SizeChanged += MainWindow_SizeChanged;
    }

    private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if ((System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) != System.Windows.Input.ModifierKeys.Control)
            return;

        string? target = e.Key switch
        {
            System.Windows.Input.Key.D1 => "Wallpaper",
            System.Windows.Input.Key.D2 => "AnimatedWallpapers",
            System.Windows.Input.Key.D3 => "Quotes",
            System.Windows.Input.Key.D4 => "Backgrounds",
            System.Windows.Input.Key.D5 => "History",
            System.Windows.Input.Key.D6 => "Playlists",
            System.Windows.Input.Key.D7 => "Settings",
            _ => null
        };

        if (target != null)
        {
            NavigateToPage(target);
            e.Handled = true;
        }
    }

    /// <summary>
    /// Handles window size changes to adjust sidebar width responsively (only when expanded).
    /// </summary>
    private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        // Don't override width when user has manually collapsed the sidebar
        if (_isSidebarCollapsed) return;

        try
        {
            if (SidebarColumn != null)
            {
                var windowWidth = ActualWidth;
                SidebarColumn.Width = new GridLength(windowWidth < 1000 ? 200 : 240, GridUnitType.Pixel);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MainWindow_SizeChanged error: {ex.Message}");
        }
    }

    /// <summary>
    /// Toggles the sidebar between expanded (240px with labels) and collapsed (64px icon-only) modes.
    /// </summary>
    private void SidebarCollapseButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _isSidebarCollapsed = !_isSidebarCollapsed;

            if (SidebarColumn != null)
                SidebarColumn.Width = new GridLength(_isSidebarCollapsed ? 64 : 240, GridUnitType.Pixel);

            var labelVisibility = _isSidebarCollapsed ? Visibility.Collapsed : Visibility.Visible;

            // Nav item labels
            if (NavWallpaperLabel    != null) NavWallpaperLabel.Visibility    = labelVisibility;
            if (NavAnimatedLabel     != null) NavAnimatedLabel.Visibility     = labelVisibility;
            if (NavQuotesLabel       != null) NavQuotesLabel.Visibility       = labelVisibility;
            if (NavBackgroundsLabel  != null) NavBackgroundsLabel.Visibility  = labelVisibility;
            if (NavHistoryLabel      != null) NavHistoryLabel.Visibility      = labelVisibility;
            if (NavPlaylistsLabel    != null) NavPlaylistsLabel.Visibility    = labelVisibility;
            if (NavSettingsLabel     != null) NavSettingsLabel.Visibility     = labelVisibility;

            // Group section headers
            if (NavGroupLabel1 != null) NavGroupLabel1.Visibility = labelVisibility;
            if (NavGroupLabel2 != null) NavGroupLabel2.Visibility = labelVisibility;
            if (NavGroupLabel3 != null) NavGroupLabel3.Visibility = labelVisibility;

            // Branding text and footer
            if (SidebarBrandText != null) SidebarBrandText.Visibility = labelVisibility;
            if (SidebarFooter    != null) SidebarFooter.Visibility    = labelVisibility;

            // Collapse button icon and label
            if (CollapseIcon  != null) CollapseIcon.Kind = _isSidebarCollapsed ? PackIconMaterialKind.ChevronRight : PackIconMaterialKind.ChevronLeft;
            if (CollapseLabel != null) CollapseLabel.Visibility = labelVisibility;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SidebarCollapseButton_Click error: {ex.Message}");
        }
    }

    private async void InitializeAsync()
    {
        try
        {
            // Ensure directories exist in background (non-blocking)
            _ = System.Threading.Tasks.Task.Run(() => AppConfiguration.EnsureDirectories())
                .ContinueWith(t =>
                {
                    if (t.IsFaulted && t.Exception != null)
                        System.Diagnostics.Debug.WriteLine($"EnsureDirectories failed: {t.Exception.GetBaseException().Message}");
                }, System.Threading.Tasks.TaskScheduler.Default);
            
            // Wait for Frame to be fully loaded before navigating
            if (ContentFrame != null)
            {
                ContentFrame.Loaded += async (s, e) =>
                {
                    try
                    {
                        // Small delay to ensure UI is ready
                        await System.Threading.Tasks.Task.Delay(50);
                        if (PageTitleText != null)
                        {
                            NavigateToPage("Wallpaper");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show($"Navigation Error: {ex.Message}\n\n{ex.StackTrace}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    }
                };
            }
            else
            {
                // Fallback: defer navigation
                await System.Threading.Tasks.Task.Delay(100);
                _ = Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        if (ContentFrame != null && PageTitleText != null)
                        {
                            NavigateToPage("Wallpaper");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show($"Navigation Error: {ex.Message}\n\n{ex.StackTrace}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    }
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Initialization Error: {ex.Message}\n\n{ex.StackTrace}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private void NavButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is System.Windows.Controls.Button button)
            {
                string? pageName = null;
                
                // Get page name from Tag, or determine from button name if Tag is "Selected"
                var tagValue = button.Tag?.ToString();
                if (tagValue == "Selected")
                {
                    // If button is already selected, determine page name from button name
                    pageName = button.Name switch
                    {
                        "NavWallpaperButton" => "Wallpaper",
                        "NavQuotesButton" => "Quotes",
                        "NavBackgroundsButton" => "Backgrounds",
                        "NavAnimatedWallpapersButton" => "AnimatedWallpapers",
                        "NavHistoryButton" => "History",
                        "NavPlaylistsButton" => "Playlists",
                        "NavSettingsButton" => "Settings",
                        _ => null
                    };
                }
                else
                {
                    pageName = tagValue;
                }
                
                if (!string.IsNullOrEmpty(pageName))
                {
                    NavigateToPage(pageName);
                }
                else
                {
                    System.Windows.MessageBox.Show("Could not determine page name from button.", "Navigation Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                }
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Button click error: {ex.Message}\n\n{ex.StackTrace}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private void ContentFrame_LoadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e)
    {
        // Clear navigation history after each navigation to prevent back/forward issues
        try
        {
            while (ContentFrame.CanGoBack)
            {
                ContentFrame.RemoveBackEntry();
            }
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"SimpleMainWindow.ContentFrame_LoadCompleted: {ex.Message}"); }
    }

    /// <summary>
    /// Navigates to the specified page.
    /// 
    /// Process:
    /// 1. Find the navigation button for the page
    /// 2. Update button states (clear all, highlight selected)
    /// 3. Create a new page instance (ensures fresh state)
    /// 4. Navigate Frame to the new page
    /// 5. Update page title
    /// 
    /// Creating a new page instance each time ensures:
    /// - Fresh page state on each navigation
    /// - Reliable navigation even when clicking the same page multiple times
    /// - Proper initialization of page controls
    /// 
    /// </summary>
    /// <param name="pageName">Name of the page to navigate to</param>
    private async void NavigateToPage(string pageName)
    {
        try
        {
            // Safety check: ensure UI elements are available
            if (ContentFrame == null || PageTitleText == null)
            {
                return;
            }

            // Find the navigation button for this page
            System.Windows.Controls.Button? navButton = pageName switch
            {
                "Wallpaper" => NavWallpaperButton,
                "Quotes" => NavQuotesButton,
                "Backgrounds" => NavBackgroundsButton,
                "AnimatedWallpapers" => NavAnimatedWallpapersButton,
                "History" => NavHistoryButton,
                "Playlists" => NavPlaylistsButton,
                "Settings" => NavSettingsButton,
                _ => null
            };

            if (navButton == null)
            {
                System.Windows.MessageBox.Show($"Unknown page name: {pageName}", "Navigation Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            // Clear all navigation button selected states
            // Reset buttons back to their page names if they were selected
            // This ensures the page name is always available in Tag for navigation
            if (NavWallpaperButton.Tag?.ToString() == "Selected") NavWallpaperButton.Tag = "Wallpaper";
            if (NavQuotesButton.Tag?.ToString() == "Selected") NavQuotesButton.Tag = "Quotes";
            if (NavBackgroundsButton.Tag?.ToString() == "Selected") NavBackgroundsButton.Tag = "Backgrounds";
            if (NavAnimatedWallpapersButton.Tag?.ToString() == "Selected") NavAnimatedWallpapersButton.Tag = "AnimatedWallpapers";
            if (NavHistoryButton.Tag?.ToString() == "Selected") NavHistoryButton.Tag = "History";
            if (NavPlaylistsButton.Tag?.ToString() == "Selected") NavPlaylistsButton.Tag = "Playlists";
            if (NavSettingsButton.Tag?.ToString() == "Selected") NavSettingsButton.Tag = "Settings";

            // Set the selected button's Tag to "Selected" (triggers highlighted style)
            navButton.Tag = "Selected";
            _currentNavButton = navButton;

            // Navigate to page - create new instance each time to ensure navigation works
            // This is important because WPF Frame won't navigate if the page instance is the same
            // NOTE: All WPF pages MUST be created on the UI thread (STA thread)
            Page? page = null;
            try
            {
                // Create a new page instance on UI thread (required for WPF)
                // Pages handle their own async data loading internally
                    page = pageName switch
                {
                    "Wallpaper" => new WallpaperPage(),
                    "Quotes" => new QuotesPage(),
                    "Backgrounds" => new BackgroundsPage(),
                    "AnimatedWallpapers" => new AnimatedWallpapersPage(),
                    "History" => new HistoryPage(),
                    "Playlists" => new PlaylistsPage(),
                    "Settings" => new SettingsPage(),
                    _ => null
                };
                
                // Store reference for potential future use (currently unused but kept for extensibility)
                if (page != null)
                {
                    switch (pageName)
                    {
                        case "Wallpaper": _wallpaperPage = (WallpaperPage)page; break;
                        case "Quotes": _quotesPage = (QuotesPage)page; break;
                        case "Backgrounds": _backgroundsPage = (BackgroundsPage)page; break;
                        case "AnimatedWallpapers": _animatedWallpapersPage = (AnimatedWallpapersPage)page; break;
                        case "History": _historyPage = (HistoryPage)page; break;
                        case "Playlists": _playlistsPage = (PlaylistsPage)page; break;
                        case "Settings": _settingsPage = (SettingsPage)page; break;
                    }
                }
            }
            catch (Exception pageEx)
            {
                System.Windows.MessageBox.Show($"Failed to create {pageName} page: {pageEx.Message}\n\n{pageEx.StackTrace}", "Page Creation Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            // Navigate Frame to the new page
            if (page != null)
            {
                try
                {
                    // Always navigate - creating new instance ensures navigation works
                    ContentFrame.Navigate(page);
                    // Update page title with friendly names
                    PageTitleText.Text = pageName switch
                    {
                        "Wallpaper" => "Static Wallpaper Generator",
                        "Quotes" => "Quotes",
                        "Backgrounds" => "Image Library",
                        "AnimatedWallpapers" => "Animated Wallpapers Library",
                        "History" => "History",
                        "Playlists" => "Playlists",
                        "Settings" => "Settings",
                        _ => pageName
                    };
                    _currentPageName = pageName;
                }
                catch (Exception navEx)
                {
                    System.Windows.MessageBox.Show($"Failed to navigate to {pageName}: {navEx.Message}\n\n{navEx.StackTrace}", "Navigation Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Navigation Error: {ex.Message}\n\n{ex.StackTrace}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }
}
