using System;
using System.Collections.Generic;
using System.IO;

namespace AnimeQuoteWall.Core.Services;

/// <summary>
/// The decision half of per-monitor wallpaper application: which requested monitor/image pairs
/// are dispatchable, and which device path each one resolves to.
/// </summary>
/// <remarks>
/// Split out from <see cref="PerMonitorWallpaperService"/> so the index-to-device-path mapping
/// and its guards can be tested without the IDesktopWallpaper COM object or actually changing
/// the desktop (CLAUDE.md Section 9: mock the P/Invoke, never assert the wallpaper was applied).
/// </remarks>
public static class PerMonitorDispatch
{
    /// <summary>
    /// One resolved assignment: a monitor device path and the image to place on it.
    /// </summary>
    /// <param name="MonitorIndex">Zero-based index the caller requested.</param>
    /// <param name="MonitorDevicePath">Device path that index resolved to.</param>
    /// <param name="WallpaperPath">Absolute path of the image to apply.</param>
    public readonly record struct Assignment(int MonitorIndex, string MonitorDevicePath, string WallpaperPath);

    /// <summary>
    /// Resolves a monitor-index-to-image map into the assignments that can actually be applied.
    /// </summary>
    /// <param name="monitorWallpaperMap">Requested pairs, keyed by zero-based monitor index.</param>
    /// <param name="monitorDevicePaths">Device paths in monitor-index order.</param>
    /// <param name="fileExists">
    /// Existence probe for the image path. Injected so tests do not need real files; production
    /// callers pass <see cref="File.Exists(string)"/>.
    /// </param>
    /// <returns>
    /// Assignments in ascending monitor-index order, so the result does not depend on dictionary
    /// enumeration order. Entries with an out-of-range index, a blank path, or a missing file are
    /// skipped rather than throwing: one bad entry must not abandon the other monitors.
    /// </returns>
    /// <exception cref="ArgumentNullException">A required argument is null.</exception>
    public static IReadOnlyList<Assignment> Resolve(
        IReadOnlyDictionary<int, string> monitorWallpaperMap,
        IReadOnlyList<string> monitorDevicePaths,
        Func<string, bool> fileExists)
    {
        ArgumentNullException.ThrowIfNull(monitorWallpaperMap);
        ArgumentNullException.ThrowIfNull(monitorDevicePaths);
        ArgumentNullException.ThrowIfNull(fileExists);

        var indexes = new List<int>(monitorWallpaperMap.Keys);
        indexes.Sort();

        var assignments = new List<Assignment>(indexes.Count);
        foreach (int index in indexes)
        {
            if (index < 0 || index >= monitorDevicePaths.Count)
                continue;

            string wallpaperPath = monitorWallpaperMap[index];
            if (string.IsNullOrWhiteSpace(wallpaperPath) || !fileExists(wallpaperPath))
                continue;

            assignments.Add(new Assignment(index, monitorDevicePaths[index], Path.GetFullPath(wallpaperPath)));
        }

        return assignments;
    }
}
