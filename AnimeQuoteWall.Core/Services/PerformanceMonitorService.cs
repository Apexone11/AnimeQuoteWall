using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace AnimeQuoteWall.Core.Services;

/// <summary>
/// Service for monitoring system performance and detecting fullscreen applications.
/// Automatically pauses wallpaper changes when fullscreen applications are running.
/// </summary>
public class PerformanceMonitorService : IDisposable
{
    private bool _isDisposed = false;
    private bool _isMonitoring = false;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _monitoringTask;

    /// <summary>
    /// Gets or sets whether a fullscreen application is currently active.
    /// </summary>
    public bool IsFullscreenActive { get; private set; }

    /// <summary>
    /// Gets or sets whether auto-pause on fullscreen is enabled.
    /// </summary>
    public bool AutoPauseEnabled { get; set; } = true;

    /// <summary>Gets or sets whether to pause while the device is on battery power.</summary>
    public bool PauseOnBattery { get; set; }

    /// <summary>Gets or sets whether to pause when a foreground window covers most of the screen.</summary>
    public bool PauseOnMaximizedWindow { get; set; }

    /// <summary>Gets or sets the screen-coverage percent (50-100) that counts as a maximized window.</summary>
    public int MaximizedCoverageThresholdPercent { get; set; } = 95;

    /// <summary>Gets or sets whether to pause during Remote Desktop sessions.</summary>
    public bool PauseOnRemoteDesktop { get; set; }

    /// <summary>Foreground process names (without extension) that force a pause when focused.</summary>
    public IReadOnlyList<string> PerAppPauseProcesses { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the polling interval in milliseconds for checking fullscreen state.
    /// Default: 2000ms (2 seconds).
    /// </summary>
    public int PollingIntervalMs { get; set; } = 2000;

    /// <summary>
    /// Event raised when fullscreen state changes.
    /// </summary>
    public event EventHandler<bool>? FullscreenStateChanged;

    /// <summary>
    /// Gets whether the wallpaper engine should currently be paused, considering every enabled
    /// pause rule (fullscreen, maximized/coverage, battery, Remote Desktop, per-app process).
    /// </summary>
    public bool ShouldPause { get; private set; }

    /// <summary>Event raised when the aggregate <see cref="ShouldPause"/> state changes.</summary>
    public event EventHandler<bool>? ShouldPauseChanged;

    // Windows API declarations for detecting fullscreen windows
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;
    private const int SM_REMOTESESSION = 0x1000; // GetSystemMetrics index: nonzero inside an RDP session

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemPowerStatus(out SYSTEM_POWER_STATUS lpSystemPowerStatus);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [StructLayout(LayoutKind.Sequential)]
    private struct SYSTEM_POWER_STATUS
    {
        public byte ACLineStatus;       // 0 = on battery, 1 = on AC, 255 = unknown
        public byte BatteryFlag;
        public byte BatteryLifePercent;
        public byte SystemStatusFlag;
        public int BatteryLifeTime;
        public int BatteryFullLifeTime;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    /// <summary>
    /// Starts monitoring for fullscreen applications.
    /// </summary>
    public void StartMonitoring()
    {
        if (_isMonitoring)
            return;

        _isMonitoring = true;
        _cancellationTokenSource = new CancellationTokenSource();
        _monitoringTask = Task.Run(() => MonitorFullscreenAsync(_cancellationTokenSource.Token));
    }

    /// <summary>
    /// Stops monitoring for fullscreen applications.
    /// </summary>
    public void StopMonitoring()
    {
        if (!_isMonitoring)
            return;

        _isMonitoring = false;
        _cancellationTokenSource?.Cancel();
        _monitoringTask?.Wait(TimeSpan.FromSeconds(5));
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
        _monitoringTask = null;
    }

    /// <summary>
    /// Checks if the foreground window is fullscreen.
    /// Includes compatibility handling for different Windows versions and screen configurations.
    /// </summary>
    /// <returns>True if a fullscreen application is active.</returns>
    public bool CheckFullscreen()
    {
        try
        {
            var foregroundWindow = GetForegroundWindow();
            if (foregroundWindow == IntPtr.Zero)
            {
                IsFullscreenActive = false;
                return false;
            }

            if (!GetWindowRect(foregroundWindow, out var windowRect))
            {
                IsFullscreenActive = false;
                return false;
            }

            // Use compatibility helper for screen resolution (handles multi-monitor and fallbacks)
            var (screenWidth, screenHeight) = WindowsCompatibilityHelper.GetPrimaryScreenResolution();

            var windowWidth = windowRect.Right - windowRect.Left;
            var windowHeight = windowRect.Bottom - windowRect.Top;

            // Check if window covers the entire screen (with tolerance for taskbar)
            // Tolerance varies by Windows version (Windows 11 has different taskbar behavior)
            var taskbarTolerance = 50; // Default tolerance
            var version = WindowsCompatibilityHelper.GetWindowsVersion();
            if (version == WindowsVersion.Windows11)
            {
                taskbarTolerance = 40; // Windows 11 taskbar is typically smaller
            }

            var isFullscreen = windowWidth >= screenWidth - 10 && // 10px tolerance for window borders
                              windowHeight >= screenHeight - taskbarTolerance;

            if (isFullscreen != IsFullscreenActive)
            {
                IsFullscreenActive = isFullscreen;
                FullscreenStateChanged?.Invoke(this, isFullscreen);
            }

            return isFullscreen;
        }
        catch
        {
            // If detection fails, assume not fullscreen (safer default)
            IsFullscreenActive = false;
            return false;
        }
    }

    /// <summary>
    /// Returns true when the device is currently running on battery (not AC) power.
    /// </summary>
    public bool IsOnBattery()
    {
        try
        {
            if (GetSystemPowerStatus(out var status))
            {
                // ACLineStatus: 0 = on battery, 1 = on AC, 255 = unknown. Treat unknown as AC.
                return status.ACLineStatus == 0;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PerformanceMonitorService.IsOnBattery failed: {ex.Message}");
        }
        return false;
    }

    /// <summary>
    /// Returns true when the current session is a Remote Desktop (Terminal Services) session.
    /// </summary>
    public bool IsRemoteSession()
    {
        try
        {
            return GetSystemMetrics(SM_REMOTESESSION) != 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PerformanceMonitorService.IsRemoteSession failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Returns true when the foreground window covers at least
    /// <see cref="MaximizedCoverageThresholdPercent"/> percent of the primary screen area.
    /// Distinct from exclusive fullscreen: catches maximized normal windows.
    /// </summary>
    public bool IsForegroundWindowMaximized()
    {
        try
        {
            IntPtr hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero) return false;
            if (!GetWindowRect(hwnd, out var r)) return false;

            var (screenWidth, screenHeight) = WindowsCompatibilityHelper.GetPrimaryScreenResolution();
            if (screenWidth <= 0 || screenHeight <= 0) return false;

            // Coverage = area of the window/screen intersection over total screen area.
            var left = Math.Max(0, r.Left);
            var top = Math.Max(0, r.Top);
            var right = Math.Min(screenWidth, r.Right);
            var bottom = Math.Min(screenHeight, r.Bottom);
            var visibleWidth = Math.Max(0, right - left);
            var visibleHeight = Math.Max(0, bottom - top);

            var coverage = (double)visibleWidth * visibleHeight / ((double)screenWidth * screenHeight) * 100.0;
            return coverage >= MaximizedCoverageThresholdPercent;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PerformanceMonitorService.IsForegroundWindowMaximized failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Returns true when the foreground window belongs to a process named in
    /// <see cref="PerAppPauseProcesses"/> (case-insensitive, ".exe" optional).
    /// </summary>
    public bool IsBlockedAppForeground()
    {
        try
        {
            if (PerAppPauseProcesses == null || PerAppPauseProcesses.Count == 0) return false;

            IntPtr hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero) return false;

            _ = GetWindowThreadProcessId(hwnd, out uint pid);
            if (pid == 0) return false;

            using var process = System.Diagnostics.Process.GetProcessById((int)pid);
            var name = process.ProcessName; // already without the .exe extension

            foreach (var blocked in PerAppPauseProcesses)
            {
                if (string.IsNullOrWhiteSpace(blocked)) continue;
                var normalized = blocked.Trim();
                if (normalized.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    normalized = normalized[..^4];
                if (string.Equals(name, normalized, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PerformanceMonitorService.IsBlockedAppForeground failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Evaluates every enabled pause rule and updates <see cref="ShouldPause"/>, raising
    /// <see cref="ShouldPauseChanged"/> when the aggregate state flips. Returns the new state.
    /// </summary>
    public bool CheckPausePolicy()
    {
        var pause = false;

        // Exclusive-fullscreen rule reuses the existing detector (this also keeps
        // IsFullscreenActive and FullscreenStateChanged current for older consumers).
        if (AutoPauseEnabled && CheckFullscreen()) pause = true;
        if (!pause && PauseOnMaximizedWindow && IsForegroundWindowMaximized()) pause = true;
        if (!pause && PauseOnBattery && IsOnBattery()) pause = true;
        if (!pause && PauseOnRemoteDesktop && IsRemoteSession()) pause = true;
        if (!pause && IsBlockedAppForeground()) pause = true;

        if (pause != ShouldPause)
        {
            ShouldPause = pause;
            ShouldPauseChanged?.Invoke(this, pause);
        }
        return pause;
    }

    /// <summary>
    /// Background task that continuously monitors for fullscreen applications.
    /// </summary>
    private async Task MonitorFullscreenAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            // Evaluate the full pause policy each tick (fullscreen + maximized + battery + RDP +
            // per-app). CheckPausePolicy gates the fullscreen rule behind AutoPauseEnabled itself
            // and updates ShouldPause / raises ShouldPauseChanged. This is the single evaluator so
            // consumers (PlaylistWorker) can simply read ShouldPause.
            CheckPausePolicy();

            try
            {
                await Task.Delay(PollingIntervalMs, cancellationToken).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }

    /// <summary>
    /// Disposes resources used by the performance monitor.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        StopMonitoring();
        _isDisposed = true;
    }
}

