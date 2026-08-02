using System;
using System.Diagnostics;
using System.IO;

namespace AnimeQuoteWall.GUI.Services;

/// <summary>
/// Centralized helpers for the few places where the app must hand a path or URL off
/// to the Windows shell (open a folder in Explorer, open the default browser). Every
/// caller validates input here so URLs and paths are sanity-checked before being
/// expanded by the shell.
/// </summary>
public static class ShellLauncher
{
    public static void OpenFolder(string folder)
    {
        if (string.IsNullOrWhiteSpace(folder)) return;
        if (!Directory.Exists(folder)) return;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                UseShellExecute = false,
                CreateNoWindow = false,
                Arguments = $"\"{folder}\""
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ShellLauncher.OpenFolder: {ex.Message}");
        }
    }

    public static void OpenUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return;
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = uri.AbsoluteUri,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ShellLauncher.OpenUrl: {ex.Message}");
        }
    }
}
