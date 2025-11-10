using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using AnimeQuoteWall.Core.Configuration;
using AnimeQuoteWall.Core.Interfaces;
using AnimeQuoteWall.Core.Models;
using AnimeQuoteWall.Core.Services;
using Microsoft.Win32;
using Forms = System.Windows.Forms;

namespace AnimeQuoteWall.GUI;

public partial class SimpleMainWindow : Window
{
    private readonly IQuoteService _quoteService;
    private readonly IBackgroundService _backgroundService;
    private readonly IWallpaperService _wallpaperService;

    private List<Quote> _quotes = new();

    public SimpleMainWindow()
    {
        InitializeComponent();

        _quoteService = new QuoteService();
        _backgroundService = new BackgroundService();
        _wallpaperService = new WallpaperService();

        Loaded += async (s, e) => await InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            AppConfiguration.EnsureDirectories();
            UpdatePathsUI();
            UpdateThemeCombo();
            UpdateDefaultsInfo();
        }
        catch { /* ignore UI init errors */ }

        await LoadQuotesAsync();
        LoadBackgrounds();
        LoadCurrentWallpaper();
    }

    #region Window Control Handlers

    private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            MaximizeButton_Click(sender, e);
        }
        else
        {
            DragMove();
        }
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    #endregion

    private async void GenerateButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            GenerateButton.IsEnabled = false;

            var backgrounds = _backgroundService.GetAllBackgroundImages(AppConfiguration.BackgroundsDirectory);

            if (_quotes.Count == 0)
            {
                System.Windows.MessageBox.Show("No quotes available. Please add quotes first.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (backgrounds.Count == 0)
            {
                System.Windows.MessageBox.Show("No background images available. Please add backgrounds first.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var randomQuote = _quoteService.GetRandomQuote(_quotes);
            var randomBackground = _backgroundService.GetRandomBackgroundImage(AppConfiguration.BackgroundsDirectory);

            var screenWidth = (int)SystemParameters.PrimaryScreenWidth;
            var screenHeight = (int)SystemParameters.PrimaryScreenHeight;

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

            await Task.Run(() =>
             {
                 using var bitmap = _wallpaperService.CreateWallpaperImage(randomBackground, randomQuote, settings);
                 _wallpaperService.SaveImageAsync(bitmap, AppConfiguration.CurrentWallpaperPath).Wait();
             });

            LoadCurrentWallpaper();
            System.Windows.MessageBox.Show($"Wallpaper generated!\n\nQuote: \"{randomQuote.Text}\"\nBy: {randomQuote.Character}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to generate wallpaper: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            GenerateButton.IsEnabled = true;
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
        var path = AppConfiguration.CurrentWallpaperPath;
        if (File.Exists(path))
        {
            try
            {
                // Force reload by clearing cache first
                WallpaperPreview.Source = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                bitmap.UriSource = new Uri(path, UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze(); // Freeze to release file handle

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

    private async Task LoadQuotesAsync()
    {
        try
        {
            await _quoteService.EnsureQuotesFileAsync(AppConfiguration.QuotesFilePath);
            _quotes = await _quoteService.LoadQuotesAsync(AppConfiguration.QuotesFilePath);
            QuotesListBox.ItemsSource = null;
            QuotesListBox.ItemsSource = _quotes;

            // Update quote count
            var quoteCountRun = (Run)QuoteCountTextBottom.Inlines.FirstOrDefault(i => i is Run r && r.Name == "QuoteCountRun");
            if (quoteCountRun != null)
            {
                quoteCountRun.Text = $"{_quotes.Count}";
            }
            else
            {
                QuoteCountTextBottom.Text = $"Total Quotes: {_quotes.Count}";
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to load quotes: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void AddQuoteButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new SimpleQuoteDialog();
            if (dialog.ShowDialog() == true)
            {
                var newQuote = new Quote
                {
                    Text = dialog.QuoteText,
                    Character = dialog.CharacterName,
                    Anime = dialog.AnimeName ?? "Unknown"
                };

                _quotes.Add(newQuote);
                await _quoteService.SaveQuotesAsync(_quotes, AppConfiguration.QuotesFilePath);
                await LoadQuotesAsync();
                System.Windows.MessageBox.Show($"Quote added: \"{newQuote.Text}\"", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to add quote: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void DeleteQuoteButton_Click(object sender, RoutedEventArgs e)
    {
        if (QuotesListBox.SelectedItem is Quote selectedQuote)
        {
            var result = System.Windows.MessageBox.Show(
                       $"Delete this quote?\n\n\"{selectedQuote.Text}\"\n� {selectedQuote.Character}",
           "Confirm Delete",
          MessageBoxButton.YesNo,
                 MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _quotes.Remove(selectedQuote);
                    await _quoteService.SaveQuotesAsync(_quotes, AppConfiguration.QuotesFilePath);
                    await LoadQuotesAsync();
                    System.Windows.MessageBox.Show("Quote deleted.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Failed to delete quote: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        else
        {
            System.Windows.MessageBox.Show("Please select a quote to delete.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void LoadBackgrounds()
    {
        try
        {
            var backgrounds = _backgroundService.GetAllBackgroundImages(AppConfiguration.BackgroundsDirectory);
            BackgroundsListBox.ItemsSource = null;
            BackgroundsListBox.ItemsSource = backgrounds.Select(Path.GetFileName);

            // Update background count
            var backgroundCountRun = (Run)BackgroundCountText.Inlines.FirstOrDefault(i => i is Run r && r.Name == "BackgroundCountRun");
            if (backgroundCountRun != null)
            {
                backgroundCountRun.Text = $"{backgrounds.Count}";
            }
            else
            {
                BackgroundCountText.Text = $"Total Backgrounds: {backgrounds.Count}";
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to load backgrounds: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void AddBackgroundButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select Background Images",
            Filter = "Image Files (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp",
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
                    var destPath = Path.Combine(AppConfiguration.BackgroundsDirectory, fileName);

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

            LoadBackgrounds();

            if (copiedCount > 0)
            {
                System.Windows.MessageBox.Show($"{copiedCount} background image(s) added!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }

    private void DeleteBackgroundButton_Click(object sender, RoutedEventArgs e)
    {
        if (BackgroundsListBox.SelectedItem is string selectedFileName)
        {
            var fullPath = Path.Combine(AppConfiguration.BackgroundsDirectory, selectedFileName);

            var result = System.Windows.MessageBox.Show(
         $"Delete this background?\n\n{selectedFileName}",
                       "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    File.Delete(fullPath);
                    LoadBackgrounds();
                    System.Windows.MessageBox.Show("Background deleted.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Failed to delete background: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        else
        {
            System.Windows.MessageBox.Show("Please select a background to delete.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void UpdatePathsUI()
    {
        try
        {
            BackgroundsPathTextBox.Text = AppConfiguration.BackgroundsDirectory;
            QuotesPathTextBox.Text = AppConfiguration.QuotesFilePath;
            OutputPathTextBox.Text = AppConfiguration.CurrentWallpaperPath;
        }
        catch { /* ignore */ }
    }

    private void UpdateDefaultsInfo()
    {
        try
        {
            var baseDir = AppConfiguration.DefaultBaseDirectory;
            DefaultPathsInfo.Text =
                $"Base: {baseDir}\n" +
                $"Backgrounds: {System.IO.Path.Combine(baseDir, "backgrounds")}\n" +
                $"Quotes: {System.IO.Path.Combine(baseDir, "quotes.json")}\n" +
                $"Output: {System.IO.Path.Combine(baseDir, "current.png")}";
        }
        catch { /* ignore */ }
    }

    private void UpdateThemeCombo()
    {
        try
        {
            var mode = AppConfiguration.ThemeMode;
            // 0: System Default, 1: Light, 2: Dark
            var index = mode.Equals("Light", StringComparison.OrdinalIgnoreCase) ? 1 :
                        mode.Equals("Dark", StringComparison.OrdinalIgnoreCase) ? 2 : 0;
            ThemeModeComboBox.SelectedIndex = index;
        }
        catch { /* ignore */ }
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

    private const int SPI_SETDESKWALLPAPER = 20;
    private const int SPIF_UPDATEINIFILE = 0x01;
    private const int SPIF_SENDCHANGE = 0x02;

    private void SetWallpaper(string path)
    {
        SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, path, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
    }

    private void BrowseBackgroundsButton_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new Forms.FolderBrowserDialog
        {
            Description = "Select backgrounds folder"
        };
        if (dialog.ShowDialog() == Forms.DialogResult.OK)
        {
            try
            {
                AppConfiguration.SetCustomBackgroundsPath(dialog.SelectedPath);
                UpdatePathsUI();
                LoadBackgrounds();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Invalid backgrounds path: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void BrowseQuotesButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select quotes JSON file",
            Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
            CheckFileExists = false
        };
        if (dialog.ShowDialog() == true)
        {
            try
            {
                AppConfiguration.SetCustomQuotesPath(dialog.FileName);
                UpdatePathsUI();
                _ = LoadQuotesAsync();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Invalid quotes file path: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void BrowseOutputButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Select output wallpaper path",
            Filter = "PNG Image (*.png)|*.png|All Files (*.*)|*.*",
            FileName = "current.png"
        };
        if (dialog.ShowDialog() == true)
        {
            try
            {
                AppConfiguration.SetCustomOutputPath(dialog.FileName);
                UpdatePathsUI();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Invalid output path: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void ResetDefaultsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            AppConfiguration.ResetToDefaults();
            AppConfiguration.SetCustomOutputPath(null);
            UpdatePathsUI();
            _ = LoadQuotesAsync();
            LoadBackgrounds();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to reset paths: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ThemeModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            var index = ThemeModeComboBox.SelectedIndex;
            var mode = index == 1 ? "Light" : index == 2 ? "Dark" : "System";
            AppConfiguration.ThemeMode = mode;
            ThemeManager.ApplyTheme();
        }
        catch { /* ignore */ }
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
        Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 247, 250));
        ResizeMode = ResizeMode.NoResize;

        var mainGrid = new Grid { Margin = new Thickness(0) };
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Modern Header
        var headerBorder = new Border
        {
            Background = new LinearGradientBrush(
                System.Windows.Media.Color.FromRgb(129, 140, 248),
                System.Windows.Media.Color.FromRgb(99, 102, 241),
                new System.Windows.Point(0, 0),
                new System.Windows.Point(1, 1)),
            Padding = new Thickness(24),
            Effect = new DropShadowEffect
            {
                Color = Colors.DarkBlue,
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
            Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(224, 231, 255)),
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
            Effect = new DropShadowEffect
            {
                Color = Colors.Black,
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
            Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 41, 59)),
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
            Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(248, 250, 252)),
            BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(226, 232, 240)),
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
            Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 41, 59)),
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
            Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(248, 250, 252)),
            BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(226, 232, 240)),
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
            Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 41, 59)),
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
            Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(248, 250, 252)),
            BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(226, 232, 240)),
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
            Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(241, 245, 249)),
            Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(71, 85, 105)),
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
            Background = new LinearGradientBrush(
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

        // Set focus to quote text box
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