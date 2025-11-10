using System;
using System.Windows;
using AnimeQuoteWall.Core.Configuration;

namespace AnimeQuoteWall.GUI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        try
        {
            // Apply theme before any window is created
            ThemeManager.ApplyTheme();
            ThemeManager.StartSystemThemeWatch();

            // Ensure directories exist early
            AppConfiguration.EnsureDirectories();

            base.OnStartup(e);

            var window = new SimpleMainWindow();
            window.Show();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Startup Error: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }
}

