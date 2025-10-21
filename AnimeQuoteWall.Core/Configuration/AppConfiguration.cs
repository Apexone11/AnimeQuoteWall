using System.Text.Json;

namespace AnimeQuoteWall.Core.Configuration;

/// <summary>
/// User settings that can be customized and persisted.
/// </summary>
public class UserSettings
{
    public string? CustomBackgroundsPath { get; set; }
    public string? CustomQuotesPath { get; set; }
    public bool UseDarkMode { get; set; } = false;
    public string ThemeColor { get; set; } = "#5E35B1"; // Default purple
}

/// <summary>
/// Configuration paths and settings for the AnimeQuoteWall application.
/// </summary>
public class AppConfiguration
{
    private static UserSettings? _userSettings;
    private static readonly string _settingsFilePath;

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
            return !string.IsNullOrWhiteSpace(_userSettings?.CustomBackgroundsPath) && 
                   Directory.Exists(_userSettings.CustomBackgroundsPath)
                ? _userSettings.CustomBackgroundsPath
                : Path.Combine(DefaultBaseDirectory, "backgrounds");
        }
    }

    /// <summary>
    /// Gets the path to the frames directory.
    /// </summary>
    public static string FramesDirectory => Path.Combine(DefaultBaseDirectory, "frames");

    /// <summary>
    /// Gets the path to the quotes JSON file (user-configurable).
    /// </summary>
    public static string QuotesFilePath
    {
        get
        {
            LoadSettings();
            return !string.IsNullOrWhiteSpace(_userSettings?.CustomQuotesPath) && 
                   File.Exists(_userSettings.CustomQuotesPath)
                ? _userSettings.CustomQuotesPath
                : Path.Combine(DefaultBaseDirectory, "quotes.json");
        }
    }

    /// <summary>
    /// Gets the path to the current wallpaper file.
    /// </summary>
    public static string CurrentWallpaperPath => Path.Combine(DefaultBaseDirectory, "current.png");

    /// <summary>
    /// Gets the supported image file extensions.
    /// </summary>
    public static string[] SupportedImageExtensions => new[]
    {
        ".jpg", ".jpeg", ".png", ".bmp", ".gif"
    };

    /// <summary>
    /// Gets or sets the dark mode setting.
    /// </summary>
    public static bool IsDarkMode
    {
        get
        {
            LoadSettings();
            return _userSettings?.UseDarkMode ?? false;
        }
        set
        {
            LoadSettings();
            if (_userSettings != null)
            {
                _userSettings.UseDarkMode = value;
                SaveSettings();
            }
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
    /// Resets custom paths to defaults.
    /// </summary>
    public static void ResetToDefaults()
    {
        LoadSettings();
        if (_userSettings != null)
        {
            _userSettings.CustomBackgroundsPath = null;
            _userSettings.CustomQuotesPath = null;
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
    /// Loads user settings from disk.
    /// </summary>
    private static void LoadSettings()
    {
        if (_userSettings != null)
            return;

        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = File.ReadAllText(_settingsFilePath);
                _userSettings = JsonSerializer.Deserialize<UserSettings>(json) ?? new UserSettings();
            }
            else
            {
                _userSettings = new UserSettings();
                SaveSettings();
            }
        }
        catch
        {
            _userSettings = new UserSettings();
        }
    }

    /// <summary>
    /// Saves user settings to disk.
    /// </summary>
    private static void SaveSettings()
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
            File.WriteAllText(_settingsFilePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
        }
    }

    /// <summary>
    /// Ensures all required directories exist.
    /// </summary>
    public static void EnsureDirectories()
    {
        Directory.CreateDirectory(DefaultBaseDirectory);
        Directory.CreateDirectory(BackgroundsDirectory);
        Directory.CreateDirectory(FramesDirectory);
    }
}