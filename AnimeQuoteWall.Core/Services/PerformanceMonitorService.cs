using System;
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

    /// <summary>
    /// Gets or sets the polling interval in milliseconds for checking fullscreen state.
    /// Default: 2000ms (2 seconds).
    /// </summary>
    public int PollingIntervalMs { get; set; } = 2000;

    /// <summary>
    /// Event raised when fullscreen state changes.
    /// </summary>
    public event EventHandler<bool>? FullscreenStateChanged;

    // Windows API declarations for detecting fullscreen windows
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;

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
    /// Background task that continuously monitors for fullscreen applications.
    /// </summary>
    private async Task MonitorFullscreenAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (AutoPauseEnabled)
            {
                CheckFullscreen();
            }

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

