using System;
using System.IO;
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
        try
        {
            // Log to file
            LogException(e.Exception);
            
            // Show user-friendly error dialog
            var message = GetUserFriendlyErrorMessage(e.Exception);
            var result = System.Windows.MessageBox.Show(
                message + "\n\nWould you like to continue?",
                "Application Error",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);
            
            e.Handled = result == System.Windows.MessageBoxResult.Yes;
        }
        catch
        {
            // Fallback if error handling itself fails
            e.Handled = true;
        }
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        try
        {
            if (e.ExceptionObject is Exception ex)
            {
                LogException(ex);
                var message = GetUserFriendlyErrorMessage(ex);
                System.Windows.MessageBox.Show(
                    message + "\n\nThe application will now close.",
                    "Fatal Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }
        catch
        {
            // Ignore errors in error handler
        }
    }

    /// <summary>
    /// Logs exception details to a log file.
    /// </summary>
    private void LogException(Exception ex)
    {
        try
        {
            var logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AnimeQuoteWall",
                "logs");
            Directory.CreateDirectory(logDir);
            
            var logFile = Path.Combine(logDir, $"error_{DateTime.Now:yyyyMMdd}.txt");
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}\n\n";
            
            File.AppendAllText(logFile, logEntry);
        }
        catch
        {
            // Ignore logging errors
        }
    }

    /// <summary>
    /// Converts technical exception messages to user-friendly error messages.
    /// </summary>
    private string GetUserFriendlyErrorMessage(Exception ex)
    {
        var errorType = ex.GetType().Name;
        var message = ex.Message;

        // Handle common error types with friendly messages
        if (ex is System.IO.FileNotFoundException)
            return "A required file could not be found. Please ensure all application files are present.";
        
        if (ex is System.IO.DirectoryNotFoundException)
            return "A required folder could not be found. Please check your file paths in Settings.";
        
        if (ex is UnauthorizedAccessException)
            return "Access denied. Please run the application as administrator or check file permissions.";
        
        if (ex is System.IO.IOException)
            return "A file operation failed. The file may be in use by another program.";
        
        if (ex is OutOfMemoryException)
            return "The application ran out of memory. Try closing other applications or reducing the number of images.";
        
        if (message.Contains("XamlParseException") || message.Contains("StaticResource"))
            return "A UI component failed to load. This may be resolved by restarting the application.";
        
        // Default: show simplified message
        return $"An error occurred: {message}";
    }
}

