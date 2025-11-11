using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using AnimeQuoteWall.Core.Configuration;
using AnimeQuoteWall.Core.Services;
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
    /// Monitor service for detecting monitors.
    /// </summary>
    private readonly MonitorService _monitorService = new MonitorService();

    /// <summary>
    /// Initializes the settings page by loading current values.
    /// </summary>
    private void InitializeSettings()
    {
        UpdatePathsUI();
        UpdateThemeCombo();
        UpdateDefaultsInfo();
        UpdateBehaviorSettings();
        UpdateMonitorSettings();
    }

    /// <summary>
    /// Updates the behavior settings checkboxes with current configuration values.
    /// </summary>
    private void UpdateBehaviorSettings()
    {
        try
        {
            AutoRefreshPreviewCheckBox.IsChecked = AppConfiguration.AutoRefreshPreview;
            ShowNotificationsCheckBox.IsChecked = AppConfiguration.ShowGenerationNotifications;
            AutoSaveHistoryCheckBox.IsChecked = AppConfiguration.AutoSaveToHistory;
        }
        catch { /* ignore */ }
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
    /// Updates the theme setting (will apply after restart).
    /// </summary>
    private void ThemeModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            var index = ThemeModeComboBox.SelectedIndex;
            // Map combo box index to theme mode: 0=System, 1=Light, 2=Dark
            var mode = index == 1 ? "Light" : index == 2 ? "Dark" : "System";
            AppConfiguration.ThemeMode = mode;
            // Theme will apply after application restart
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

    /// <summary>
    /// Handles Auto-refresh preview checkbox checked event.
    /// </summary>
    private void AutoRefreshPreviewCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        AppConfiguration.AutoRefreshPreview = true;
    }

    /// <summary>
    /// Handles Auto-refresh preview checkbox unchecked event.
    /// </summary>
    private void AutoRefreshPreviewCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        AppConfiguration.AutoRefreshPreview = false;
    }

    /// <summary>
    /// Handles Show notifications checkbox checked event.
    /// </summary>
    private void ShowNotificationsCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        AppConfiguration.ShowGenerationNotifications = true;
    }

    /// <summary>
    /// Handles Show notifications checkbox unchecked event.
    /// </summary>
    private void ShowNotificationsCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        AppConfiguration.ShowGenerationNotifications = false;
    }

    /// <summary>
    /// Handles Auto-save history checkbox checked event.
    /// </summary>
    private void AutoSaveHistoryCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        AppConfiguration.AutoSaveToHistory = true;
    }

    /// <summary>
    /// Handles Auto-save history checkbox unchecked event.
    /// </summary>
    private void AutoSaveHistoryCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        AppConfiguration.AutoSaveToHistory = false;
    }

    /// <summary>
    /// Updates the monitor settings UI with current configuration.
    /// </summary>
    private void UpdateMonitorSettings()
    {
        try
        {
            // Update monitor mode combo box
            var mode = AppConfiguration.MultiMonitorMode;
            var modeIndex = mode switch
            {
                "All" => 1,
                "Span" => 2,
                _ => 0 // Primary
            };
            MonitorModeComboBox.SelectedIndex = modeIndex;

            // Refresh monitor list
            RefreshMonitorList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating monitor settings: {ex.Message}");
        }
    }

    /// <summary>
    /// Refreshes the monitor checkboxes list.
    /// </summary>
    private void RefreshMonitorList()
    {
        try
        {
            MonitorCheckboxesPanel.Children.Clear();

            var monitors = _monitorService.GetAllMonitors();
            var enabledIndices = AppConfiguration.EnabledMonitorIndices;

            if (monitors.Count == 0)
            {
                var noMonitorsText = new TextBlock
                {
                    Text = "No monitors detected",
                    FontSize = 12,
                    Foreground = System.Windows.Media.Brushes.Gray,
                    Margin = new Thickness(0, 4, 0, 0)
                };
                MonitorCheckboxesPanel.Children.Add(noMonitorsText);
                return;
            }

            foreach (var monitor in monitors)
            {
                var checkBox = new System.Windows.Controls.CheckBox
                {
                    Content = $"{monitor.Name} ({monitor.Width}x{monitor.Height}){(monitor.IsPrimary ? " [Primary]" : "")}",
                    FontSize = 13,
                    Margin = new Thickness(0, 0, 0, 8),
                    IsChecked = enabledIndices.Count == 0 || enabledIndices.Contains(monitor.Index),
                    Tag = monitor.Index
                };
                checkBox.SetResourceReference(FrameworkElement.StyleProperty, "ModernCheckBox");
                checkBox.Checked += MonitorCheckbox_Checked;
                checkBox.Unchecked += MonitorCheckbox_Unchecked;
                MonitorCheckboxesPanel.Children.Add(checkBox);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error refreshing monitor list: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles monitor mode combo box selection change.
    /// </summary>
    private void MonitorModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (MonitorModeComboBox.SelectedItem is ComboBoxItem item && item.Tag is string mode)
            {
                AppConfiguration.MultiMonitorMode = mode;
                
                // Update monitor selection panel visibility
                // Only show checkboxes for "All" mode (per-monitor selection)
                MonitorSelectionPanel.Visibility = mode == "All" ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error changing monitor mode: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles monitor checkbox checked event.
    /// </summary>
    private void MonitorCheckbox_Checked(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is System.Windows.Controls.CheckBox checkBox && checkBox.Tag is int index)
            {
                var enabledIndices = AppConfiguration.EnabledMonitorIndices.ToList();
                if (!enabledIndices.Contains(index))
                {
                    enabledIndices.Add(index);
                    AppConfiguration.EnabledMonitorIndices = enabledIndices;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error checking monitor: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles monitor checkbox unchecked event.
    /// </summary>
    private void MonitorCheckbox_Unchecked(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is System.Windows.Controls.CheckBox checkBox && checkBox.Tag is int index)
            {
                var enabledIndices = AppConfiguration.EnabledMonitorIndices.ToList();
                enabledIndices.Remove(index);
                AppConfiguration.EnabledMonitorIndices = enabledIndices;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error unchecking monitor: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles refresh monitors button click.
    /// </summary>
    private void RefreshMonitorsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            RefreshMonitorList();
            System.Windows.MessageBox.Show($"Refreshed monitor list. Found {_monitorService.GetMonitorCount()} monitor(s).", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to refresh monitors: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

