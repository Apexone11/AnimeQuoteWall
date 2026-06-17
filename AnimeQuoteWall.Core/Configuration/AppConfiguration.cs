using System.Text.Json;
using Microsoft.Win32;

namespace AnimeQuoteWall.Core.Configuration;

/// <summary>
/// User settings that can be customized and persisted.
/// </summary>
public class UserSettings
{
    public string? CustomBackgroundsPath { get; set; } // Custom background path
    public string? CustomQuotesPath { get; set; } // Custom quotes path
    public bool UseDarkMode { get; set; } = false; // Light or dark mode
    public string ThemeColor { get; set; } = "#5E35B1"; // Default purple

    public string ThemeFont { get; set; } = "System"; // System font or a custom font

    public string? CustomOutputPath { get; set; } // Custom output path for the wallpaper
    public string ThemeMode { get; set; } = "System"; // "System" | "Light" | "Dark"

    // Animation defaults
    public int AnimationFps { get; set; } = 24;
    public int AnimationDurationSec { get; set; } = 6;
    public string? LastExportDirectory { get; set; }
    public string? FfmpegPath { get; set; }

    // UI and behavior settings
    public bool AutoRefreshPreview { get; set; } = true; // Auto-refresh preview after generation
    public bool ShowGenerationNotifications { get; set; } = true; // Show notifications on generation
    public bool AutoSaveToHistory { get; set; } = true; // Automatically save to history
    public bool MinimizeToTray { get; set; } = true; // Hide to the system tray (notification area) when minimized

    // Window placement (nullable so an unset value serializes cleanly; restored on next launch)
    public double? WindowLeft { get; set; }
    public double? WindowTop { get; set; }
    public double? WindowWidth { get; set; }
    public double? WindowHeight { get; set; }
    public bool WindowMaximized { get; set; }

    // Background fit mode for generated wallpapers: Fill | Fit | Stretch | Center
    public string WallpaperFillMode { get; set; } = "Fill";

    // Playlist settings
    public string? ActivePlaylistId { get; set; } // ID of the currently active playlist

    // Performance settings
    public bool AutoPauseOnFullscreen { get; set; } = true; // Automatically pause playlists when fullscreen apps are running
    public bool PauseOnBattery { get; set; } = false; // Pause animation/rotation when the device is on battery power
    public bool PauseOnMaximizedWindow { get; set; } = false; // Pause when a foreground window covers most of the screen
    public int MaximizedCoverageThresholdPercent { get; set; } = 95; // Screen-coverage percent that counts as "maximized"
    public bool PauseOnRemoteDesktop { get; set; } = true; // Pause during Remote Desktop sessions to save bandwidth/CPU
    public List<string> PerAppPauseProcesses { get; set; } = new(); // Foreground process names (no extension) that force a pause
    public bool LowPowerMode { get; set; } = false; // One-click low-power profile: caps FPS, disables heavy effects, enables all pause rules
    public int AnimationFpsCap { get; set; } = 30; // Cap for in-app/desktop animation framerate
    public int RenderScalePercent { get; set; } = 100; // Downscale render resolution (50-100) to bound GPU/CPU use

    // Multi-monitor settings
    public string MultiMonitorMode { get; set; } = "Primary"; // "Primary", "All", "Span"
    public List<int> EnabledMonitorIndices { get; set; } = new(); // List of monitor indices to use (empty = all)

    // Per-monitor wallpaper paths (monitor index -> wallpaper path)
    public Dictionary<int, string> PerMonitorWallpaperPaths { get; set; } = new();

    // Feature flags
    public bool EnableAnimatedApply { get; set; } = true;
    public bool EnablePerMonitorApply { get; set; } = true;
}

/// <summary>
/// Configuration paths and settings for the AnimeQuoteWall application.
/// </summary>
public class AppConfiguration
{
    private static UserSettings? _userSettings;
    private static readonly string _settingsFilePath;

    // Guards lazy load and serializes saves so the UI thread and the background PlaylistWorker
    // cannot interleave writes to settings.json (Monitor is re-entrant, so LoadSettings may
    // call SaveSettings while holding the lock).
    private static readonly object _settingsLock = new();

    static AppConfiguration()
    {
        _settingsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AnimeQuotes",
            "settings.json");
    }

    /// <summary>
    /// Gets the base directory for application data (default location).
    /// </summary>
    public static string DefaultBaseDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AnimeQuotes");

    /// <summary>
    /// Gets the active base directory (respects user settings).
    /// </summary>
    public static string BaseDirectory => DefaultBaseDirectory;

    /// <summary>
    /// Gets the path to the backgrounds directory (user-configurable).
    /// </summary>
    public static string BackgroundsDirectory
    {
        get
        {
            LoadSettings();
            // Return the configured custom path if set, regardless of current existence.
            // Directory creation is handled by EnsureDirectories().
            if (!string.IsNullOrWhiteSpace(_userSettings?.CustomBackgroundsPath))
            {
                return _userSettings.CustomBackgroundsPath;
            }
            return Path.Combine(DefaultBaseDirectory, "backgrounds");
        }
    }

    /// <summary>
    /// Gets the path to the frames directory.
    /// </summary>
    public static string FramesDirectory => Path.Combine(DefaultBaseDirectory, "frames");

    /// <summary>
    /// Gets the directory for wallpaper history.
    /// </summary>
    public static string HistoryDirectory => Path.Combine(DefaultBaseDirectory, "history");

    /// <summary>
    /// Gets the directory for playlists.
    /// </summary>
    public static string PlaylistsDirectory => Path.Combine(DefaultBaseDirectory, "playlists");

    /// <summary>
    /// Gets the path to the quotes JSON file (user-configurable).
    /// </summary>
    public static string QuotesFilePath
    {
        get
        {
            LoadSettings();
            // Return the configured custom file path if set, regardless of current existence.
            // File creation is handled by QuoteService.EnsureQuotesFileAsync.
            if (!string.IsNullOrWhiteSpace(_userSettings?.CustomQuotesPath))
            {
                return _userSettings.CustomQuotesPath;
            }
            return Path.Combine(DefaultBaseDirectory, "quotes.json");
        }
    }

    /// <summary>
    /// Gets the path to the current wallpaper file.
    /// </summary>
    public static string CurrentWallpaperPath
    {
        get
        {
            LoadSettings();

            var defaultPath = Path.Combine(DefaultBaseDirectory, "current.png"); // Default output path for the wallpaper
            var custom = _userSettings?.CustomOutputPath; // Custom output path for the wallpaper

            if (!string.IsNullOrWhiteSpace(custom)) // Check if custom output path is set
            {
                var fullPath = Path.GetFullPath(custom); // Prevent path traversal attacks

                // Validate the path is safe
                if (!IsPathSafe(fullPath))
                    return defaultPath;

                // If it looks like a directory (exists or no extension), use current.png inside it
                var hasExtension = Path.HasExtension(fullPath);
                if (!hasExtension || Directory.Exists(fullPath))
                {
                    return Path.Combine(fullPath, "current.png");
                }

                //otherwise, treat it as a file and return it
                return fullPath;
            }

            return defaultPath;
        }
    }

    /// <summary>
    /// Gets the path to the previous wallpaper file.
    /// </summary>
    public static string PreviousWallpaperPath
    {
        get
        {
            LoadSettings();

            var defaultPath = Path.Combine(DefaultBaseDirectory, "previous.png");
            var custom = _userSettings?.CustomOutputPath;

            if (!string.IsNullOrWhiteSpace(custom))
            {
                var fullPath = Path.GetFullPath(custom);

                if (!IsPathSafe(fullPath))
                    return defaultPath;

                var hasExtension = Path.HasExtension(fullPath);
                if (!hasExtension || Directory.Exists(fullPath))
                {
                    return Path.Combine(fullPath, "previous.png");
                }

                var dir = Path.GetDirectoryName(fullPath);
                var fileName = Path.GetFileNameWithoutExtension(fullPath) + "_previous" + Path.GetExtension(fullPath);
                return Path.Combine(dir ?? DefaultBaseDirectory, fileName);
            }

            return defaultPath;
        }
    }

    /// <summary>
    /// Gets the supported image file extensions.
    /// </summary>
    public static string[] SupportedImageExtensions => new[]
    {
        ".jpg", ".jpeg", ".png", ".bmp", ".gif"
    };

    /// <summary>
    /// Theme mode persisted setting. Allowed values: "System" | "Light" | "Dark".
    /// </summary>
    public static string ThemeMode
    {
        get
        {
            LoadSettings();
            return _userSettings?.ThemeMode ?? "System";
        }
        set
        {
            LoadSettings();
            if (_userSettings != null)
            {
                _userSettings.ThemeMode = string.IsNullOrWhiteSpace(value) ? "System" : value;
                SaveSettings();
            }
        }
    }

    // Animation defaults and export settings
    public static int AnimationFps
    {
        get { LoadSettings(); return _userSettings?.AnimationFps ?? 24; }
        set { LoadSettings(); if (_userSettings != null) { _userSettings.AnimationFps = value; SaveSettings(); } }
    }

    public static int AnimationDurationSec
    {
        get { LoadSettings(); return _userSettings?.AnimationDurationSec ?? 6; }
        set { LoadSettings(); if (_userSettings != null) { _userSettings.AnimationDurationSec = value; SaveSettings(); } }
    }

    public static string? LastExportDirectory
    {
        get { LoadSettings(); return _userSettings?.LastExportDirectory; }
        set { LoadSettings(); if (_userSettings != null) { _userSettings.LastExportDirectory = value; SaveSettings(); } }
    }

    public static string? FfmpegPath
    {
        get { LoadSettings(); return _userSettings?.FfmpegPath; }
        set { LoadSettings(); if (_userSettings != null) { _userSettings.FfmpegPath = value; SaveSettings(); } }
    }

    /// <summary>
    /// Gets or sets whether to auto-refresh preview after generation.
    /// </summary>
    public static bool AutoRefreshPreview
    {
        get { LoadSettings(); return _userSettings?.AutoRefreshPreview ?? true; }
        set { LoadSettings(); if (_userSettings != null) { _userSettings.AutoRefreshPreview = value; SaveSettings(); } }
    }

    /// <summary>
    /// Gets or sets whether to show notifications on generation.
    /// </summary>
    public static bool ShowGenerationNotifications
    {
        get { LoadSettings(); return _userSettings?.ShowGenerationNotifications ?? true; }
        set { LoadSettings(); if (_userSettings != null) { _userSettings.ShowGenerationNotifications = value; SaveSettings(); } }
    }

    /// <summary>
    /// Gets or sets whether to automatically save to history.
    /// </summary>
    public static bool AutoSaveToHistory
    {
        get { LoadSettings(); return _userSettings?.AutoSaveToHistory ?? true; }
        set { LoadSettings(); if (_userSettings != null) { _userSettings.AutoSaveToHistory = value; SaveSettings(); } }
    }

    /// <summary>Gets or sets whether the window hides to the system tray when minimized.</summary>
    public static bool MinimizeToTray
    {
        get { LoadSettings(); return _userSettings?.MinimizeToTray ?? true; }
        set { LoadSettings(); if (_userSettings != null && _userSettings.MinimizeToTray != value) { _userSettings.MinimizeToTray = value; SaveSettings(); } }
    }

    /// <summary>Persists the main window's normal-state placement so it can be restored next launch.</summary>
    public static void SaveWindowPlacement(double left, double top, double width, double height, bool maximized)
    {
        LoadSettings();
        if (_userSettings == null) return;
        _userSettings.WindowLeft = left;
        _userSettings.WindowTop = top;
        _userSettings.WindowWidth = width;
        _userSettings.WindowHeight = height;
        _userSettings.WindowMaximized = maximized;
        SaveSettings();
    }

    /// <summary>
    /// Returns the saved window placement. Returns false (and zeroed outputs) when no valid
    /// placement has been stored yet, so the caller can fall back to the default position.
    /// </summary>
    public static bool TryGetWindowPlacement(out double left, out double top, out double width, out double height, out bool maximized)
    {
        LoadSettings();
        maximized = _userSettings?.WindowMaximized ?? false;
        if (_userSettings?.WindowLeft is double l && _userSettings.WindowTop is double t
            && _userSettings.WindowWidth is double w && _userSettings.WindowHeight is double h
            && w > 0 && h > 0)
        {
            left = l; top = t; width = w; height = h;
            return true;
        }
        left = top = width = height = 0;
        return false;
    }

    /// <summary>
    /// Gets or sets how background images are fit to the wallpaper canvas
    /// ("Fill", "Fit", "Stretch", or "Center"). Defaults to "Fill".
    /// </summary>
    public static string WallpaperFillMode
    {
        get { LoadSettings(); var v = _userSettings?.WallpaperFillMode; return string.IsNullOrWhiteSpace(v) ? "Fill" : v; }
        set { LoadSettings(); if (_userSettings != null) { _userSettings.WallpaperFillMode = string.IsNullOrWhiteSpace(value) ? "Fill" : value; SaveSettings(); } }
    }

    /// <summary>
    /// Gets or sets the dark mode setting.
    /// </summary>
    public static bool IsDarkMode
    {
        get
        {
            // Back-compat for older callers: map to ThemeMode
            var mode = ThemeMode;
            return mode.Equals("Dark", StringComparison.OrdinalIgnoreCase)
                || (mode.Equals("System", StringComparison.OrdinalIgnoreCase) && GetSystemThemeIsDark());
        }
        set
        {
            // Back-compat: setting IsDarkMode flips ThemeMode to Light/Dark explicitly
            ThemeMode = value ? "Dark" : "Light";
        }
    }

    /// <summary>
    /// Gets or sets the theme color.
    /// </summary>
    public static string ThemeColor
    {
        get
        {
            LoadSettings();
            return _userSettings?.ThemeColor ?? "#5E35B1";
        }
        set
        {
            LoadSettings();
            if (_userSettings != null)
            {
                _userSettings.ThemeColor = value;
                SaveSettings();
            }
        }
    }

    /// <summary>
    /// Returns effective dark mode based on ThemeMode and system theme when applicable.
    /// </summary>
    public static bool GetEffectiveIsDark()
    {
        var mode = ThemeMode;
        return mode.Equals("Dark", StringComparison.OrdinalIgnoreCase)
            || (mode.Equals("System", StringComparison.OrdinalIgnoreCase) && GetSystemThemeIsDark());
    }

    /// <summary>
    /// Reads the Windows AppsUseLightTheme setting. Returns true when system theme is dark.
    /// Includes compatibility checks for older Windows versions.
    /// </summary>
    private static bool GetSystemThemeIsDark()
    {
        try
        {
            // Check if registry theme detection is supported on this Windows version
            // Windows 10+ has AppsUseLightTheme, older versions don't
            var version = Environment.OSVersion.Version;
            if (version.Major < 10)
            {
                // Windows 7/8/8.1: Default to light theme (no dark mode support)
                return false;
            }

            using var key = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize");
            if (key == null) return false;

            var valueObj = key.GetValue("AppsUseLightTheme");
            if (valueObj == null) return false;

            if (valueObj is int i) return i == 0; // 0 = dark, 1 = light
            if (valueObj is long l) return l == 0;
            return false;
        }
        catch
        {
            // If registry access fails (permissions, etc.), default to light theme
            return false;
        }
    }

    /// <summary>
    /// Sets a custom backgrounds directory path.
    /// </summary>
    public static void SetCustomBackgroundsPath(string path)
    {
        LoadSettings();
        if (_userSettings != null)
        {
            // Sanitize path to prevent directory traversal attacks
            var fullPath = Path.GetFullPath(path);
            if (IsPathSafe(fullPath))
            {
                _userSettings.CustomBackgroundsPath = fullPath;
                Directory.CreateDirectory(fullPath);
                SaveSettings();
            }
            else
            {
                throw new UnauthorizedAccessException("Invalid or unsafe path specified.");
            }
        }
    }

    /// <summary>
    /// Sets a custom quotes file path.
    /// </summary>
    public static void SetCustomQuotesPath(string path)
    {
        LoadSettings();
        if (_userSettings != null)
        {
            // Sanitize path to prevent directory traversal attacks
            var fullPath = Path.GetFullPath(path);
            if (IsPathSafe(fullPath))
            {
                _userSettings.CustomQuotesPath = fullPath;
                var directory = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                SaveSettings();
            }
            else
            {
                throw new UnauthorizedAccessException("Invalid or unsafe path specified.");
            }
        }
    }

    /// <summary>
    /// Sets a custom output file or directory path for the generated wallpaper.
    /// Pass null or empty to clear and use the default path.
    /// </summary>
    public static void SetCustomOutputPath(string? path)
    {
        LoadSettings();
        if (_userSettings != null)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                _userSettings.CustomOutputPath = null;
                SaveSettings();
                return;
            }

            var fullPath = Path.GetFullPath(path);
            if (!IsPathSafe(fullPath))
            {
                throw new UnauthorizedAccessException("Invalid or unsafe path specified.");
            }

            // Ensure directory exists depending on path type
            if (Path.HasExtension(fullPath))
            {
                var dir = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrWhiteSpace(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }
            else
            {
                Directory.CreateDirectory(fullPath);
            }

            _userSettings.CustomOutputPath = fullPath;
            SaveSettings();
        }
    }

    /// <summary>
    /// Resets custom paths to defaults.
    /// </summary>
    public static void ResetToDefaults()
    {
        LoadSettings();
        if (_userSettings != null)
        {
            _userSettings.CustomBackgroundsPath = null;
            _userSettings.CustomQuotesPath = null;
            _userSettings.CustomOutputPath = null;
            SaveSettings();
        }
    }

    /// <summary>
    /// Validates that a path is safe (prevents path traversal attacks).
    /// </summary>
    private static bool IsPathSafe(string path)
    {
        try
        {
            // Check for invalid characters
            if (path.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                return false;

            // Ensure path doesn't try to escape using relative paths
            var fullPath = Path.GetFullPath(path);

            // Don't allow system directories
            var systemPath = Environment.GetFolderPath(Environment.SpecialFolder.System);
            var windowsPath = Environment.GetFolderPath(Environment.SpecialFolder.Windows);

            if (fullPath.StartsWith(systemPath, StringComparison.OrdinalIgnoreCase) ||
                fullPath.StartsWith(windowsPath, StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Loads user settings from disk. Thread-safe: double-checked under the settings lock so the
    /// UI thread and the background worker cannot both initialize, or read a half-populated object.
    /// </summary>
    private static void LoadSettings()
    {
        if (_userSettings != null)
            return;
        lock (_settingsLock)
        {
            if (_userSettings == null)
                LoadSettingsCore();
        }
    }

    private static void LoadSettingsCore()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = File.ReadAllText(_settingsFilePath);
                _userSettings = JsonSerializer.Deserialize<UserSettings>(json) ?? new UserSettings();

                // Backward compatibility migration:
                // If ThemeMode property was missing in the existing settings file,
                // migrate from legacy UseDarkMode to ThemeMode and persist.
                try
                {
                    using var doc = JsonDocument.Parse(json);
                    var hasThemeMode = doc.RootElement.ValueKind == JsonValueKind.Object
                        && doc.RootElement.TryGetProperty("ThemeMode", out _);

                    if (!hasThemeMode && _userSettings != null)
                    {
                        _userSettings.ThemeMode = _userSettings.UseDarkMode ? "Dark" : "Light";
                        SaveSettings();
                    }
                }
                catch (Exception migrateEx)
                {
                    System.Diagnostics.Debug.WriteLine($"AppConfiguration migration skipped: {migrateEx.Message}");
                }
            }
            else
            {
                _userSettings = new UserSettings();
                SaveSettings();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AppConfiguration.LoadSettings: {ex.Message}");
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    var corruptBackup = _settingsFilePath + ".corrupt-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                    File.Copy(_settingsFilePath, corruptBackup, overwrite: true);
                    System.Diagnostics.Debug.WriteLine($"AppConfiguration: settings file backed up to {corruptBackup}");
                }
            }
            catch (Exception backupEx)
            {
                System.Diagnostics.Debug.WriteLine($"AppConfiguration backup failed: {backupEx.Message}");
            }
            _userSettings = new UserSettings();
        }
    }

    /// <summary>
    /// Saves user settings to disk.
    /// </summary>
    private static void SaveSettings()
    {
        // Serialize concurrent saves and write atomically (temp file + move) so a concurrent
        // reader can never observe a half-written settings.json.
        lock (_settingsLock)
        {
            try
            {
                var directory = Path.GetDirectoryName(_settingsFilePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(_userSettings, options);

                var tempPath = _settingsFilePath + ".tmp";
                File.WriteAllText(tempPath, json);
                File.Move(tempPath, _settingsFilePath, overwrite: true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Gets or sets the active playlist ID.
    /// </summary>
    public static string? ActivePlaylistId
    {
        get { LoadSettings(); return _userSettings?.ActivePlaylistId; }
        set { LoadSettings(); if (_userSettings != null) { _userSettings.ActivePlaylistId = value; SaveSettings(); } }
    }

    /// <summary>
    /// Gets or sets whether to auto-pause playlists when fullscreen applications are running.
    /// </summary>
    public static bool AutoPauseOnFullscreen
    {
        get { LoadSettings(); return _userSettings?.AutoPauseOnFullscreen ?? true; }
        set { LoadSettings(); if (_userSettings != null && _userSettings.AutoPauseOnFullscreen != value) { _userSettings.AutoPauseOnFullscreen = value; SaveSettings(); } }
    }

    /// <summary>Gets or sets whether to pause animation/rotation while the device is on battery power.</summary>
    public static bool PauseOnBattery
    {
        get { LoadSettings(); return _userSettings?.PauseOnBattery ?? false; }
        set { LoadSettings(); if (_userSettings != null && _userSettings.PauseOnBattery != value) { _userSettings.PauseOnBattery = value; SaveSettings(); } }
    }

    /// <summary>Gets or sets whether to pause when a foreground window covers most of the screen.</summary>
    public static bool PauseOnMaximizedWindow
    {
        get { LoadSettings(); return _userSettings?.PauseOnMaximizedWindow ?? false; }
        set { LoadSettings(); if (_userSettings != null && _userSettings.PauseOnMaximizedWindow != value) { _userSettings.PauseOnMaximizedWindow = value; SaveSettings(); } }
    }

    /// <summary>Gets or sets the screen-coverage percent (50-100) that counts as a maximized window.</summary>
    public static int MaximizedCoverageThresholdPercent
    {
        get { LoadSettings(); return Math.Clamp(_userSettings?.MaximizedCoverageThresholdPercent ?? 95, 50, 100); }
        set { LoadSettings(); if (_userSettings != null) { _userSettings.MaximizedCoverageThresholdPercent = Math.Clamp(value, 50, 100); SaveSettings(); } }
    }

    /// <summary>Gets or sets whether to pause during Remote Desktop sessions to save bandwidth and CPU.</summary>
    public static bool PauseOnRemoteDesktop
    {
        get { LoadSettings(); return _userSettings?.PauseOnRemoteDesktop ?? true; }
        set { LoadSettings(); if (_userSettings != null && _userSettings.PauseOnRemoteDesktop != value) { _userSettings.PauseOnRemoteDesktop = value; SaveSettings(); } }
    }

    /// <summary>Gets or sets the foreground process names (without extension) that force a pause.</summary>
    public static List<string> PerAppPauseProcesses
    {
        get { LoadSettings(); return _userSettings?.PerAppPauseProcesses ?? new List<string>(); }
        set { LoadSettings(); if (_userSettings != null) { _userSettings.PerAppPauseProcesses = value ?? new List<string>(); SaveSettings(); } }
    }

    /// <summary>
    /// Gets or sets the one-click low-power profile. When enabled, the pause policy treats
    /// battery/maximized/RDP rules as active and the effective FPS cap is lowered, regardless
    /// of the individual toggles. See <see cref="EffectiveAnimationFpsCap"/>.
    /// </summary>
    public static bool LowPowerMode
    {
        get { LoadSettings(); return _userSettings?.LowPowerMode ?? false; }
        set { LoadSettings(); if (_userSettings != null && _userSettings.LowPowerMode != value) { _userSettings.LowPowerMode = value; SaveSettings(); } }
    }

    /// <summary>Gets or sets the animation framerate cap (5-60 fps).</summary>
    public static int AnimationFpsCap
    {
        get { LoadSettings(); return Math.Clamp(_userSettings?.AnimationFpsCap ?? 30, 5, 60); }
        set { LoadSettings(); if (_userSettings != null) { _userSettings.AnimationFpsCap = Math.Clamp(value, 5, 60); SaveSettings(); } }
    }

    /// <summary>Gets or sets the render-scale percent (50-100); lower values reduce GPU/CPU cost.</summary>
    public static int RenderScalePercent
    {
        get { LoadSettings(); return Math.Clamp(_userSettings?.RenderScalePercent ?? 100, 50, 100); }
        set { LoadSettings(); if (_userSettings != null) { _userSettings.RenderScalePercent = Math.Clamp(value, 50, 100); SaveSettings(); } }
    }

    /// <summary>
    /// The effective animation FPS cap after applying Low-Power mode. In Low-Power mode the
    /// cap is the smaller of the user's cap and 20 fps, to bound battery and CPU use.
    /// </summary>
    public static int EffectiveAnimationFpsCap => LowPowerMode ? Math.Min(AnimationFpsCap, 20) : AnimationFpsCap;

    /// <summary>
    /// Gets or sets the multi-monitor mode.
    /// Valid values: "Primary", "All", "Span"
    /// </summary>
    public static string MultiMonitorMode
    {
        get { LoadSettings(); return _userSettings?.MultiMonitorMode ?? "Primary"; }
        set { LoadSettings(); if (_userSettings != null) { _userSettings.MultiMonitorMode = value; SaveSettings(); } }
    }

    /// <summary>
    /// Gets or sets the list of enabled monitor indices.
    /// </summary>
    public static List<int> EnabledMonitorIndices
    {
        get { LoadSettings(); return _userSettings?.EnabledMonitorIndices ?? new List<int>(); }
        set { LoadSettings(); if (_userSettings != null) { _userSettings.EnabledMonitorIndices = value; SaveSettings(); } }
    }

    /// <summary>
    /// Gets or sets whether animated wallpaper apply is enabled.
    /// </summary>
    public static bool EnableAnimatedApply
    {
        get { LoadSettings(); return _userSettings?.EnableAnimatedApply ?? true; }
        set { LoadSettings(); if (_userSettings != null) { _userSettings.EnableAnimatedApply = value; SaveSettings(); } }
    }

    /// <summary>
    /// Gets or sets whether per-monitor wallpaper apply is enabled.
    /// </summary>
    public static bool EnablePerMonitorApply
    {
        get { LoadSettings(); return _userSettings?.EnablePerMonitorApply ?? true; }
        set { LoadSettings(); if (_userSettings != null) { _userSettings.EnablePerMonitorApply = value; SaveSettings(); } }
    }

    /// <summary>
    /// Gets the wallpaper path for a specific monitor.
    /// </summary>
    /// <param name="monitorIndex">Monitor index (0-based)</param>
    /// <returns>Path to the wallpaper file for that monitor, or null if not set</returns>
    public static string? GetMonitorWallpaperPath(int monitorIndex)
    {
        LoadSettings();
        lock (_settingsLock)
        {
            if (_userSettings?.PerMonitorWallpaperPaths != null
                && _userSettings.PerMonitorWallpaperPaths.TryGetValue(monitorIndex, out var path))
            {
                return path;
            }
            return null;
        }
    }

    /// <summary>
    /// Sets the wallpaper path for a specific monitor.
    /// </summary>
    /// <param name="monitorIndex">Monitor index (0-based)</param>
    /// <param name="wallpaperPath">Path to the wallpaper file</param>
    public static void SetMonitorWallpaperPath(int monitorIndex, string wallpaperPath)
    {
        LoadSettings();
        lock (_settingsLock)
        {
            if (_userSettings == null) return;
            _userSettings.PerMonitorWallpaperPaths ??= new Dictionary<int, string>();

            // If wallpaperPath is empty or null, remove the key instead of storing empty string
            if (string.IsNullOrWhiteSpace(wallpaperPath))
                _userSettings.PerMonitorWallpaperPaths.Remove(monitorIndex);
            else
                _userSettings.PerMonitorWallpaperPaths[monitorIndex] = wallpaperPath;

            SaveSettings();
        }
    }

    /// <summary>
    /// Clears the wallpaper path for a specific monitor.
    /// </summary>
    /// <param name="monitorIndex">Monitor index (0-based)</param>
    public static void ClearMonitorWallpaperPath(int monitorIndex)
    {
        LoadSettings();
        lock (_settingsLock)
        {
            if (_userSettings?.PerMonitorWallpaperPaths != null)
            {
                _userSettings.PerMonitorWallpaperPaths.Remove(monitorIndex);
                SaveSettings();
            }
        }
    }

    /// <summary>
    /// Gets all per-monitor wallpaper paths.
    /// </summary>
    /// <returns>Dictionary mapping monitor indices to wallpaper paths</returns>
    public static Dictionary<int, string> GetAllMonitorWallpaperPaths()
    {
        LoadSettings();
        lock (_settingsLock)
        {
            // Return a copy, not the live dictionary, so callers cannot mutate shared state, and
            // take the snapshot under the lock so no other thread mutates mid-enumeration.
            return _userSettings?.PerMonitorWallpaperPaths is { } paths
                ? new Dictionary<int, string>(paths)
                : new Dictionary<int, string>();
        }
    }

    /// <summary>
    /// Gets the path for a monitor-specific wallpaper file.
    /// </summary>
    /// <param name="monitorIndex">Monitor index (0-based)</param>
    /// <returns>Path to store the wallpaper file for that monitor</returns>
    public static string GetMonitorWallpaperFilePath(int monitorIndex)
    {
        LoadSettings();
        var baseDir = DefaultBaseDirectory;
        var custom = _userSettings?.CustomOutputPath;

        if (!string.IsNullOrWhiteSpace(custom))
        {
            var fullPath = Path.GetFullPath(custom);
            if (IsPathSafe(fullPath))
            {
                var hasExtension = Path.HasExtension(fullPath);
                if (!hasExtension || Directory.Exists(fullPath))
                {
                    return Path.Combine(fullPath, $"monitor_{monitorIndex}.png");
                }
                var dir = Path.GetDirectoryName(fullPath);
                return Path.Combine(dir ?? DefaultBaseDirectory, $"monitor_{monitorIndex}.png");
            }
        }

        return Path.Combine(baseDir, $"monitor_{monitorIndex}.png");
    }

    /// <summary>
    /// Gets the path for a monitor-specific previous wallpaper file.
    /// </summary>
    /// <param name="monitorIndex">Monitor index (0-based)</param>
    /// <returns>Path to store the previous wallpaper file for that monitor</returns>
    public static string GetMonitorPreviousWallpaperFilePath(int monitorIndex)
    {
        LoadSettings();
        var baseDir = DefaultBaseDirectory;
        var custom = _userSettings?.CustomOutputPath;

        if (!string.IsNullOrWhiteSpace(custom))
        {
            var fullPath = Path.GetFullPath(custom);
            if (IsPathSafe(fullPath))
            {
                var hasExtension = Path.HasExtension(fullPath);
                if (!hasExtension || Directory.Exists(fullPath))
                {
                    return Path.Combine(fullPath, $"monitor_{monitorIndex}_previous.png");
                }
                var dir = Path.GetDirectoryName(fullPath);
                return Path.Combine(dir ?? DefaultBaseDirectory, $"monitor_{monitorIndex}_previous.png");
            }
        }

        return Path.Combine(baseDir, $"monitor_{monitorIndex}_previous.png");
    }

    /// <summary>
    /// Ensures all required directories exist.
    /// </summary>
    public static void EnsureDirectories()
    {
        Directory.CreateDirectory(DefaultBaseDirectory);
        Directory.CreateDirectory(BackgroundsDirectory);
        Directory.CreateDirectory(FramesDirectory);
        Directory.CreateDirectory(PlaylistsDirectory);

        // Ensure the output directory exists for the current wallpaper path
        var outputDir = Path.GetDirectoryName(CurrentWallpaperPath);
        if (!string.IsNullOrWhiteSpace(outputDir) && !Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }
    }
}
