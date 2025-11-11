using System;
using System.Windows;
using AnimeQuoteWall.Core.Configuration;
using AnimeQuoteWall.Core.Protection;

namespace AnimeQuoteWall.GUI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        // Global exception handler
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

        try
        {
            // Initialize code protection (for Steam release)
            CodeProtection.Initialize();
            
            base.OnStartup(e);
            
            // Apply theme before any window is created
            ThemeManager.ApplyTheme();
            ThemeManager.StartSystemThemeWatch();

            // Verify theme resources are loaded
            if (Current.Resources["WindowBackground"] == null)
            {
                System.Windows.MessageBox.Show("Theme resources failed to load. The application may not display correctly.", "Warning", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            }

            // Ensure directories exist early (async to not block UI)
            _ = System.Threading.Tasks.Task.Run(() => AppConfiguration.EnsureDirectories());

            var window = new SimpleMainWindow();
            MainWindow = window; // Set as main window
            window.Show();

            // Cleanup old thumbnails in background after window is shown
            _ = System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    var thumbnailService = new Core.Services.VideoThumbnailService();
                    thumbnailService.CleanupOldThumbnails(30);
                }
                catch { /* Ignore cleanup errors */ }
            });
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Startup Error: {ex.Message}\n\n{ex.StackTrace}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            Shutdown();
        }
    }

    private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        System.Windows.MessageBox.Show($"Unhandled Exception: {e.Exception.Message}\n\n{e.Exception.StackTrace}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        e.Handled = true; // Prevent app crash
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            System.Windows.MessageBox.Show($"Unhandled Exception: {ex.Message}\n\n{ex.StackTrace}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }
}

