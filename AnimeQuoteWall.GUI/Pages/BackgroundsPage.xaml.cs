using System;
using System.IO;
using System.Linq;
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
    /// Initializes a new instance of the BackgroundsPage.
    /// </summary>
    public BackgroundsPage()
    {
        InitializeComponent();
        _backgroundService = new BackgroundService();
        Loaded += (s, e) => LoadBackgrounds();
    }

    /// <summary>
    /// Loads all background images from the backgrounds directory and displays them.
    /// Updates the background count display.
    /// </summary>
    private void LoadBackgrounds()
    {
        try
        {
            // Get all background images from directory
            var backgrounds = _backgroundService.GetAllBackgroundImages(AppConfiguration.BackgroundsDirectory);
            
            // Create items with image paths and file info
            var items = backgrounds.Select(path =>
            {
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
            }).ToList();
            
            // Update list box with background items
            BackgroundsListBox.ItemsSource = null;
            BackgroundsListBox.ItemsSource = items;

            // Update background count text
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
            LoadBackgrounds();

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
}

