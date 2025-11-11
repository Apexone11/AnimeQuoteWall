using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AnimeQuoteWall.Core.Services;

/// <summary>
/// Helper class for Windows version detection and compatibility checks.
/// Provides fallbacks for different Windows versions and hardware configurations.
/// </summary>
public static class WindowsCompatibilityHelper
{
    /// <summary>
    /// Gets the Windows version information.
    /// </summary>
    public static WindowsVersion GetWindowsVersion()
    {
        try
        {
            var version = Environment.OSVersion.Version;
            var major = version.Major;
            var minor = version.Minor;
            var build = version.Build;

            // Windows 11 has build number 22000+
            if (major == 10 && build >= 22000)
                return WindowsVersion.Windows11;

            // Windows 10
            if (major == 10)
                return WindowsVersion.Windows10;

            // Windows 8.1
            if (major == 6 && minor == 3)
                return WindowsVersion.Windows81;

            // Windows 8
            if (major == 6 && minor == 2)
                return WindowsVersion.Windows8;

            // Windows 7
            if (major == 6 && minor == 1)
                return WindowsVersion.Windows7;

            // Older versions
            return WindowsVersion.Unknown;
        }
        catch
        {
            return WindowsVersion.Unknown;
        }
    }

    /// <summary>
    /// Checks if the current Windows version supports the specified feature.
    /// </summary>
    public static bool IsFeatureSupported(WindowsFeature feature)
    {
        var version = GetWindowsVersion();

        return feature switch
        {
            WindowsFeature.RegistryThemeDetection => version >= WindowsVersion.Windows10,
            WindowsFeature.MultiMonitorSupport => version >= WindowsVersion.Windows7,
            WindowsFeature.FullscreenDetection => version >= WindowsVersion.Windows7,
            WindowsFeature.WindowsForms => true, // Available on all Windows versions we support
            _ => true
        };
    }

    /// <summary>
    /// Gets the primary screen resolution with fallback for older Windows versions.
    /// </summary>
    public static (int width, int height) GetPrimaryScreenResolution()
    {
        try
        {
            // Try using Windows Forms Screen API (most reliable)
            var primaryScreen = Screen.PrimaryScreen;
            if (primaryScreen != null)
            {
                return (primaryScreen.Bounds.Width, primaryScreen.Bounds.Height);
            }
        }
        catch
        {
            // Fall through to Windows API
        }

        try
        {
            // Fallback to Windows API
            var width = GetSystemMetrics(0); // SM_CXSCREEN
            var height = GetSystemMetrics(1); // SM_CYSCREEN
            if (width > 0 && height > 0)
            {
                return (width, height);
            }
        }
        catch
        {
            // Fall through to default
        }

        // Default fallback resolution
        return (1920, 1080);
    }

    /// <summary>
    /// Gets the number of monitors with fallback handling.
    /// </summary>
    public static int GetMonitorCount()
    {
        try
        {
            var screens = Screen.AllScreens;
            return screens?.Length ?? 1;
        }
        catch
        {
            // Fallback: assume single monitor
            return 1;
        }
    }

    /// <summary>
    /// Checks if multi-monitor support is available and working.
    /// </summary>
    public static bool IsMultiMonitorAvailable()
    {
        try
        {
            var count = GetMonitorCount();
            return count > 1;
        }
        catch
        {
            return false;
        }
    }

    // Windows API for system metrics (fallback)
    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);
}

/// <summary>
/// Represents Windows version information.
/// </summary>
public enum WindowsVersion
{
    Unknown = 0,
    Windows7 = 1,
    Windows8 = 2,
    Windows81 = 3,
    Windows10 = 4,
    Windows11 = 5
}

/// <summary>
/// Represents Windows features that may not be available on all versions.
/// </summary>
public enum WindowsFeature
{
    RegistryThemeDetection,
    MultiMonitorSupport,
    FullscreenDetection,
    WindowsForms
}

