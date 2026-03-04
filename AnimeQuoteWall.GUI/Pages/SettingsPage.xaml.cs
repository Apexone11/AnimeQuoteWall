using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using AnimeQuoteWall.Core.Configuration;
using AnimeQuoteWall.GUI;
using Microsoft.Win32;
using Forms = System.Windows.Forms;

namespace AnimeQuoteWall.GUI.Pages;

/// <summary>
/// Page for managing application settings.
/// 
/// Features:
/// - Configure theme mode (System/Light/Dark)
/// - Set custom paths for backgrounds, quotes, and output
/// - Reset paths to defaults
/// - View default path information
/// </summary>
public partial class SettingsPage : Page
{
    /// <summary>
    /// Initializes a new instance of the SettingsPage.
    /// </summary>
    public SettingsPage()
    {
        InitializeComponent();
        Loaded += (s, e) => InitializeSettings();
    }

    /// <summary>
    /// Initializes the settings page by loading current values.
    /// </summary>
    private void InitializeSettings()
    {
        UpdatePathsUI();
        UpdateThemeCombo();
        UpdateDefaultsInfo();
    }

    /// <summary>
    /// Updates the path text boxes with current configuration values.
    /// </summary>
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

    /// <summary>
    /// Updates the default paths information text block.
    /// Shows users what the default paths are.
    /// </summary>
    private void UpdateDefaultsInfo()
    {
        try
        {
            var baseDir = AppConfiguration.DefaultBaseDirectory;
            DefaultPathsInfo.Text =
                $"Base: {baseDir}\n" +
                $"Backgrounds: {Path.Combine(baseDir, "backgrounds")}\n" +
                $"Quotes: {Path.Combine(baseDir, "quotes.json")}\n" +
                $"Output: {Path.Combine(baseDir, "current.png")}";
        }
        catch { /* ignore */ }
    }

    /// <summary>
    /// Updates the theme combo box to reflect the current theme mode.
    /// </summary>
    private void UpdateThemeCombo()
    {
        try
        {
            var mode = AppConfiguration.ThemeMode;
            // Map theme mode to combo box index: 0=System, 1=Light, 2=Dark
            var index = mode.Equals("Light", StringComparison.OrdinalIgnoreCase) ? 1 :
                        mode.Equals("Dark", StringComparison.OrdinalIgnoreCase) ? 2 : 0;
            ThemeModeComboBox.SelectedIndex = index;
        }
        catch { /* ignore */ }
    }

    /// <summary>
    /// Handles theme mode combo box selection change.
    /// Updates the theme and applies it immediately.
    /// </summary>
    private void ThemeModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            var index = ThemeModeComboBox.SelectedIndex;
            // Map combo box index to theme mode: 0=System, 1=Light, 2=Dark
            var mode = index == 1 ? "Light" : index == 2 ? "Dark" : "System";
            AppConfiguration.ThemeMode = mode;
            ThemeManager.ApplyTheme(); // Apply theme immediately
        }
        catch { /* ignore */ }
    }

    /// <summary>
    /// Handles the Browse Backgrounds button click event.
    /// Opens a folder browser dialog to select a custom backgrounds directory.
    /// </summary>
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
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Invalid backgrounds path: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    /// <summary>
    /// Handles the Browse Quotes button click event.
    /// Opens a file dialog to select a custom quotes JSON file.
    /// </summary>
    private void BrowseQuotesButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select quotes JSON file",
            Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
            CheckFileExists = false // Allow creating new file
        };
        if (dialog.ShowDialog() == true)
        {
            try
            {
                AppConfiguration.SetCustomQuotesPath(dialog.FileName);
                UpdatePathsUI();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Invalid quotes file path: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    /// <summary>
    /// Handles the Browse Output button click event.
    /// Opens a save file dialog to select a custom output wallpaper path.
    /// </summary>
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

    /// <summary>
    /// Handles the Reset Defaults button click event.
    /// Resets all paths to their default values.
    /// </summary>
    private void ResetDefaultsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            AppConfiguration.ResetToDefaults();
            AppConfiguration.SetCustomOutputPath(null);
            UpdatePathsUI();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to reset paths: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

