using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace AnimeQuoteWall.Core.Services;

/// <summary>
/// Service for setting wallpapers on individual monitors using Windows 10/11 IDesktopWallpaper COM interface.
/// Provides true per-monitor wallpaper support.
/// </summary>
public class PerMonitorWallpaperService : IDisposable
{
    // COM Interface GUIDs
    private static readonly Guid CLSID_DesktopWallpaper = new Guid("C2CF3110-460E-4fc1-B9D0-8A1C0C9CC4BD");
    private static readonly Guid IID_IDesktopWallpaper = new Guid("B92B56A9-8B55-4E14-9A89-0199BBB6F93B");

    // IDesktopWallpaper COM Interface
    [ComImport]
    [Guid("B92B56A9-8B55-4E14-9A89-0199BBB6F93B")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IDesktopWallpaper
    {
        void SetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string? monitorID, [MarshalAs(UnmanagedType.LPWStr)] string wallpaper);
        [return: MarshalAs(UnmanagedType.LPWStr)]
        string GetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string? monitorID);
        [return: MarshalAs(UnmanagedType.LPWStr)]
        string GetMonitorDevicePathAt(uint monitorIndex);
        [return: MarshalAs(UnmanagedType.U4)]
        uint GetMonitorDevicePathCount();
        [return: MarshalAs(UnmanagedType.Struct)]
        Rect GetMonitorRECT([MarshalAs(UnmanagedType.LPWStr)] string monitorID);
        void SetBackgroundColor([MarshalAs(UnmanagedType.U4)] uint color);
        [return: MarshalAs(UnmanagedType.U4)]
        uint GetBackgroundColor();
        void SetPosition([MarshalAs(UnmanagedType.I4)] DesktopWallpaperPosition position);
        [return: MarshalAs(UnmanagedType.I4)]
        DesktopWallpaperPosition GetPosition();
        void SetSlideshow(IntPtr items);
        IntPtr GetSlideshow();
        void SetSlideshowOptions(DesktopSlideshowDirection options, uint slideshowTick);
        void GetSlideshowOptions(out DesktopSlideshowDirection options, out uint slideshowTick);
        void AdvanceSlideshow([MarshalAs(UnmanagedType.LPWStr)] string? monitorID, [MarshalAs(UnmanagedType.I4)] DesktopSlideshowDirection direction);
        [return: MarshalAs(UnmanagedType.U4)]
        DesktopSlideshowStatus GetStatus();
        [return: MarshalAs(UnmanagedType.Bool)]
        bool Enable();
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    private enum DesktopWallpaperPosition
    {
        Center = 0,
        Tile = 1,
        Stretch = 2,
        Fit = 3,
        Fill = 4,
        Span = 5
    }

    private enum DesktopSlideshowDirection
    {
        Forward = 0,
        Backward = 1
    }

    private enum DesktopSlideshowStatus
    {
        Enabled = 0x01,
        Slideshow = 0x02,
        Disabled = 0x00
    }

    private IDesktopWallpaper? _desktopWallpaper;
    private bool _isAvailable = false;

    /// <summary>
    /// Initializes the per-monitor wallpaper service.
    /// </summary>
    public PerMonitorWallpaperService()
    {
        try
        {
            // Check if running on Windows 10/11 (IDesktopWallpaper requires Windows 8+)
            var osVersion = Environment.OSVersion.Version;
            if (osVersion.Major >= 6 && osVersion.Minor >= 2) // Windows 8+
            {
                // Create COM object
                var type = Type.GetTypeFromCLSID(CLSID_DesktopWallpaper);
                if (type != null)
                {
                    _desktopWallpaper = (IDesktopWallpaper)Activator.CreateInstance(type)!;
                    _isAvailable = true;
                }
            }
        }
        catch
        {
            _isAvailable = false;
        }
    }

    /// <summary>
    /// Checks if per-monitor wallpaper is available (Windows 8+).
    /// </summary>
    public bool IsAvailable => _isAvailable && _desktopWallpaper != null;

    /// <summary>
    /// Gets all monitor device paths.
    /// </summary>
    public List<string> GetMonitorDevicePaths()
    {
        var paths = new List<string>();
        
        if (!IsAvailable)
            return paths;

        try
        {
            uint count = _desktopWallpaper!.GetMonitorDevicePathCount();
            for (uint i = 0; i < count; i++)
            {
                var path = _desktopWallpaper.GetMonitorDevicePathAt(i);
                if (!string.IsNullOrEmpty(path))
                    paths.Add(path);
            }
        }
        catch
        {
            // Fallback: return empty list
        }

        return paths;
    }

    /// <summary>
    /// Sets wallpaper on a specific monitor by device path.
    /// </summary>
    /// <param name="monitorDevicePath">Monitor device path (from GetMonitorDevicePaths), or null for all monitors</param>
    /// <param name="wallpaperPath">Path to wallpaper image</param>
    /// <returns>True if successful</returns>
    public bool SetWallpaperOnMonitor(string? monitorDevicePath, string wallpaperPath)
    {
        if (!IsAvailable || string.IsNullOrEmpty(wallpaperPath))
            return false;

        try
        {
            if (!File.Exists(wallpaperPath))
                return false;

            var fullPath = Path.GetFullPath(wallpaperPath);
            _desktopWallpaper!.SetWallpaper(monitorDevicePath, fullPath);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Sets wallpaper on a monitor by index (0-based).
    /// </summary>
    /// <param name="monitorIndex">Monitor index (0-based)</param>
    /// <param name="wallpaperPath">Path to wallpaper image</param>
    /// <returns>True if successful</returns>
    public bool SetWallpaperOnMonitorByIndex(int monitorIndex, string wallpaperPath)
    {
        if (!IsAvailable)
            return false;

        try
        {
            var paths = GetMonitorDevicePaths();
            if (monitorIndex < 0 || monitorIndex >= paths.Count)
                return false;

            return SetWallpaperOnMonitor(paths[monitorIndex], wallpaperPath);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Sets the same wallpaper on all monitors.
    /// </summary>
    /// <param name="wallpaperPath">Path to wallpaper image</param>
    /// <returns>Number of monitors successfully updated</returns>
    public int SetWallpaperOnAllMonitors(string wallpaperPath)
    {
        if (!IsAvailable)
            return 0;

        int successCount = 0;
        try
        {
            var paths = GetMonitorDevicePaths();
            foreach (var path in paths)
            {
                if (SetWallpaperOnMonitor(path, wallpaperPath))
                    successCount++;
            }
        }
        catch
        {
            // Return count of successful operations
        }

        return successCount;
    }

    /// <summary>
    /// Sets different wallpapers on different monitors in a batch operation.
    /// This is similar to how Wallpaper Engine applies wallpapers - all at once.
    /// </summary>
    /// <param name="monitorWallpaperMap">Dictionary mapping monitor index (0-based) to wallpaper path</param>
    /// <returns>Number of monitors successfully updated</returns>
    public int SetWallpapersOnMonitors(Dictionary<int, string> monitorWallpaperMap)
    {
        if (!IsAvailable || monitorWallpaperMap == null || monitorWallpaperMap.Count == 0)
            return 0;

        int successCount = 0;
        try
        {
            var devicePaths = GetMonitorDevicePaths();
            
            // Apply all wallpapers in sequence without delays
            // Windows will batch these operations internally
            foreach (var kvp in monitorWallpaperMap)
            {
                if (kvp.Key >= 0 && kvp.Key < devicePaths.Count)
                {
                    if (!string.IsNullOrEmpty(kvp.Value) && File.Exists(kvp.Value))
                    {
                        var fullPath = Path.GetFullPath(kvp.Value);
                        _desktopWallpaper!.SetWallpaper(devicePaths[kvp.Key], fullPath);
                        successCount++;
                    }
                }
            }
        }
        catch
        {
            // Return count of successful operations
        }

        return successCount;
    }

    /// <summary>
    /// Gets the current wallpaper for a monitor.
    /// </summary>
    /// <param name="monitorDevicePath">Monitor device path</param>
    /// <returns>Wallpaper path, or null if not available</returns>
    public string? GetWallpaperForMonitor(string monitorDevicePath)
    {
        if (!IsAvailable || string.IsNullOrEmpty(monitorDevicePath))
            return null;

        try
        {
            return _desktopWallpaper!.GetWallpaper(monitorDevicePath);
        }
        catch
        {
            return null;
        }
    }

    public void Dispose()
    {
        if (_desktopWallpaper != null)
        {
            Marshal.ReleaseComObject(_desktopWallpaper);
            _desktopWallpaper = null;
        }
    }
}

