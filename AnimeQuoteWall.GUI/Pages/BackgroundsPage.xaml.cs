using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media.Imaging;
using AnimeQuoteWall.Core.Configuration;
using AnimeQuoteWall.Core.Interfaces;
using AnimeQuoteWall.Core.Services;
using Microsoft.Win32;

namespace AnimeQuoteWall.GUI.Pages;

/// <summary>
/// Model for displaying background image information with thumbnail.
/// </summary>
public class BackgroundImageItem
{
    public string FileName { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;
    public string FileSize { get; set; } = string.Empty;
}

/// <summary>
/// Page for managing background images.
/// 
/// Features:
/// - Display list of available background images
/// - Add new background images (copy to backgrounds directory)
/// - Delete background images
/// - View background count
/// </summary>
public partial class BackgroundsPage : Page
{
    /// <summary>
    /// Service for managing background images.
    /// </summary>
    private readonly IBackgroundService _backgroundService;
    
    /// <summary>
    /// Cancellation token source for async operations.
    /// </summary>
    private CancellationTokenSource? _cancellationTokenSource;

    /// <summary>
    /// Initializes a new instance of the BackgroundsPage.
    /// </summary>
    public BackgroundsPage()
    {
        InitializeComponent();
        _backgroundService = new BackgroundService();
        Loaded += async (s, e) => await LoadBackgroundsAsync();
        Unloaded += (s, e) => _cancellationTokenSource?.Cancel();
    }

    /// <summary>
    /// Loads all background images from the backgrounds directory and displays them asynchronously.
    /// Updates the background count display.
    /// </summary>
    private async Task LoadBackgroundsAsync()
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
                if (EmptyStateBorder != null)
                    EmptyStateBorder.Visibility = Visibility.Collapsed;
                if (BackgroundsListBox != null)
                    BackgroundsListBox.ItemsSource = null;
                UpdateCount(0);
            });

            // Load file list on background thread
            List<string> backgrounds;
            try
            {
                backgrounds = await Task.Run(() =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return _backgroundService.GetAllBackgroundImages(AppConfiguration.BackgroundsDirectory);
                }, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return; // User navigated away or cancelled
            }

            if (cancellationToken.IsCancellationRequested)
                return;

            // Create items with image paths and file info (on background thread for large lists)
            List<BackgroundImageItem> items;
            try
            {
                items = await Task.Run(() =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return backgrounds.Select(path =>
                    {
                        try
                        {
                            if (!File.Exists(path))
                                return null;

                            var fileInfo = new FileInfo(path);
                            var sizeInMB = fileInfo.Length / (1024.0 * 1024.0);
                            return new BackgroundImageItem
                            {
                                FileName = Path.GetFileName(path),
                                ImagePath = path,
                                FileSize = sizeInMB < 1 
                                    ? $"{(fileInfo.Length / 1024.0):F1} KB" 
                                    : $"{sizeInMB:F2} MB"
                            };
                        }
                        catch
                        {
                            // Skip files that can't be accessed
                            return null;
                        }
                    }).Where(item => item != null).Cast<BackgroundImageItem>().ToList();
                }, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (cancellationToken.IsCancellationRequested)
                return;

            // Update UI on UI thread
            await Dispatcher.InvokeAsync(() =>
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                if (LoadingIndicator != null)
                    LoadingIndicator.Visibility = Visibility.Collapsed;

                if (items.Count == 0)
                {
                    if (EmptyStateBorder != null)
                        EmptyStateBorder.Visibility = Visibility.Visible;
                    if (BackgroundsListBox != null)
                        BackgroundsListBox.ItemsSource = null;
                }
                else
                {
                    if (EmptyStateBorder != null)
                        EmptyStateBorder.Visibility = Visibility.Collapsed;
                    if (BackgroundsListBox != null)
                        BackgroundsListBox.ItemsSource = items;
                }

                UpdateCount(items.Count);
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
                if (EmptyStateBorder != null)
                    EmptyStateBorder.Visibility = Visibility.Visible;
                
                System.Windows.MessageBox.Show(
                    $"Failed to load backgrounds: {ex.Message}\n\nPlease check that the backgrounds folder exists and is accessible.",
                    "Error Loading Backgrounds",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            });
        }
    }

    /// <summary>
    /// Updates the background count display.
    /// </summary>
    private void UpdateCount(int count)
    {
        try
        {
            var backgroundCountRun = (Run)BackgroundCountText.Inlines.FirstOrDefault(i => i is Run r && r.Name == "BackgroundCountRun");
            if (backgroundCountRun != null)
            {
                backgroundCountRun.Text = $"{count}";
            }
            else
            {
                BackgroundCountText.Text = $"Total Backgrounds: {count}";
            }
        }
        catch { /* ignore */ }
    }

    /// <summary>
    /// Handles the Add Background button click event.
    /// Opens a file dialog to select images and copies them to the backgrounds directory.
    /// </summary>
    private void AddBackgroundButton_Click(object sender, RoutedEventArgs e)
    {
        // Open file dialog for selecting images
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select Background Images",
            Filter = "Image Files (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp",
            Multiselect = true // Allow selecting multiple files
        };

        if (dialog.ShowDialog() == true)
        {
            int copiedCount = 0;

            // Copy each selected file to backgrounds directory
            foreach (var file in dialog.FileNames)
            {
                try
                {
                    var fileName = Path.GetFileName(file);
                    var destPath = Path.Combine(AppConfiguration.BackgroundsDirectory, fileName);

                    // Only copy if file doesn't already exist (avoid overwriting)
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

            // Reload backgrounds to show new additions
            _ = LoadBackgroundsAsync();

            if (copiedCount > 0)
            {
                System.Windows.MessageBox.Show($"{copiedCount} background image(s) added!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }

    /// <summary>
    /// Handles the Delete Background button click event.
    /// Removes the selected background image after user confirmation.
    /// </summary>
    private void DeleteBackgroundButton_Click(object sender, RoutedEventArgs e)
    {
        if (BackgroundsListBox.SelectedItem is BackgroundImageItem selectedItem)
        {
            var fullPath = selectedItem.ImagePath;
            var selectedFileName = selectedItem.FileName;

            // Confirm deletion with user
            var result = System.Windows.MessageBox.Show(
                $"Delete this background?\n\n{selectedFileName}",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // Delete file and reload list
                    File.Delete(fullPath);
                    _ = LoadBackgroundsAsync();
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
}


