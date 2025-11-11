using System;
using System.IO;
using System.Runtime.InteropServices;

namespace AnimeQuoteWall.Core.Services;

/// <summary>
/// Helper class for setting wallpapers with cross-version Windows compatibility.
/// Provides fallbacks and error handling for different Windows versions and configurations.
/// </summary>
public static class WallpaperSettingHelper
{
    // Windows API declarations
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

    private const int SPI_SETDESKWALLPAPER = 20;
    private const int SPIF_UPDATEINIFILE = 0x01;
    private const int SPIF_SENDCHANGE = 0x02;

    /// <summary>
    /// Sets the desktop wallpaper with comprehensive error handling and compatibility checks.
    /// </summary>
    /// <param name="imagePath">Path to the wallpaper image file</param>
    /// <returns>True if successful, false otherwise</returns>
    public static bool SetWallpaper(string imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
            return false;

        try
        {
            // Validate file exists and is accessible
            var fullPath = Path.GetFullPath(imagePath);
            if (!File.Exists(fullPath))
            {
                System.Diagnostics.Debug.WriteLine($"Wallpaper file not found: {fullPath}");
                return false;
            }

            // Check file extension (Windows supports: .bmp, .jpg, .jpeg, .png, .gif)
            var extension = Path.GetExtension(fullPath).ToLowerInvariant();
            var supportedExtensions = new[] { ".bmp", ".jpg", ".jpeg", ".png", ".gif" };
            if (!Array.Exists(supportedExtensions, ext => ext == extension))
            {
                System.Diagnostics.Debug.WriteLine($"Unsupported image format: {extension}");
                return false;
            }

            // Set wallpaper using Windows API
            // This API works on Windows 7, 8, 8.1, 10, and 11
            var result = SystemParametersInfo(
                SPI_SETDESKWALLPAPER,
                0,
                fullPath,
                SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);

            if (result == 0)
            {
                // Get last error for debugging (but don't expose to user)
                var errorCode = Marshal.GetLastWin32Error();
                System.Diagnostics.Debug.WriteLine($"SystemParametersInfo failed with error code: {errorCode}");
                return false;
            }

            return true;
        }
        catch (UnauthorizedAccessException)
        {
            // Group policy or permissions issue
            System.Diagnostics.Debug.WriteLine("Access denied when setting wallpaper. Check group policy.");
            return false;
        }
        catch (FileNotFoundException)
        {
            System.Diagnostics.Debug.WriteLine($"Wallpaper file not found: {imagePath}");
            return false;
        }
        catch (PathTooLongException)
        {
            System.Diagnostics.Debug.WriteLine($"Wallpaper path too long: {imagePath}");
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error setting wallpaper: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Sets an animated wallpaper (GIF/MP4) using Wallpaper Engine or fallback method.
    /// </summary>
    /// <param name="videoPath">Path to the animated wallpaper file</param>
    /// <returns>True if successful, false otherwise</returns>
    public static bool SetAnimatedWallpaper(string videoPath)
    {
        var service = new AnimatedWallpaperService();
        return service.SetAnimatedWallpaper(videoPath);
    }

    /// <summary>
    /// Validates that a wallpaper file can be set as desktop background.
    /// </summary>
    /// <param name="imagePath">Path to the image file</param>
    /// <returns>True if the file is valid for use as wallpaper</returns>
    public static bool ValidateWallpaperFile(string imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
            return false;

        try
        {
            if (!File.Exists(imagePath))
                return false;

            var extension = Path.GetExtension(imagePath).ToLowerInvariant();
            var supportedExtensions = new[] { ".bmp", ".jpg", ".jpeg", ".png", ".gif" };
            
            if (!Array.Exists(supportedExtensions, ext => ext == extension))
                return false;

            // Check file size (Windows has limits, typically 256MB)
            var fileInfo = new FileInfo(imagePath);
            if (fileInfo.Length > 256 * 1024 * 1024) // 256MB
                return false;

            return true;
        }
        catch
        {
            return false;
        }
    }
}

