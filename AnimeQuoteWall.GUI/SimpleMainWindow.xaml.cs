using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using AnimeQuoteWall.Core.Interfaces;
using AnimeQuoteWall.Core.Models;
using AnimeQuoteWall.Core.Services;
using Microsoft.Win32;

namespace AnimeQuoteWall.GUI;

public partial class SimpleMainWindow : Window
{
    private readonly IQuoteService _quoteService;
    private readonly IBackgroundService _backgroundService;
    private readonly IWallpaperService _wallpaperService;
    
    private readonly string _quotesPath;
    private readonly string _backgroundsPath;
    private readonly string _currentWallpaperPath;
    
    private List<Quote> _quotes = new();
  
    public SimpleMainWindow()
 {
     InitializeComponent();
        
        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        _quotesPath = Path.Combine(baseDirectory, "quotes.json");
  _backgroundsPath = Path.Combine(baseDirectory, "backgrounds");
        _currentWallpaperPath = Path.Combine(baseDirectory, "current.png");
        
        Directory.CreateDirectory(_backgroundsPath);
        
        _quoteService = new QuoteService();
        _backgroundService = new BackgroundService();
        _wallpaperService = new WallpaperService();
        
Loaded += async (s, e) => await InitializeAsync();
    }
    
    private async Task InitializeAsync()
{
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
            
  var backgrounds = _backgroundService.GetAllBackgroundImages(_backgroundsPath);
 
          if (_quotes.Count == 0)
     {
         MessageBox.Show("No quotes available. Please add quotes first.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
           return;
          }
       
        if (backgrounds.Count == 0)
   {
                MessageBox.Show("No background images available. Please add backgrounds first.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
      return;
            }
            
       var randomQuote = _quoteService.GetRandomQuote(_quotes);
         var randomBackground = _backgroundService.GetRandomBackgroundImage(_backgroundsPath);
      
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
 _wallpaperService.SaveImageAsync(bitmap, _currentWallpaperPath).Wait();
            });

       LoadCurrentWallpaper();
         MessageBox.Show($"Wallpaper generated!\n\nQuote: \"{randomQuote.Text}\"\nBy: {randomQuote.Character}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
         MessageBox.Show($"Failed to generate wallpaper: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            GenerateButton.IsEnabled = true;
        }
    }
    
    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
 LoadCurrentWallpaper();
        MessageBox.Show("Preview refreshed!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
    }
    
    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
try
        {
     if (!File.Exists(_currentWallpaperPath))
    {
   MessageBox.Show("No wallpaper to apply. Generate one first!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    return;
         }
          
  SetWallpaper(_currentWallpaperPath);
            MessageBox.Show("Wallpaper applied successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
 }
        catch (Exception ex)
        {
MessageBox.Show($"Failed to apply wallpaper: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private void LoadCurrentWallpaper()
    {
        if (File.Exists(_currentWallpaperPath))
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
                bitmap.UriSource = new Uri(_currentWallpaperPath, UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze(); // Freeze to release file handle
                
                WallpaperPreview.Source = bitmap;
                NoPreviewText.Visibility = Visibility.Collapsed;
                NoPreviewBorder.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                NoPreviewText.Text = $"Error loading preview: {ex.Message}";
                NoPreviewText.Visibility = Visibility.Visible;
                NoPreviewBorder.Visibility = Visibility.Visible;
            }
        }
        else
        {
            WallpaperPreview.Source = null;
            NoPreviewText.Visibility = Visibility.Visible;
            NoPreviewBorder.Visibility = Visibility.Visible;
        }
    }
    
    private async Task LoadQuotesAsync()
    {
        try
        {
      await _quoteService.EnsureQuotesFileAsync(_quotesPath);
    _quotes = await _quoteService.LoadQuotesAsync(_quotesPath);
 QuotesListBox.ItemsSource = null;
            QuotesListBox.ItemsSource = _quotes;
       QuoteCountTextBottom.Text = $"{_quotes.Count}";
        }
     catch (Exception ex)
 {
            MessageBox.Show($"Failed to load quotes: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
 await _quoteService.SaveQuotesAsync(_quotes, _quotesPath);
    await LoadQuotesAsync();
     MessageBox.Show($"Quote added: \"{newQuote.Text}\"", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        }
     catch (Exception ex)
{
         MessageBox.Show($"Failed to add quote: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
   }
    }
    
 private async void DeleteQuoteButton_Click(object sender, RoutedEventArgs e)
    {
     if (QuotesListBox.SelectedItem is Quote selectedQuote)
        {
     var result = MessageBox.Show(
                $"Delete this quote?\n\n\"{selectedQuote.Text}\"\n� {selectedQuote.Character}",
    "Confirm Delete",
   MessageBoxButton.YesNo,
          MessageBoxImage.Question);
 
            if (result == MessageBoxResult.Yes)
            {
       try
       {
           _quotes.Remove(selectedQuote);
       await _quoteService.SaveQuotesAsync(_quotes, _quotesPath);
    await LoadQuotesAsync();
   MessageBox.Show("Quote deleted.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
    }
     catch (Exception ex)
           {
         MessageBox.Show($"Failed to delete quote: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }
    }
        }
 else
        {
   MessageBox.Show("Please select a quote to delete.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
    
  private void LoadBackgrounds()
    {
      try
    {
            var backgrounds = _backgroundService.GetAllBackgroundImages(_backgroundsPath);
            BackgroundsListBox.ItemsSource = null;
      BackgroundsListBox.ItemsSource = backgrounds.Select(Path.GetFileName);
      BackgroundCountText.Text = $"{backgrounds.Count}";
   }
        catch (Exception ex)
        {
         MessageBox.Show($"Failed to load backgrounds: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
}
    }

    private void AddBackgroundButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
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
    var destPath = Path.Combine(_backgroundsPath, fileName);

          if (!File.Exists(destPath))
            {
     File.Copy(file, destPath);
         copiedCount++;
   }
     }
           catch (Exception ex)
                {
        MessageBox.Show($"Failed to copy {Path.GetFileName(file)}: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }
  
          LoadBackgrounds();
            
     if (copiedCount > 0)
       {
         MessageBox.Show($"{copiedCount} background image(s) added!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
    
    private void DeleteBackgroundButton_Click(object sender, RoutedEventArgs e)
    {
        if (BackgroundsListBox.SelectedItem is string selectedFileName)
     {
        var fullPath = Path.Combine(_backgroundsPath, selectedFileName);
     
     var result = MessageBox.Show(
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
    MessageBox.Show("Background deleted.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
          }
      catch (Exception ex)
      {
      MessageBox.Show($"Failed to delete background: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
   }
    }
        }
        else
        {
            MessageBox.Show("Please select a background to delete.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
        }
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
}

// Simple dialog for adding quotes without external dependencies
public partial class SimpleQuoteDialog : Window
{
    public string QuoteText { get; private set; } = "";
    public string CharacterName { get; private set; } = "";
    public string? AnimeName { get; private set; }
    
    private readonly TextBox _quoteTextBox;
    private readonly TextBox _characterTextBox;
    private readonly TextBox _animeTextBox;
    
    public SimpleQuoteDialog()
    {
        Title = "Add New Quote";
        Width = 550;
        Height = 480;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 247, 250));
        ResizeMode = ResizeMode.NoResize;
        WindowStyle = WindowStyle.SingleBorderWindow;
        
        var mainGrid = new Grid { Margin = new Thickness(30), Background = System.Windows.Media.Brushes.White };
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
     
        // Title
        var title = new TextBlock
        {
            Text = "Add New Quote",
            FontSize = 24,
            FontWeight = FontWeights.Bold,
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(26, 32, 44)),
            Margin = new Thickness(0, 0, 0, 25)
        };
        Grid.SetRow(title, 0);
        mainGrid.Children.Add(title);
      
        // Quote Text Label
        var quoteLabel = new TextBlock 
        { 
            Text = "Quote Text:", 
            FontSize = 14,
            FontWeight = FontWeights.Medium,
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(74, 85, 104)),
            Margin = new Thickness(0, 0, 0, 8) 
        };
        Grid.SetRow(quoteLabel, 1);
        mainGrid.Children.Add(quoteLabel);
    
        // Quote Text Box
        _quoteTextBox = new TextBox
        {
            Height = 120,
            TextWrapping = TextWrapping.Wrap,
            AcceptsReturn = true,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Margin = new Thickness(0, 0, 0, 20),
            Padding = new Thickness(12),
            FontSize = 14,
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(237, 242, 247)),
            BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(225, 232, 237)),
            BorderThickness = new Thickness(1)
        };
        Grid.SetRow(_quoteTextBox, 2);
        mainGrid.Children.Add(_quoteTextBox);
        
        // Character Label
        var characterLabel = new TextBlock 
        { 
            Text = "Character Name:", 
            FontSize = 14,
            FontWeight = FontWeights.Medium,
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(74, 85, 104)),
            Margin = new Thickness(0, 0, 0, 8) 
        };
        Grid.SetRow(characterLabel, 3);
        mainGrid.Children.Add(characterLabel);
        
        // Character Text Box
        _characterTextBox = new TextBox 
        { 
            Margin = new Thickness(0, 0, 0, 20),
            Padding = new Thickness(12),
            FontSize = 14,
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(237, 242, 247)),
            BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(225, 232, 237)),
            BorderThickness = new Thickness(1)
        };
        Grid.SetRow(_characterTextBox, 4);
        mainGrid.Children.Add(_characterTextBox);
        
        // Anime Label
        var animeLabel = new TextBlock 
        { 
            Text = "Anime Name (Optional):", 
            FontSize = 14,
            FontWeight = FontWeights.Medium,
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(74, 85, 104)),
            Margin = new Thickness(0, 0, 0, 8) 
        };
        Grid.SetRow(animeLabel, 5);
        mainGrid.Children.Add(animeLabel);
  
        // Anime Text Box
        _animeTextBox = new TextBox 
        { 
            Margin = new Thickness(0, 0, 0, 25),
            Padding = new Thickness(12),
            FontSize = 14,
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(237, 242, 247)),
            BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(225, 232, 237)),
            BorderThickness = new Thickness(1)
        };
        Grid.SetRow(_animeTextBox, 6);
        mainGrid.Children.Add(_animeTextBox);
        
        // Buttons
        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        
        var cancelButton = new Button
        {
            Content = "Cancel",
            MinWidth = 110,
            Height = 42,
            Margin = new Thickness(0, 0, 12, 0),
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(226, 232, 240)),
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(74, 85, 104)),
            BorderThickness = new Thickness(0),
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            Cursor = System.Windows.Input.Cursors.Hand
        };
        cancelButton.Click += (s, e) => { DialogResult = false; Close(); };
        
        var okButton = new Button
        {
            Content = "Add Quote",
            MinWidth = 130,
            Height = 42,
            Background = new System.Windows.Media.LinearGradientBrush(
                System.Windows.Media.Color.FromRgb(102, 126, 234),
                System.Windows.Media.Color.FromRgb(90, 103, 216),
                new System.Windows.Point(0, 0),
                new System.Windows.Point(0, 1)),
            Foreground = System.Windows.Media.Brushes.White,
            BorderThickness = new Thickness(0),
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            Cursor = System.Windows.Input.Cursors.Hand,
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = System.Windows.Media.Colors.Black,
                BlurRadius = 10,
                ShadowDepth = 2,
                Opacity = 0.2
            }
        };
        okButton.Click += OkButton_Click;
        
        buttonPanel.Children.Add(cancelButton);
        buttonPanel.Children.Add(okButton);
        Grid.SetRow(buttonPanel, 7);
        mainGrid.Children.Add(buttonPanel);
        
        Content = mainGrid;
        
        // Set focus to quote text box
        _quoteTextBox.Focus();
    }
    
    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
    if (string.IsNullOrWhiteSpace(_quoteTextBox.Text) || string.IsNullOrWhiteSpace(_characterTextBox.Text))
    {
         MessageBox.Show("Please fill in both Quote Text and Character Name.", "Missing Information", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        QuoteText = _quoteTextBox.Text.Trim();
        CharacterName = _characterTextBox.Text.Trim();
   AnimeName = string.IsNullOrWhiteSpace(_animeTextBox.Text) ? null : _animeTextBox.Text.Trim();
  
        DialogResult = true;
   Close();
    }
}