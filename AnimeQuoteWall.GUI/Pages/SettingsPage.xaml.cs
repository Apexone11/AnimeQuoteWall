using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using AnimeQuoteWall.Core.Configuration;
using AnimeQuoteWall.Core.Services;
using AnimeQuoteWall.GUI;
using AnimeQuoteWall.GUI.Dialogs;
using AnimeQuoteWall.GUI.Services;
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
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
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
        try
        {
            UpdatePathsUI();
            UpdateThemeCombo();
            UpdateDefaultsInfo();
            UpdateBehaviorSettings();
            UpdatePerformanceSettings();
            UpdateMonitorSettings();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error initializing settings: {ex.Message}");
            System.Windows.MessageBox.Show(
                $"Failed to initialize settings page: {ex.Message}",
                "Settings Error",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        InitializeSettings();
        try
        {
            SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"SettingsPage: {ex.Message}"); }
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        try
        {
            SystemEvents.DisplaySettingsChanged -= SystemEvents_DisplaySettingsChanged;
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"SettingsPage: {ex.Message}"); }
    }

    private void SystemEvents_DisplaySettingsChanged(object? sender, EventArgs e)
    {
        try
        {
            Dispatcher?.Invoke(() =>
            {
                RefreshMonitorList();
            });
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"SettingsPage: {ex.Message}"); }
    }

    /// <summary>
    /// Updates the behavior settings checkboxes with current configuration values.
    /// </summary>
    private void UpdateBehaviorSettings()
    {
        try
        {
            if (AutoRefreshPreviewCheckBox != null)
                AutoRefreshPreviewCheckBox.IsChecked = AppConfiguration.AutoRefreshPreview;

            if (ShowNotificationsCheckBox != null)
                ShowNotificationsCheckBox.IsChecked = AppConfiguration.ShowGenerationNotifications;

            if (AutoSaveHistoryCheckBox != null)
                AutoSaveHistoryCheckBox.IsChecked = AppConfiguration.AutoSaveToHistory;

            if (EnableAnimatedApplyCheckBox != null)
                EnableAnimatedApplyCheckBox.IsChecked = AppConfiguration.EnableAnimatedApply;

            if (EnablePerMonitorApplyCheckBox != null)
                EnablePerMonitorApplyCheckBox.IsChecked = AppConfiguration.EnablePerMonitorApply;

            if (StartWithWindowsCheckBox != null)
                StartWithWindowsCheckBox.IsChecked = StartupService.IsEnabled();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating behavior settings: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates the path text boxes with current configuration values.
    /// </summary>
    private void UpdatePathsUI()
    {
        try
        {
            if (BackgroundsPathTextBox != null)
                BackgroundsPathTextBox.Text = AppConfiguration.BackgroundsDirectory;

            if (QuotesPathTextBox != null)
                QuotesPathTextBox.Text = AppConfiguration.QuotesFilePath;

            if (OutputPathTextBox != null)
                OutputPathTextBox.Text = AppConfiguration.CurrentWallpaperPath;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating paths UI: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates the default paths information text block.
    /// Shows users what the default paths are.
    /// </summary>
    private void UpdateDefaultsInfo()
    {
        try
        {
            if (DefaultPathsInfo == null)
                return;

            var baseDir = AppConfiguration.DefaultBaseDirectory;
            DefaultPathsInfo.Text =
                $"Base: {baseDir}\n" +
                $"Backgrounds: {Path.Combine(baseDir, "backgrounds")}\n" +
                $"Quotes: {Path.Combine(baseDir, "quotes.json")}\n" +
                $"Output: {Path.Combine(baseDir, "current.png")}";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating defaults info: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates the theme combo box to reflect the current theme mode.
    /// </summary>
    private void UpdateThemeCombo()
    {
        try
        {
            if (ThemeModeComboBox == null)
                return;

            var mode = AppConfiguration.ThemeMode;
            // Map theme mode to combo box index: 0=System, 1=Light, 2=Dark
            var index = mode.Equals("Light", StringComparison.OrdinalIgnoreCase) ? 1 :
                        mode.Equals("Dark", StringComparison.OrdinalIgnoreCase) ? 2 : 0;
            ThemeModeComboBox.SelectedIndex = index;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating theme combo: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles theme mode combo box selection change. Applies the new theme
    /// immediately via <see cref="ThemeManager.ApplyTheme"/>; no restart is required.
    /// </summary>
    private void ThemeModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            var index = ThemeModeComboBox.SelectedIndex;
            var mode = index == 1 ? "Light" : index == 2 ? "Dark" : "System";
            AppConfiguration.ThemeMode = mode;
            ThemeManager.ApplyTheme();
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"SettingsPage: {ex.Message}"); }
    }

    /// <summary>
    /// Shows the restart-required warning banner.
    /// </summary>
    private void ShowRestartWarning()
    {
        if (RestartWarningBanner != null)
            RestartWarningBanner.Visibility = Visibility.Visible;
    }

    /// <summary>
    /// Shows or hides the path validation error banner.
    /// </summary>
    private void ShowPathError(string? message)
    {
        if (PathValidationErrorBanner == null) return;
        if (string.IsNullOrEmpty(message))
        {
            PathValidationErrorBanner.Visibility = Visibility.Collapsed;
        }
        else
        {
            if (PathValidationErrorText != null)
                PathValidationErrorText.Text = message;
            PathValidationErrorBanner.Visibility = Visibility.Visible;
        }
    }

    /// <summary>
    /// Validates that a directory path exists (or can be created) and is writable.
    /// Returns an error message on failure, or null on success.
    /// </summary>
    private static string? ValidateDirectoryPath(string path)
    {
        try
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            // Test write access
            var testFile = Path.Combine(path, $"__write_test_{Guid.NewGuid():N}.tmp");
            File.WriteAllText(testFile, "test");
            File.Delete(testFile);
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return $"Access denied: you do not have write permission to '{path}'.";
        }
        catch (Exception ex)
        {
            return $"Path error: {ex.Message}";
        }
    }

    /// <summary>
    /// Validates that a file path's parent directory exists (or can be created) and is writable.
    /// Returns an error message on failure, or null on success.
    /// </summary>
    private static string? ValidateFilePath(string path)
    {
        try
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
            {
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                var testFile = Path.Combine(dir, $"__write_test_{Guid.NewGuid():N}.tmp");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);
            }
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return $"Access denied: you do not have write permission to the directory containing '{path}'.";
        }
        catch (Exception ex)
        {
            return $"Path error: {ex.Message}";
        }
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
            var validationError = ValidateDirectoryPath(dialog.SelectedPath);
            if (validationError != null)
            {
                ShowPathError(validationError);
                return;
            }
            try
            {
                ShowPathError(null);
                AppConfiguration.SetCustomBackgroundsPath(dialog.SelectedPath);
                UpdatePathsUI();
                ShowRestartWarning();
            }
            catch (Exception ex)
            {
                ShowPathError($"Invalid backgrounds path: {ex.Message}");
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
            var validationError = ValidateFilePath(dialog.FileName);
            if (validationError != null)
            {
                ShowPathError(validationError);
                return;
            }
            try
            {
                ShowPathError(null);
                AppConfiguration.SetCustomQuotesPath(dialog.FileName);
                UpdatePathsUI();
                ShowRestartWarning();
            }
            catch (Exception ex)
            {
                ShowPathError($"Invalid quotes file path: {ex.Message}");
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
            var validationError = ValidateFilePath(dialog.FileName);
            if (validationError != null)
            {
                ShowPathError(validationError);
                return;
            }
            try
            {
                ShowPathError(null);
                AppConfiguration.SetCustomOutputPath(dialog.FileName);
                UpdatePathsUI();
                ShowRestartWarning();
            }
            catch (Exception ex)
            {
                ShowPathError($"Invalid output path: {ex.Message}");
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
            ShowPathError(null);
            // Hide restart banner only if it was previously triggered by a path change
            // (after reset, paths are back to default — no restart needed for the next session)
            if (RestartWarningBanner != null)
                RestartWarningBanner.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            ShowPathError($"Failed to reset paths: {ex.Message}");
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
    /// Registers the app to start at sign-in via the per-user HKCU Run key (no admin needed).
    /// </summary>
    private void StartWithWindowsCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        var exePath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(exePath))
            return;
        if (!StartupService.SetEnabled(true, exePath))
            ToastService.ShowError("Could not enable Start with Windows.");
    }

    /// <summary>
    /// Removes the per-user startup entry.
    /// </summary>
    private void StartWithWindowsCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        StartupService.SetEnabled(false, Environment.ProcessPath ?? string.Empty);
    }

    /// <summary>
    /// Populates the Performance and Power controls from the current configuration.
    /// </summary>
    private void UpdatePerformanceSettings()
    {
        try
        {
            if (LowPowerModeCheckBox != null) LowPowerModeCheckBox.IsChecked = AppConfiguration.LowPowerMode;
            if (PauseOnFullscreenCheckBox != null) PauseOnFullscreenCheckBox.IsChecked = AppConfiguration.AutoPauseOnFullscreen;
            if (PauseOnMaximizedCheckBox != null) PauseOnMaximizedCheckBox.IsChecked = AppConfiguration.PauseOnMaximizedWindow;
            if (PauseOnBatteryCheckBox != null) PauseOnBatteryCheckBox.IsChecked = AppConfiguration.PauseOnBattery;
            if (PauseOnRemoteDesktopCheckBox != null) PauseOnRemoteDesktopCheckBox.IsChecked = AppConfiguration.PauseOnRemoteDesktop;
            if (CoverageThresholdSlider != null) CoverageThresholdSlider.Value = AppConfiguration.MaximizedCoverageThresholdPercent;
            if (FpsCapSlider != null) FpsCapSlider.Value = AppConfiguration.AnimationFpsCap;
            if (RenderScaleSlider != null) RenderScaleSlider.Value = AppConfiguration.RenderScalePercent;
            if (PerAppPauseTextBox != null) PerAppPauseTextBox.Text = string.Join(", ", AppConfiguration.PerAppPauseProcesses);
            UpdateCoverageThresholdLabel();
            UpdateFpsCapLabel();
            UpdateRenderScaleLabel();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating performance settings: {ex.Message}");
        }
    }

    private void UpdateCoverageThresholdLabel()
    {
        if (CoverageThresholdLabel != null)
            CoverageThresholdLabel.Text = $"Maximized coverage threshold: {(int)(CoverageThresholdSlider?.Value ?? 95)}%";
    }

    private void UpdateFpsCapLabel()
    {
        if (FpsCapLabel != null)
            FpsCapLabel.Text = $"Animation framerate cap: {(int)(FpsCapSlider?.Value ?? 30)} fps";
    }

    private void UpdateRenderScaleLabel()
    {
        if (RenderScaleLabel != null)
            RenderScaleLabel.Text = $"Render scale: {(int)(RenderScaleSlider?.Value ?? 100)}%";
    }

    private void LowPowerModeCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        AppConfiguration.LowPowerMode = true;
    }

    private void LowPowerModeCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        AppConfiguration.LowPowerMode = false;
    }

    private void PauseOnFullscreenCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        AppConfiguration.AutoPauseOnFullscreen = true;
    }

    private void PauseOnFullscreenCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        AppConfiguration.AutoPauseOnFullscreen = false;
    }

    private void PauseOnMaximizedCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        AppConfiguration.PauseOnMaximizedWindow = true;
    }

    private void PauseOnMaximizedCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        AppConfiguration.PauseOnMaximizedWindow = false;
    }

    private void PauseOnBatteryCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        AppConfiguration.PauseOnBattery = true;
    }

    private void PauseOnBatteryCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        AppConfiguration.PauseOnBattery = false;
    }

    private void PauseOnRemoteDesktopCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        AppConfiguration.PauseOnRemoteDesktop = true;
    }

    private void PauseOnRemoteDesktopCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        AppConfiguration.PauseOnRemoteDesktop = false;
    }

    private void CoverageThresholdSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        AppConfiguration.MaximizedCoverageThresholdPercent = (int)e.NewValue;
        UpdateCoverageThresholdLabel();
    }

    private void FpsCapSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        AppConfiguration.AnimationFpsCap = (int)e.NewValue;
        UpdateFpsCapLabel();
    }

    private void RenderScaleSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        AppConfiguration.RenderScalePercent = (int)e.NewValue;
        UpdateRenderScaleLabel();
    }

    /// <summary>
    /// Parses the comma/semicolon-separated process list into the per-app pause configuration.
    /// </summary>
    private void PerAppPauseTextBox_LostKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
    {
        try
        {
            var names = (PerAppPauseTextBox?.Text ?? string.Empty)
                .Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
            AppConfiguration.PerAppPauseProcesses = names;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error parsing per-app pause list: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates the monitor settings UI with current configuration.
    /// </summary>
    private void UpdateMonitorSettings()
    {
        try
        {
            // Update monitor mode combo box
            if (MonitorModeComboBox != null)
            {
                var mode = AppConfiguration.MultiMonitorMode;
                var modeIndex = mode switch
                {
                    "All" => 1,
                    "Span" => 2,
                    _ => 0 // Primary
                };
                MonitorModeComboBox.SelectedIndex = modeIndex;
            }

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
            if (MonitorCheckboxesPanel == null)
                return;

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
            if (MonitorModeComboBox?.SelectedItem is ComboBoxItem item && item.Tag is string mode)
            {
                AppConfiguration.MultiMonitorMode = mode;

                // Update monitor selection panel visibility
                // Only show checkboxes for "All" mode (per-monitor selection)
                if (MonitorSelectionPanel != null)
                {
                    MonitorSelectionPanel.Visibility = mode == "All" ? Visibility.Visible : Visibility.Collapsed;
                }
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

    /// <summary>
    /// Handles Enable Animated Apply checkbox checked event.
    /// </summary>
    private void EnableAnimatedApplyCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is System.Windows.Controls.CheckBox checkBox && checkBox.IsChecked == true)
            {
                AppConfiguration.EnableAnimatedApply = true;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error setting EnableAnimatedApply: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles Enable Animated Apply checkbox unchecked event.
    /// </summary>
    private void EnableAnimatedApplyCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is System.Windows.Controls.CheckBox checkBox && checkBox.IsChecked == false)
            {
                AppConfiguration.EnableAnimatedApply = false;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error setting EnableAnimatedApply: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles Enable Per-Monitor Apply checkbox checked event.
    /// </summary>
    private void EnablePerMonitorApplyCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is System.Windows.Controls.CheckBox checkBox && checkBox.IsChecked == true)
            {
                AppConfiguration.EnablePerMonitorApply = true;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error setting EnablePerMonitorApply: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles Enable Per-Monitor Apply checkbox unchecked event.
    /// </summary>
    private void EnablePerMonitorApplyCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is System.Windows.Controls.CheckBox checkBox && checkBox.IsChecked == false)
            {
                AppConfiguration.EnablePerMonitorApply = false;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error setting EnablePerMonitorApply: {ex.Message}");
        }
    }

    private void OpenDataFolderButton_Click(object sender, RoutedEventArgs e)
    {
        var dataDir = AppConfiguration.BaseDirectory;
        Directory.CreateDirectory(dataDir);
        ShellLauncher.OpenFolder(dataDir);
    }

    private void OpenLogFolderButton_Click(object sender, RoutedEventArgs e)
    {
        var logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AnimeQuoteWall",
            "logs");
        Directory.CreateDirectory(logDir);
        ShellLauncher.OpenFolder(logDir);
    }

    private void AboutButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new AboutDialog { Owner = Window.GetWindow(this) };
        dialog.ShowDialog();
    }

    private async void ExportBackupButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "AnimeQuoteWall backup (*.zip)|*.zip",
            FileName = $"AnimeQuoteWall-Backup-{DateTime.Now:yyyyMMdd-HHmm}.zip",
            Title = "Export AnimeQuoteWall backup"
        };
        if (dialog.ShowDialog(Window.GetWindow(this)) != true) return;

        try
        {
            var service = new BackupService();
            await service.ExportAsync(dialog.FileName).ConfigureAwait(true);
            ToastService.ShowSuccess($"Backup written to {Path.GetFileName(dialog.FileName)}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExportBackupButton_Click: {ex.Message}");
            ToastService.ShowError($"Export failed: {ex.Message}");
        }
    }

    private async void ImportBackupButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "AnimeQuoteWall backup (*.zip)|*.zip",
            Title = "Import AnimeQuoteWall backup"
        };
        if (dialog.ShowDialog(Window.GetWindow(this)) != true) return;

        var confirm = System.Windows.MessageBox.Show(
            Window.GetWindow(this),
            "Importing a backup will overwrite settings.json, quotes.json, playlists, and the backgrounds folder. Continue?",
            "Confirm import",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            var service = new BackupService();
            await service.ImportAsync(dialog.FileName).ConfigureAwait(true);
            ToastService.ShowSuccess("Backup restored. Some changes may require a restart.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ImportBackupButton_Click: {ex.Message}");
            ToastService.ShowError($"Import failed: {ex.Message}");
        }
    }
}

