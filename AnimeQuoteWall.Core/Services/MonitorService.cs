using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace AnimeQuoteWall.Core.Services;

/// <summary>
/// Service for detecting and managing multiple monitors.
/// Provides information about monitor resolutions, positions, and configurations.
/// </summary>
public class MonitorService
{
    /// <summary>
    /// Represents information about a single monitor.
    /// </summary>
    public class MonitorInfo
    {
        /// <summary>
        /// Gets or sets the monitor index (0-based).
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Gets or sets the monitor's display name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the monitor's resolution width in pixels.
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// Gets or sets the monitor's resolution height in pixels.
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// Gets or sets the X position of the monitor (for multi-monitor setups).
        /// </summary>
        public int X { get; set; }

        /// <summary>
        /// Gets or sets the Y position of the monitor (for multi-monitor setups).
        /// </summary>
        public int Y { get; set; }

        /// <summary>
        /// Gets or sets whether this is the primary monitor.
        /// </summary>
        public bool IsPrimary { get; set; }

        /// <summary>
        /// Gets the monitor's bounds rectangle.
        /// </summary>
        public Rectangle Bounds => new Rectangle(X, Y, Width, Height);

        /// <summary>
        /// Gets a string representation of the monitor.
        /// </summary>
        public override string ToString()
        {
            return $"{Name} ({Width}x{Height}){(IsPrimary ? " [Primary]" : "")}";
        }
    }

    /// <summary>
    /// Gets information about all connected monitors.
    /// </summary>
    /// <returns>List of monitor information.</returns>
    public List<MonitorInfo> GetAllMonitors()
    {
        var monitors = new List<MonitorInfo>();

        try
        {
            var screens = System.Windows.Forms.Screen.AllScreens;
            if (screens == null || screens.Length == 0)
            {
                // Fallback: create a default monitor entry
                var defaultRes = WindowsCompatibilityHelper.GetPrimaryScreenResolution();
                monitors.Add(new MonitorInfo
                {
                    Index = 0,
                    Name = "Primary Monitor",
                    Width = defaultRes.width,
                    Height = defaultRes.height,
                    X = 0,
                    Y = 0,
                    IsPrimary = true
                });
                return monitors;
            }

            for (int i = 0; i < screens.Length; i++)
            {
                try
                {
                    var screen = screens[i];
                    var monitor = new MonitorInfo
                    {
                        Index = i,
                        Name = $"Monitor {i + 1}",
                        Width = screen.Bounds.Width,
                        Height = screen.Bounds.Height,
                        X = screen.Bounds.X,
                        Y = screen.Bounds.Y,
                        IsPrimary = screen.Primary
                    };

                    // Try to get a better name if available
                    try
                    {
                        monitor.Name = screen.DeviceName;
                    }
                    catch
                    {
                        // Use default name if DeviceName is not available
                    }

                    monitors.Add(monitor);
                }
                catch
                {
                    // Skip invalid monitor entries
                    continue;
                }
            }

            // Ensure at least one monitor is returned
            if (monitors.Count == 0)
            {
                var defaultRes = WindowsCompatibilityHelper.GetPrimaryScreenResolution();
                monitors.Add(new MonitorInfo
                {
                    Index = 0,
                    Name = "Primary Monitor",
                    Width = defaultRes.width,
                    Height = defaultRes.height,
                    X = 0,
                    Y = 0,
                    IsPrimary = true
                });
            }
        }
        catch
        {
            // Complete fallback: return default monitor
            var defaultRes = WindowsCompatibilityHelper.GetPrimaryScreenResolution();
            monitors.Add(new MonitorInfo
            {
                Index = 0,
                Name = "Primary Monitor",
                Width = defaultRes.width,
                Height = defaultRes.height,
                X = 0,
                Y = 0,
                IsPrimary = true
            });
        }

        return monitors;
    }

    /// <summary>
    /// Gets the primary monitor information.
    /// </summary>
    /// <returns>The primary monitor, or null if no monitors are detected.</returns>
    public MonitorInfo? GetPrimaryMonitor()
    {
        var monitors = GetAllMonitors();
        return monitors.FirstOrDefault(m => m.IsPrimary) ?? monitors.FirstOrDefault();
    }

    /// <summary>
    /// Gets the combined bounds of all monitors (virtual desktop size).
    /// </summary>
    /// <returns>A rectangle representing the combined bounds.</returns>
    public Rectangle GetCombinedBounds()
    {
        try
        {
            var screens = System.Windows.Forms.Screen.AllScreens;
            if (screens == null || screens.Length == 0)
            {
                var defaultRes = WindowsCompatibilityHelper.GetPrimaryScreenResolution();
                return new Rectangle(0, 0, defaultRes.width, defaultRes.height);
            }

            int minX = screens.Min(s => s.Bounds.Left);
            int minY = screens.Min(s => s.Bounds.Top);
            int maxX = screens.Max(s => s.Bounds.Right);
            int maxY = screens.Max(s => s.Bounds.Bottom);

            return new Rectangle(minX, minY, maxX - minX, maxY - minY);
        }
        catch
        {
            // Fallback to default resolution
            var defaultRes = WindowsCompatibilityHelper.GetPrimaryScreenResolution();
            return new Rectangle(0, 0, defaultRes.width, defaultRes.height);
        }
    }

    /// <summary>
    /// Gets the total number of connected monitors.
    /// </summary>
    /// <returns>The number of monitors.</returns>
    public int GetMonitorCount()
    {
        try
        {
            return WindowsCompatibilityHelper.GetMonitorCount();
        }
        catch
        {
            return 1; // Fallback to single monitor
        }
    }

    /// <summary>
    /// Gets a monitor by its index.
    /// </summary>
    /// <param name="index">The monitor index (0-based).</param>
    /// <returns>The monitor information, or null if index is invalid.</returns>
    public MonitorInfo? GetMonitorByIndex(int index)
    {
        var monitors = GetAllMonitors();
        if (index >= 0 && index < monitors.Count)
            return monitors[index];
        return null;
    }

    /// <summary>
    /// Checks if the monitor configuration has changed since last check.
    /// Useful for detecting when monitors are connected/disconnected.
    /// </summary>
    /// <param name="lastKnownCount">The last known monitor count.</param>
    /// <returns>True if the monitor count has changed.</returns>
    public bool HasMonitorConfigurationChanged(int lastKnownCount)
    {
        return GetMonitorCount() != lastKnownCount;
    }
}

