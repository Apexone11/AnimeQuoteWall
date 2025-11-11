using System;
using System.Windows;
using System.Windows.Controls;
using AnimeQuoteWall.Core.Configuration;
using AnimeQuoteWall.GUI.Pages;

namespace AnimeQuoteWall.GUI;

public partial class SimpleMainWindow : Window
{
    private WallpaperPage? _wallpaperPage;
    private QuotesPage? _quotesPage;
    private BackgroundsPage? _backgroundsPage;
    private AnimationPage? _animationPage;
    private AnimatedWallpapersPage? _animatedWallpapersPage;
    private HistoryPage? _historyPage;
    private PlaylistsPage? _playlistsPage;
    private SettingsPage? _settingsPage;
    private System.Windows.Controls.Button? _currentNavButton;
    private string? _currentPageName;

    public SimpleMainWindow()
    {
        InitializeComponent();
        // Defer heavy initialization until after window is shown
        Loaded += (s, e) => InitializeAsync();
    }

    private async void InitializeAsync()
    {
        try
        {
            // Ensure directories exist in background (non-blocking)
            _ = System.Threading.Tasks.Task.Run(() => AppConfiguration.EnsureDirectories());
            
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
                Dispatcher.BeginInvoke(new Action(() =>
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
                        "NavAnimationButton" => "Animation",
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
        catch { /* ignore */ }
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
                "Animation" => NavAnimationButton,
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
            if (NavAnimationButton.Tag?.ToString() == "Selected") NavAnimationButton.Tag = "Animation";
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
                    "Animation" => new AnimationPage(),
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
                        case "Animation": _animationPage = (AnimationPage)page; break;
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
                        "Animation" => "Animated Wallpaper Generator",
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

// Simple dialog for adding quotes without external dependencies
public partial class SimpleQuoteDialog : Window
{
    public string QuoteText { get; private set; } = "";
    public string CharacterName { get; private set; } = "";
    public string? AnimeName { get; private set; }

    private readonly System.Windows.Controls.TextBox _quoteTextBox;
    private readonly System.Windows.Controls.TextBox _characterTextBox;
    private readonly System.Windows.Controls.TextBox _animeTextBox;

    public SimpleQuoteDialog()
    {
        Title = "Add New Quote";
        Width = 560;
        Height = 480;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 247, 250));
        ResizeMode = ResizeMode.NoResize;

        var mainGrid = new Grid { Margin = new Thickness(0) };
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Modern Header
        var headerBorder = new Border
        {
            Background = new System.Windows.Media.LinearGradientBrush(
                System.Windows.Media.Color.FromRgb(129, 140, 248),
                System.Windows.Media.Color.FromRgb(99, 102, 241),
                new System.Windows.Point(0, 0),
                new System.Windows.Point(1, 1)),
            Padding = new Thickness(24),
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = System.Windows.Media.Colors.DarkBlue,
                Opacity = 0.3,
                BlurRadius = 20,
                ShadowDepth = 4
            }
        };
        var headerStack = new StackPanel();
        var headerTitle = new TextBlock
        {
            Text = "✨ Add New Quote",
            FontSize = 22,
            FontWeight = FontWeights.Bold,
            Foreground = System.Windows.Media.Brushes.White
        };
        var headerSubtitle = new TextBlock
        {
            Text = "Fill in the details below to add a new quote",
            FontSize = 13,
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(224, 231, 255)),
            Margin = new Thickness(0, 6, 0, 0)
        };
        headerStack.Children.Add(headerTitle);
        headerStack.Children.Add(headerSubtitle);
        headerBorder.Child = headerStack;
        Grid.SetRow(headerBorder, 0);
        mainGrid.Children.Add(headerBorder);

        // Content Area
        var contentBorder = new Border
        {
            Background = System.Windows.Media.Brushes.White,
            Padding = new Thickness(28),
            Margin = new Thickness(0),
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = System.Windows.Media.Colors.Black,
                Opacity = 0.08,
                BlurRadius = 10,
                ShadowDepth = 2
            }
        };
        var contentGrid = new Grid { Margin = new Thickness(0) };
        contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Quote Text Label
        var quoteLabel = new TextBlock
        {
            Text = "Quote Text",
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 41, 59)),
            Margin = new Thickness(0, 0, 0, 8)
        };
        Grid.SetRow(quoteLabel, 0);
        contentGrid.Children.Add(quoteLabel);

        // Quote Text Box
        _quoteTextBox = new System.Windows.Controls.TextBox
        {
            Height = 110,
            TextWrapping = TextWrapping.Wrap,
            AcceptsReturn = true,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Margin = new Thickness(0, 0, 0, 20),
            Padding = new Thickness(12),
            FontSize = 14,
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(248, 250, 252)),
            BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(226, 232, 240)),
            BorderThickness = new Thickness(1)
        };
        Grid.SetRow(_quoteTextBox, 1);
        contentGrid.Children.Add(_quoteTextBox);

        // Character Label
        var characterLabel = new TextBlock
        {
            Text = "Character Name",
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 41, 59)),
            Margin = new Thickness(0, 0, 0, 8)
        };
        Grid.SetRow(characterLabel, 2);
        contentGrid.Children.Add(characterLabel);

        // Character Text Box
        _characterTextBox = new System.Windows.Controls.TextBox
        {
            Margin = new Thickness(0, 0, 0, 20),
            Padding = new Thickness(12),
            FontSize = 14,
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(248, 250, 252)),
            BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(226, 232, 240)),
            BorderThickness = new Thickness(1)
        };
        Grid.SetRow(_characterTextBox, 3);
        contentGrid.Children.Add(_characterTextBox);

        // Anime Label
        var animeLabel = new TextBlock
        {
            Text = "Anime Name (Optional)",
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 41, 59)),
            Margin = new Thickness(0, 0, 0, 8)
        };
        Grid.SetRow(animeLabel, 4);
        contentGrid.Children.Add(animeLabel);

        // Anime Text Box
        _animeTextBox = new System.Windows.Controls.TextBox
        {
            Margin = new Thickness(0, 0, 0, 24),
            Padding = new Thickness(12),
            FontSize = 14,
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(248, 250, 252)),
            BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(226, 232, 240)),
            BorderThickness = new Thickness(1)
        };
        Grid.SetRow(_animeTextBox, 5);
        contentGrid.Children.Add(_animeTextBox);

        // Buttons
        var buttonPanel = new StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
            Margin = new Thickness(0, 8, 0, 0)
        };

        var cancelButton = new System.Windows.Controls.Button
        {
            Content = "Cancel",
            MinWidth = 100,
            Height = 40,
            Margin = new Thickness(0, 0, 12, 0),
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(241, 245, 249)),
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(71, 85, 105)),
            BorderThickness = new Thickness(0),
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            Cursor = System.Windows.Input.Cursors.Hand
        };
        cancelButton.Click += (s, e) => { DialogResult = false; Close(); };

        var okButton = new System.Windows.Controls.Button
        {
            Content = "➕ Add Quote",
            MinWidth = 130,
            Height = 40,
            Background = new System.Windows.Media.LinearGradientBrush(
                System.Windows.Media.Color.FromRgb(129, 140, 248),
                System.Windows.Media.Color.FromRgb(99, 102, 241),
                new System.Windows.Point(0, 0),
                new System.Windows.Point(0, 1)),
            Foreground = System.Windows.Media.Brushes.White,
            BorderThickness = new Thickness(0),
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            Cursor = System.Windows.Input.Cursors.Hand
        };
        okButton.Click += OkButton_Click;

        buttonPanel.Children.Add(cancelButton);
        buttonPanel.Children.Add(okButton);
        contentGrid.Children.Add(buttonPanel);
        Grid.SetRow(buttonPanel, 6);
        contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        contentBorder.Child = contentGrid;
        Grid.SetRow(contentBorder, 1);
        mainGrid.Children.Add(contentBorder);

        Content = mainGrid;

        _quoteTextBox.Focus();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_quoteTextBox.Text) || string.IsNullOrWhiteSpace(_characterTextBox.Text))
        {
            System.Windows.MessageBox.Show("Please fill in both Quote Text and Character Name.", "Missing Information", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        QuoteText = _quoteTextBox.Text.Trim();
        CharacterName = _characterTextBox.Text.Trim();
        AnimeName = string.IsNullOrWhiteSpace(_animeTextBox.Text) ? null : _animeTextBox.Text.Trim();

        DialogResult = true;
        Close();
    }
}
