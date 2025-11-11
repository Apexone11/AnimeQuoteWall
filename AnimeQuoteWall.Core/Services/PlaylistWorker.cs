using System;
using System.Threading;
using System.Threading.Tasks;
using AnimeQuoteWall.Core.Configuration;
using AnimeQuoteWall.Core.Interfaces;
using AnimeQuoteWall.Core.Models;

namespace AnimeQuoteWall.Core.Services;

/// <summary>
/// Background service for automatic wallpaper rotation based on playlists.
/// Handles timer-based and scheduled wallpaper changes.
/// </summary>
public class PlaylistWorker : IDisposable
{
    private readonly PlaylistService _playlistService;
    private readonly ScheduleService _scheduleService;
    private readonly PerformanceMonitorService _performanceMonitor;
    private readonly IWallpaperService _wallpaperService;
    private readonly WallpaperHistoryService _historyService;

    private bool _isDisposed = false;
    private bool _isRunning = false;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _workerTask;
    private System.Threading.Timer? _intervalTimer;
    private DateTime? _nextScheduledExecution;

    /// <summary>
    /// Event raised when a wallpaper is changed by the playlist worker.
    /// </summary>
    public event EventHandler<PlaylistWallpaperEntry>? WallpaperChanged;

    /// <summary>
    /// Initializes a new instance of the PlaylistWorker.
    /// </summary>
    public PlaylistWorker(
        PlaylistService playlistService,
        ScheduleService scheduleService,
        PerformanceMonitorService performanceMonitor,
        IWallpaperService wallpaperService,
        WallpaperHistoryService historyService)
    {
        _playlistService = playlistService;
        _scheduleService = scheduleService;
        _performanceMonitor = performanceMonitor;
        _wallpaperService = wallpaperService;
        _historyService = historyService;
    }

    /// <summary>
    /// Starts the playlist worker.
    /// </summary>
    public void Start()
    {
        if (_isRunning)
            return;

        _isRunning = true;
        _cancellationTokenSource = new CancellationTokenSource();
        _performanceMonitor.AutoPauseEnabled = AppConfiguration.AutoPauseOnFullscreen;
        _performanceMonitor.StartMonitoring();

        _workerTask = Task.Run(() => WorkerLoopAsync(_cancellationTokenSource.Token));
    }

    /// <summary>
    /// Stops the playlist worker.
    /// </summary>
    public void Stop()
    {
        if (!_isRunning)
            return;

        _isRunning = false;
        _cancellationTokenSource?.Cancel();
        _intervalTimer?.Dispose();
        _intervalTimer = null;
        _performanceMonitor.StopMonitoring();

        _workerTask?.Wait(TimeSpan.FromSeconds(5));
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
        _workerTask = null;
    }

    /// <summary>
    /// Main worker loop that handles playlist execution.
    /// </summary>
    private async Task WorkerLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var playlist = await _playlistService.GetEnabledPlaylistAsync().ConfigureAwait(false);

                if (playlist == null || !playlist.IsValid())
                {
                    // No enabled playlist, wait and check again
                    await Task.Delay(5000, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                // Check if we should pause due to fullscreen
                if (AppConfiguration.AutoPauseOnFullscreen && _performanceMonitor.IsFullscreenActive)
                {
                    await Task.Delay(2000, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                // Handle different schedule types
                if (playlist.ScheduleType == "Interval")
                {
                    await HandleIntervalScheduleAsync(playlist, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await HandleScheduledExecutionAsync(playlist, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistWorker error: {ex.Message}");
                await Task.Delay(5000, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Handles interval-based scheduling.
    /// </summary>
    private async Task HandleIntervalScheduleAsync(Playlist playlist, CancellationToken cancellationToken)
    {
        // Execute immediately on first run
        await ExecutePlaylistAsync(playlist).ConfigureAwait(false);

        // Wait for the interval
        await Task.Delay(playlist.IntervalSeconds * 1000, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Handles scheduled execution (hourly, daily, custom).
    /// </summary>
    private async Task HandleScheduledExecutionAsync(Playlist playlist, CancellationToken cancellationToken)
    {
        var now = DateTime.Now;
        var nextExecution = _scheduleService.CalculateNextExecutionTime(playlist, now);

        if (nextExecution.HasValue)
        {
            _nextScheduledExecution = nextExecution.Value;
            var delay = (int)(nextExecution.Value - now).TotalMilliseconds;

            if (delay > 0)
            {
                await Task.Delay(Math.Min(delay, 60000), cancellationToken).ConfigureAwait(false); // Max 1 minute delay
            }
        }

        // Check if it's time to execute
        if (_scheduleService.ShouldExecuteNow(playlist, now))
        {
            await ExecutePlaylistAsync(playlist).ConfigureAwait(false);
        }
        else
        {
            // Wait a bit and check again
            await Task.Delay(5000, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Executes the playlist by generating and applying the next wallpaper.
    /// </summary>
    private async Task ExecutePlaylistAsync(Playlist playlist)
    {
        var entry = playlist.GetNextEntry();
        if (entry == null || entry.Quote == null)
            return;

        try
        {
            // Generate wallpaper
            // Get screen dimensions using compatibility helper (handles all Windows versions)
            var (screenWidth, screenHeight) = WindowsCompatibilityHelper.GetPrimaryScreenResolution();

            var settings = entry.Settings ?? new WallpaperSettings
            {
                Width = screenWidth,
                Height = screenHeight,
                BackgroundColor = "#141414",
                FontFamily = "Segoe UI",
                TextColor = "#FFFFFF",
                OutlineColor = "#000000",
                PanelColor = "#1A1A1A",
                PanelOpacity = 0.85f,
                MaxPanelWidthPercent = 0.7f,
                FontSizeFactor = 25f,
                MinFontSize = 32
            };

            await Task.Run(() =>
            {
                using var bitmap = _wallpaperService.CreateWallpaperImage(entry.BackgroundPath, entry.Quote, settings);
                _wallpaperService.SaveImageAsync(bitmap, AppConfiguration.CurrentWallpaperPath).Wait();
            }).ConfigureAwait(false);

            // Apply wallpaper
            SetWallpaper(AppConfiguration.CurrentWallpaperPath);

            // Save to history if enabled
            if (AppConfiguration.AutoSaveToHistory)
            {
                try
                {
                    await _historyService.SaveToHistoryAsync(
                        AppConfiguration.CurrentWallpaperPath,
                        entry.Quote,
                        entry.BackgroundPath,
                        settings).ConfigureAwait(false);
                }
                catch
                {
                    // Ignore history save errors
                }
            }

            // Save playlist state
            await _playlistService.UpdatePlaylistAsync(playlist).ConfigureAwait(false);

            // Raise event
            WallpaperChanged?.Invoke(this, entry);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to execute playlist: {ex.Message}");
        }
    }

    /// <summary>
    /// Windows API function for setting desktop wallpaper.
    /// </summary>
    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
    private static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

    private const int SPI_SETDESKWALLPAPER = 20;
    private const int SPIF_UPDATEINIFILE = 0x01;
    private const int SPIF_SENDCHANGE = 0x02;

    /// <summary>
    /// Sets the desktop wallpaper using Windows API.
    /// Includes compatibility handling for different Windows versions and error scenarios.
    /// </summary>
    private void SetWallpaper(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
            return;

        try
        {
            // Ensure file path is absolute and properly formatted
            var fullPath = Path.GetFullPath(path);
            
            // Windows API requires the file to exist and be accessible
            if (!File.Exists(fullPath))
                return;

            // Set wallpaper with Windows API
            // This works on Windows 7, 8, 8.1, 10, and 11
            var result = SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, fullPath, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
            
            // Result of 0 typically indicates failure, but we don't throw exceptions
            // as wallpaper setting can fail for various reasons (permissions, file locks, etc.)
            if (result == 0)
            {
                System.Diagnostics.Debug.WriteLine($"Warning: Failed to set wallpaper. Path: {fullPath}");
            }
        }
        catch (UnauthorizedAccessException)
        {
            // User may not have permission to change wallpaper (group policy, etc.)
            System.Diagnostics.Debug.WriteLine("Warning: Access denied when setting wallpaper. Check permissions or group policy.");
        }
        catch (FileNotFoundException)
        {
            // File was deleted or moved between check and setting
            System.Diagnostics.Debug.WriteLine($"Warning: Wallpaper file not found: {path}");
        }
        catch (Exception ex)
        {
            // Log other errors but don't crash
            System.Diagnostics.Debug.WriteLine($"Warning: Error setting wallpaper: {ex.Message}");
        }
    }

    /// <summary>
    /// Disposes resources used by the playlist worker.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        Stop();
        _performanceMonitor.Dispose();
        _isDisposed = true;
    }
}

