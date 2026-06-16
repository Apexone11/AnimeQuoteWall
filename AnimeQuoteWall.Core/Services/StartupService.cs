using System;
using Microsoft.Win32;

namespace AnimeQuoteWall.Core.Services;

/// <summary>
/// Manages whether the application starts automatically when the current user signs in to
/// Windows, using the per-user <c>HKCU\Software\Microsoft\Windows\CurrentVersion\Run</c> key.
/// This deliberately never touches HKLM and therefore needs no elevation/admin rights.
/// </summary>
public static class StartupService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "AnimeQuoteWall";

    /// <summary>
    /// Returns true if the application is registered to start with Windows for the current user.
    /// </summary>
    public static bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
            return key?.GetValue(ValueName) is string value && !string.IsNullOrWhiteSpace(value);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"StartupService.IsEnabled failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Enables or disables launching the app at sign-in for the current user. When enabling, the
    /// executable path is stored (quoted, so paths with spaces are safe) under the HKCU Run key.
    /// </summary>
    /// <param name="enabled">True to register startup; false to remove the entry.</param>
    /// <param name="executablePath">Absolute path to the application executable.</param>
    /// <returns>True on success; false if the registry could not be updated.</returns>
    public static bool SetEnabled(bool enabled, string executablePath)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
                            ?? Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
            if (key == null)
                return false;

            if (enabled)
            {
                if (string.IsNullOrWhiteSpace(executablePath))
                    return false;
                key.SetValue(ValueName, $"\"{executablePath}\"");
            }
            else if (key.GetValue(ValueName) != null)
            {
                key.DeleteValue(ValueName, throwOnMissingValue: false);
            }

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"StartupService.SetEnabled failed: {ex.Message}");
            return false;
        }
    }
}
