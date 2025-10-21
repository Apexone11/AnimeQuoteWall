namespace AnimeQuoteWall.Core.Models;

/// <summary>
/// Configuration settings for wallpaper generation.
/// 
/// FOR BEGINNERS: This class contains all the settings that control how your wallpaper looks.
/// You can change these values to customize:
/// - Size (width/height)
/// - Colors (background, text, panels)
/// - Fonts
/// - Animation settings
/// 
/// All properties have sensible defaults, so you don't have to set them unless you want to customize.
/// </summary>
public class WallpaperSettings
{
    private int _width = 2560;
    private int _height = 1440;
    private float _panelOpacity = 0.85f;
    private int _animationFrames = 16;

    /// <summary>
    /// Wallpaper width in pixels. Must be between 320 and 7680.
    /// Default: 2560 (standard for 1440p monitors)
    /// </summary>
    public int Width 
    { 
        get => _width;
        set
        {
            if (value < 320 || value > 7680)
                throw new ArgumentException("Width must be between 320 and 7680 pixels", nameof(Width));
            _width = value;
        }
    }

    /// <summary>
    /// Wallpaper height in pixels. Must be between 240 and 4320.
    /// Default: 1440 (standard for 1440p monitors)
    /// </summary>
    public int Height 
    { 
        get => _height;
        set
        {
            if (value < 240 || value > 4320)
                throw new ArgumentException("Height must be between 240 and 4320 pixels", nameof(Height));
            _height = value;
        }
    }

    /// <summary>
    /// Background color when no image is available (hex color like "#2C3E50").
    /// Default: Dark blue-gray
    /// </summary>
    public string BackgroundColor { get; set; } = "#2C3E50";

    /// <summary>
    /// Font to use for the quote text.
    /// Default: "Comic Sans MS" (or you can try "Arial", "Georgia", etc.)
    /// </summary>
    public string FontFamily { get; set; } = "Comic Sans MS";

    /// <summary>
    /// Backup fonts if the main font isn't available on your system.
    /// The program will try each one in order until it finds one that works.
    /// </summary>
    public string[] FallbackFonts { get; set; } = 
    {
        "Trebuchet MS", "Verdana", "Segoe UI", "Calibri"
    };

    /// <summary>
    /// Color of the quote text (hex color like "#FFFFFF").
    /// Default: White (#FFFFFF)
    /// </summary>
    public string TextColor { get; set; } = "#FFFFFF";

    /// <summary>
    /// Color of the text outline (makes text easier to read).
    /// Default: Black (#000000)
    /// </summary>
    public string OutlineColor { get; set; } = "#000000";

    /// <summary>
    /// Background color for the panel behind the quote.
    /// Default: Dark gray-blue (#34495E)
    /// </summary>
    public string PanelColor { get; set; } = "#34495E";

    /// <summary>
    /// Panel opacity/transparency (0.0 = invisible, 1.0 = fully solid).
    /// Default: 0.85 (slightly transparent)
    /// </summary>
    public float PanelOpacity 
    { 
        get => _panelOpacity;
        set
        {
            if (value < 0.0f || value > 1.0f)
                throw new ArgumentException("PanelOpacity must be between 0.0 and 1.0", nameof(PanelOpacity));
            _panelOpacity = value;
        }
    }

    /// <summary>
    /// Number of animation frames to generate (more = smoother but slower).
    /// Default: 16 frames
    /// </summary>
    public int AnimationFrames 
    { 
        get => _animationFrames;
        set
        {
            if (value < 1 || value > 120)
                throw new ArgumentException("AnimationFrames must be between 1 and 120", nameof(AnimationFrames));
            _animationFrames = value;
        }
    }

    /// <summary>
    /// Maximum width of the text panel as a percentage (0.65 = 65% of screen width).
    /// This prevents quotes from stretching all the way across the screen.
    /// Default: 0.65 (65%)
    /// </summary>
    public float MaxPanelWidthPercent { get; set; } = 0.65f;

    /// <summary>
    /// Controls font size based on screen resolution (higher = bigger text).
    /// Formula: fontSize = (imageWidth / FontSizeFactor)
    /// Default: 30.0
    /// </summary>
    public float FontSizeFactor { get; set; } = 30.0f;

    /// <summary>
    /// Smallest allowed font size (prevents text from becoming unreadable).
    /// Default: 24 pixels
    /// </summary>
    public float MinFontSize { get; set; } = 24.0f;

    /// <summary>
    /// Validates all settings to make sure they're in acceptable ranges.
    /// Called automatically when creating wallpapers.
    /// </summary>
    /// <returns>True if all settings are valid</returns>
    public bool IsValid()
    {
        return Width > 0 && Height > 0 
            && PanelOpacity >= 0.0f && PanelOpacity <= 1.0f
            && AnimationFrames > 0
            && FontSizeFactor > 0
            && MinFontSize > 0;
    }
}