using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using AnimeQuoteWall.Core.Interfaces;
using AnimeQuoteWall.Core.Models;
using AnimeQuoteWall.Core.Services;
using AnimeQuoteWall.Core.Protection;

namespace AnimeQuoteWall.Core.Services;

/// <summary>
/// Service for generating wallpapers and animation frames.
/// 
/// FOR BEGINNERS: This class handles the actual image creation.
/// It combines backgrounds, text, and styling to make beautiful wallpapers.
/// 
/// Main capabilities:
/// - Create a single wallpaper image
/// - Generate multiple frames for animations
/// - Load and resize background images
/// - Draw text with nice effects (shadows, outlines, etc.)
/// </summary>
/// <summary>
/// Service for generating wallpapers and animation frames.
/// 
/// This service handles the actual image creation by combining backgrounds, text, and styling
/// to create beautiful wallpapers. It uses the ImageCacheService to optimize background loading.
/// 
/// Main capabilities:
/// - Create a single wallpaper image
/// - Generate multiple frames for animations
/// - Load and resize background images (with caching)
/// - Draw text with visual effects (shadows, outlines, etc.)
/// </summary>
public class WallpaperService : IWallpaperService
{
    /// <summary>
    /// Reference to the image cache service for optimized background loading.
    /// </summary>
    private readonly ImageCacheService _imageCache = ImageCacheService.Instance;

    /// <summary>
    /// Reference to the monitor service for multi-monitor support.
    /// </summary>
    private readonly MonitorService _monitorService = new MonitorService();
    
    /// <summary>
    /// Creates a wallpaper image by combining a background with a quote.
    /// 
    /// Process:
    /// 1. Load or create background (uses cache if available)
    /// 2. Create a new bitmap for the wallpaper
    /// 3. Draw the background onto the wallpaper
    /// 4. Draw the quote text on top with styling
    /// 
    /// </summary>
    /// <param name="backgroundPath">Path to background image (or null for solid color)</param>
    /// <param name="quote">The quote to display</param>
    /// <param name="settings">Visual settings (size, colors, fonts, etc.)</param>
    /// <returns>A bitmap image ready to be saved as wallpaper</returns>
    [System.Diagnostics.DebuggerStepThrough]
    public Bitmap CreateWallpaperImage(string? backgroundPath, Quote quote, WallpaperSettings settings)
    {
        // Load the background (either from file or create a solid color one)
        using var background = LoadBackgroundBitmap(backgroundPath, settings);
        
        // Create a new image for our wallpaper
        var wallpaper = new Bitmap(background.Width, background.Height);
        
        // Get a drawing surface to work with
        using var graphics = Graphics.FromImage(wallpaper);
        
        // Configure graphics for high quality rendering
        ConfigureGraphicsQuality(graphics);
        
        // Step 1: Draw the background image
        graphics.DrawImage(background, 0, 0, wallpaper.Width, wallpaper.Height);
        
        // Step 2: Draw the quote on top
        DrawQuote(graphics, quote, wallpaper.Width, wallpaper.Height, settings);
        
        return wallpaper;
    }

    /// <summary>
    /// Helper method: Configures Graphics object for best quality rendering.
    /// This makes text and images look smooth and professional.
    /// </summary>
    /// <param name="graphics">The graphics object to configure</param>
    public static void ConfigureGraphicsQuality(Graphics graphics)
    {
        graphics.SmoothingMode = SmoothingMode.AntiAlias;           // Smooth curves
        graphics.TextRenderingHint = TextRenderingHint.AntiAlias;   // Smooth text
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;     // Better pixel alignment
        graphics.CompositingQuality = CompositingQuality.HighQuality; // Better color blending
    }

    /// <summary>
    /// Generates multiple frames for creating animated wallpapers (GIFs).
    /// Each frame shows the quote in a different position/style.
    /// </summary>
    /// <param name="backgroundPath">Background image path</param>
    /// <param name="quote">Quote to animate</param>
    /// <param name="settings">Visual settings</param>
    /// <param name="outputDirectory">Where to save the frames</param>
    /// <returns>List of paths to all generated frame images</returns>
    public async Task<List<string>> GenerateAnimationFramesAsync(string? backgroundPath, Quote quote, WallpaperSettings settings, string outputDirectory)
    {
        // Create a unique folder for this set of frames (uses current date/time)
        var frameTimestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var frameDir = Path.Combine(outputDirectory, frameTimestamp);
        Directory.CreateDirectory(frameDir);

        var generatedFrames = new List<string>();

        // Load the background once (reuse it for all frames)
        using var background = LoadBackgroundBitmap(backgroundPath, settings);

        // Generate each frame
        for (int frame = 0; frame < settings.AnimationFrames; frame++)
        {
            // Create a new image for this frame
            using var frameBitmap = new Bitmap(background.Width, background.Height);
            using var graphics = Graphics.FromImage(frameBitmap);
            
            // Use our helper method for quality settings
            ConfigureGraphicsQuality(graphics);

            // Draw the background
            graphics.DrawImage(background, 0, 0, frameBitmap.Width, frameBitmap.Height);

            // Calculate how far through the animation we are (0.0 to 1.0)
            // Used for smooth transitions between frames
            var progress = frame / (float)(settings.AnimationFrames - 1);
            
            // Draw the quote with animation effects
            DrawAnimatedQuote(graphics, quote, frameBitmap.Width, frameBitmap.Height, settings, progress);

            // Save this frame as a PNG file
            var framePath = Path.Combine(frameDir, $"frame_{frame:D3}.png");
            frameBitmap.Save(framePath, ImageFormat.Png);
            generatedFrames.Add(framePath);
        }

        return generatedFrames;
    }

    /// <inheritdoc />
    public async Task SaveImageAsync(Bitmap bitmap, string filePath)
    {
        await Task.Run(() => bitmap.Save(filePath, ImageFormat.Png)).ConfigureAwait(false);
    }

    /// <summary>
    /// Loads a background image from file or creates a solid color background.
    /// Uses the image cache for performance optimization.
    /// </summary>
    /// <param name="backgroundPath">Path to background image file, or null/empty for solid color</param>
    /// <param name="settings">Wallpaper settings containing target dimensions and background color</param>
    /// <returns>Bitmap containing the background (loaded from file or solid color)</returns>
    public Bitmap LoadBackgroundBitmap(string? backgroundPath, WallpaperSettings settings)
    {
        // Try to load from file if path is provided
        if (!string.IsNullOrEmpty(backgroundPath) && File.Exists(backgroundPath))
        {
            try
            {
                // Use cache for background loading - this significantly improves performance
                // when the same background is used multiple times
                var cached = _imageCache.GetOrLoadImage(backgroundPath, settings.Width, settings.Height);
                if (cached != null)
                {
                    return cached;
                }
            }
            catch
            {
                // Fall back to solid color if image loading fails
                // (file might be corrupted, wrong format, etc.)
            }
        }

        // Create solid color background as fallback
        // This is used when no background image is provided or loading fails
        var solidBackground = new Bitmap(settings.Width, settings.Height);
        using var graphics = Graphics.FromImage(solidBackground);
        using var brush = new SolidBrush(ColorTranslator.FromHtml(settings.BackgroundColor));
        graphics.FillRectangle(brush, 0, 0, settings.Width, settings.Height);
        
        return solidBackground;
    }

    public void DrawQuote(Graphics graphics, Quote quote, int imageWidth, int imageHeight, WallpaperSettings settings)
    {
        // Protected method - core rendering algorithm
        // Integrity check is performed but doesn't throw to prevent crashes during development
        // In release builds, this can be made stricter
        try
        {
            if (!CodeProtection.ValidateDistributionIntegrity())
            {
                // Log warning but continue (allows development/debugging)
                System.Diagnostics.Debug.WriteLine("Warning: Code integrity check failed");
            }
        }
        catch
        {
            // Fail silently during development
        }
        
        DrawAnimatedQuote(graphics, quote, imageWidth, imageHeight, settings, 0.5f);
    }

    /// <summary>
    /// Draws a quote with interactive effects (mouse parallax and time-based effects).
    /// </summary>
    public void DrawInteractiveQuote(Graphics graphics, Quote quote, int imageWidth, int imageHeight, WallpaperSettings settings, System.Drawing.PointF? parallaxOffset = null, float? timeBasedOpacity = null, System.Drawing.Color? timeBasedColorShift = null)
    {
        // Apply time-based opacity if provided
        var baseOpacity = timeBasedOpacity ?? 1.0f;
        
        // Apply parallax offset if provided
        var parallaxX = parallaxOffset?.X ?? 0f;
        var parallaxY = parallaxOffset?.Y ?? 0f;

        // Calculate font size properly
        var fontSize = Math.Max(imageHeight / settings.FontSizeFactor, settings.MinFontSize);
        var panelWidth = (int)(imageWidth * settings.MaxPanelWidthPercent);
        
        using var font = CreateAnimeFont(fontSize, settings);
        var wrappedLines = WrapText(graphics, quote.Text, font, panelWidth - 80);
        
        // Calculate required panel height
        var lineHeight = font.Height;
        var totalTextHeight = wrappedLines.Count * (lineHeight + 10);
        var characterAnimeHeight = (int)(lineHeight * 0.7f) + 30;
        var panelHeight = totalTextHeight + characterAnimeHeight + 80;

        // Apply parallax to panel position
        var panelX = (imageWidth - panelWidth) / 2 + (int)parallaxX;
        var panelY = (imageHeight - panelHeight) / 2 + (int)parallaxY;

        // Apply time-based color shift if provided
        var textColor = ColorTranslator.FromHtml(settings.TextColor);
        var outlineColor = ColorTranslator.FromHtml(settings.OutlineColor);
        
        if (timeBasedColorShift.HasValue)
        {
            var shift = timeBasedColorShift.Value;
            textColor = Color.FromArgb(
                Math.Min(255, (int)(textColor.R * shift.R / 255f)),
                Math.Min(255, (int)(textColor.G * shift.G / 255f)),
                Math.Min(255, (int)(textColor.B * shift.B / 255f))
            );
        }

        // Apply opacity
        textColor = Color.FromArgb((int)(baseOpacity * 255), textColor);
        outlineColor = Color.FromArgb((int)(baseOpacity * 255), outlineColor);

        // Draw panel with adjusted opacity
        DrawRoundedPanel(graphics, panelX, panelY, panelWidth, panelHeight, settings, 0.3f * baseOpacity);

        // Draw quote text
        var textY = panelY + 40;
        foreach (var line in wrappedLines)
        {
            var textSize = graphics.MeasureString(line, font);
            var textX = panelX + (panelWidth - textSize.Width) / 2;

            // Draw text outline
            for (int dx = -2; dx <= 2; dx++)
            {
                for (int dy = -2; dy <= 2; dy++)
                {
                    if (dx != 0 || dy != 0)
                    {
                        using var outlineBrush = new SolidBrush(outlineColor);
                        graphics.DrawString(line, font, outlineBrush, textX + dx, textY + dy);
                    }
                }
            }

            // Draw main text
            using var textBrush = new SolidBrush(textColor);
            graphics.DrawString(line, font, textBrush, textX, textY);
            textY += lineHeight + 10;
        }

        // Draw character and anime info
        var characterAnimeText = $"— {quote.Character} ({quote.Anime})";
        using var smallerFont = CreateAnimeFont(fontSize * 0.6f, settings);
        var characterAnimeSize = graphics.MeasureString(characterAnimeText, smallerFont);
        var characterAnimeX = panelX + panelWidth - characterAnimeSize.Width - 40;
        var characterAnimeY = panelY + panelHeight - characterAnimeSize.Height - 30;

        // Draw character/anime outline
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx != 0 || dy != 0)
                {
                    using var outlineBrush = new SolidBrush(outlineColor);
                    graphics.DrawString(characterAnimeText, smallerFont, outlineBrush, 
                        characterAnimeX + dx, characterAnimeY + dy);
                }
            }
        }

        // Draw character/anime text
        using var characterAnimeBrush = new SolidBrush(Color.FromArgb((int)(baseOpacity * 230), textColor));
        graphics.DrawString(characterAnimeText, smallerFont, characterAnimeBrush, characterAnimeX, characterAnimeY);
    }

    /// <summary>
    /// Core wallpaper rendering algorithm - PROTECTED CODE.
    /// This method contains proprietary rendering logic and should be obfuscated for release.
    /// </summary>
    [System.Diagnostics.DebuggerStepThrough]
    public void DrawAnimatedQuote(Graphics graphics, Quote quote, int imageWidth, int imageHeight, WallpaperSettings settings, float animationProgress, Models.TextAnimationType textAnimationType = Models.TextAnimationType.None, List<string>? motionEffects = null)
    {
        // Calculate font size properly - divide height by factor
        var fontSize = Math.Max(imageHeight / settings.FontSizeFactor, settings.MinFontSize);
        var panelWidth = (int)(imageWidth * settings.MaxPanelWidthPercent);
        
        using var font = CreateAnimeFont(fontSize, settings);
        var wrappedLines = WrapText(graphics, quote.Text, font, panelWidth - 80);
        
        // Calculate required panel height based on text
        var lineHeight = font.Height;
        var totalTextHeight = wrappedLines.Count * (lineHeight + 10); // Add spacing between lines
        var characterAnimeHeight = (int)(lineHeight * 0.7f) + 30;
        var panelHeight = totalTextHeight + characterAnimeHeight + 80; // Add padding

        // Calculate animated positions (pulse effect)
        var pulseOffset = (float)(Math.Sin(animationProgress * Math.PI * 2) * 10);
        var glowIntensity = 0.3f + 0.2f * (float)Math.Sin(animationProgress * Math.PI * 4);

        // Apply motion effects
        var motionOffsetX = 0f;
        var motionOffsetY = 0f;
        var scale = 1.0f;
        var rotation = 0f;

        if (motionEffects != null)
        {
            foreach (var effect in motionEffects)
            {
                switch (effect.ToLowerInvariant())
                {
                    case "zoom":
                        scale = 0.8f + 0.4f * animationProgress; // Zoom from 0.8 to 1.2
                        break;
                    case "pan":
                        motionOffsetX = (animationProgress - 0.5f) * imageWidth * 0.2f; // Pan left/right
                        break;
                    case "rotation":
                        rotation = animationProgress * 360f; // Rotate 360 degrees
                        break;
                }
            }
        }

        var panelX = (imageWidth - panelWidth) / 2 + (int)pulseOffset + (int)motionOffsetX;
        var panelY = (imageHeight - panelHeight) / 2 + (int)motionOffsetY;

        // Draw panel with glow effect
        DrawRoundedPanel(graphics, panelX, panelY, panelWidth, panelHeight, settings, glowIntensity);

        // Apply transformation for zoom/rotation
        var originalTransform = graphics.Transform;
        if (scale != 1.0f || rotation != 0f)
        {
            graphics.TranslateTransform(imageWidth / 2f, imageHeight / 2f);
            graphics.ScaleTransform(scale, scale);
            graphics.RotateTransform(rotation);
            graphics.TranslateTransform(-imageWidth / 2f, -imageHeight / 2f);
        }

        // Draw quote text with better positioning and text animations
        var textY = panelY + 40;
        var textColor = ColorTranslator.FromHtml(settings.TextColor);
        var outlineColor = ColorTranslator.FromHtml(settings.OutlineColor);

        // Apply text animation effects
        float textOpacity = 1.0f;
        float textOffsetX = 0f;
        float textOffsetY = 0f;
        int visibleChars = int.MaxValue;

        switch (textAnimationType)
        {
            case Models.TextAnimationType.Fade:
                textOpacity = animationProgress;
                break;
            case Models.TextAnimationType.Slide:
                textOffsetX = (1f - animationProgress) * imageWidth * 0.3f; // Slide from right
                break;
            case Models.TextAnimationType.Typewriter:
                var totalChars = string.Join(" ", wrappedLines).Length;
                visibleChars = (int)(totalChars * animationProgress);
                break;
        }

        foreach (var line in wrappedLines)
        {
            var textSize = graphics.MeasureString(line, font);
            var textX = panelX + (panelWidth - textSize.Width) / 2 + textOffsetX;
            var currentTextY = textY + textOffsetY;

            // Apply text animation opacity
            var lineTextColor = Color.FromArgb((int)(textOpacity * 255), textColor);
            var lineOutlineColor = Color.FromArgb((int)(textOpacity * 255), outlineColor);

            // For typewriter effect, show partial text
            var displayLine = line;
            if (textAnimationType == Models.TextAnimationType.Typewriter)
            {
                var charsToShow = Math.Min(visibleChars, line.Length);
                displayLine = line.Substring(0, charsToShow);
                visibleChars -= line.Length;
                if (visibleChars <= 0) break;
            }

            // Draw text outline (shadow effect)
            for (int dx = -2; dx <= 2; dx++)
            {
                for (int dy = -2; dy <= 2; dy++)
                {
                    if (dx != 0 || dy != 0)
                    {
                        using var outlineBrush = new SolidBrush(lineOutlineColor);
                        graphics.DrawString(displayLine, font, outlineBrush, textX + dx, currentTextY + dy);
                    }
                }
            }

            // Draw main text
            using var textBrush = new SolidBrush(lineTextColor);
            graphics.DrawString(displayLine, font, textBrush, textX, currentTextY);
            textY += lineHeight + 10; // Add spacing between lines
        }

        // Restore original transform
        graphics.Transform = originalTransform;

        // Draw character and anime info
        var characterAnimeText = $"— {quote.Character} ({quote.Anime})";
        using var smallerFont = CreateAnimeFont(fontSize * 0.6f, settings);
        var characterAnimeSize = graphics.MeasureString(characterAnimeText, smallerFont);
        var characterAnimeX = panelX + panelWidth - characterAnimeSize.Width - 40;
        var characterAnimeY = panelY + panelHeight - characterAnimeSize.Height - 30;

        // Draw character/anime outline
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx != 0 || dy != 0)
                {
                    using var outlineBrush = new SolidBrush(outlineColor);
                    graphics.DrawString(characterAnimeText, smallerFont, outlineBrush, 
                        characterAnimeX + dx, characterAnimeY + dy);
                }
            }
        }

        // Draw character/anime text
        using var characterAnimeBrush = new SolidBrush(Color.FromArgb(230, textColor));
        graphics.DrawString(characterAnimeText, smallerFont, characterAnimeBrush, characterAnimeX, characterAnimeY);
    }

    /// <summary>
    /// Protected rendering method - proprietary panel drawing algorithm.
    /// </summary>
    [System.Diagnostics.DebuggerStepThrough]
    private void DrawRoundedPanel(Graphics graphics, int x, int y, int width, int height, WallpaperSettings settings, float glowIntensity)
    {
        var cornerRadius = 20;
        var panelColor = ColorTranslator.FromHtml(settings.PanelColor);
        var alpha = (int)(settings.PanelOpacity * 255);
        
        using var path = CreateRoundedRectangle(x, y, width, height, cornerRadius);
        
        // Draw glow effect
        var glowSize = (int)(20 * glowIntensity);
        for (int i = glowSize; i > 0; i--)
        {
            var glowAlpha = (int)(20 * glowIntensity * (1 - i / (float)glowSize));
            using var glowBrush = new SolidBrush(Color.FromArgb(glowAlpha, panelColor));
            using var glowPath = CreateRoundedRectangle(x - i, y - i, width + 2 * i, height + 2 * i, cornerRadius + i);
            graphics.FillPath(glowBrush, glowPath);
        }

        // Draw main panel
        using var panelBrush = new SolidBrush(Color.FromArgb(alpha, panelColor));
        graphics.FillPath(panelBrush, path);

        // Draw border
        using var borderPen = new Pen(Color.FromArgb(100, Color.White), 2);
        graphics.DrawPath(borderPen, path);
    }

    private GraphicsPath CreateRoundedRectangle(int x, int y, int width, int height, int radius)
    {
        var path = new GraphicsPath();
        var diameter = radius * 2;
        
        path.AddArc(x, y, diameter, diameter, 180, 90);
        path.AddArc(x + width - diameter, y, diameter, diameter, 270, 90);
        path.AddArc(x + width - diameter, y + height - diameter, diameter, diameter, 0, 90);
        path.AddArc(x, y + height - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        
        return path;
    }

    private Font CreateAnimeFont(float fontSize, WallpaperSettings settings)
    {
        var fontFamilies = new[] { settings.FontFamily }.Concat(settings.FallbackFonts).ToArray();
        
        foreach (var familyName in fontFamilies)
        {
            try
            {
                return new Font(familyName, fontSize, FontStyle.Bold, GraphicsUnit.Pixel);
            }
            catch
            {
                // Continue to next font
            }
        }

        // Fallback to system default
        return new Font(FontFamily.GenericSansSerif, fontSize, FontStyle.Bold, GraphicsUnit.Pixel);
    }

    private List<string> WrapText(Graphics graphics, string text, Font font, int maxWidth)
    {
        var lines = new List<string>();
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        if (words.Length == 0)
        {
            return lines;
        }

        var currentLine = words[0];
        
        for (int i = 1; i < words.Length; i++)
        {
            var testLine = currentLine + " " + words[i];
            var testSize = graphics.MeasureString(testLine, font);
            
            if (testSize.Width > maxWidth)
            {
                lines.Add(currentLine);
                currentLine = words[i];
            }
            else
            {
                currentLine = testLine;
            }
        }
        
        lines.Add(currentLine);
        return lines;
    }

    /// <summary>
    /// Creates a wallpaper for a specific monitor.
    /// </summary>
    /// <param name="backgroundPath">Path to background image (or null for solid color)</param>
    /// <param name="quote">The quote to display</param>
    /// <param name="monitorIndex">The index of the monitor (0-based)</param>
    /// <param name="settings">Visual settings (will be adjusted for monitor resolution)</param>
    /// <returns>A bitmap image sized for the specified monitor</returns>
    public Bitmap CreateWallpaperForMonitor(string? backgroundPath, Quote quote, int monitorIndex, WallpaperSettings? settings = null)
    {
        var monitor = _monitorService.GetMonitorByIndex(monitorIndex);
        if (monitor == null)
        {
            // Fallback to primary monitor
            monitor = _monitorService.GetPrimaryMonitor();
            if (monitor == null)
            {
                throw new InvalidOperationException("No monitors detected");
            }
        }

        var monitorSettings = settings ?? new WallpaperSettings();
        monitorSettings.Width = monitor.Width;
        monitorSettings.Height = monitor.Height;

        return CreateWallpaperImage(backgroundPath, quote, monitorSettings);
    }

    /// <summary>
    /// Creates a combined wallpaper that spans all monitors.
    /// Useful for "Span" multi-monitor mode.
    /// </summary>
    /// <param name="backgroundPath">Path to background image (or null for solid color)</param>
    /// <param name="quote">The quote to display</param>
    /// <param name="settings">Visual settings (will be adjusted for combined resolution)</param>
    /// <returns>A bitmap image sized for the combined monitor area</returns>
    public Bitmap CreateWallpaperForAllMonitors(string? backgroundPath, Quote quote, WallpaperSettings? settings = null)
    {
        var combinedBounds = _monitorService.GetCombinedBounds();
        
        var combinedSettings = settings ?? new WallpaperSettings();
        combinedSettings.Width = combinedBounds.Width;
        combinedSettings.Height = combinedBounds.Height;

        return CreateWallpaperImage(backgroundPath, quote, combinedSettings);
    }

    /// <summary>
    /// Creates wallpapers for multiple monitors based on the configured multi-monitor mode.
    /// </summary>
    /// <param name="backgroundPath">Path to background image (or null for solid color)</param>
    /// <param name="quote">The quote to display</param>
    /// <param name="settings">Visual settings</param>
    /// <returns>A dictionary mapping monitor indices to wallpaper bitmaps</returns>
    public Dictionary<int, Bitmap> CreateWallpapersForMonitors(string? backgroundPath, Quote quote, WallpaperSettings? settings = null)
    {
        var wallpapers = new Dictionary<int, Bitmap>();
        var mode = AnimeQuoteWall.Core.Configuration.AppConfiguration.MultiMonitorMode;
        var enabledIndices = AnimeQuoteWall.Core.Configuration.AppConfiguration.EnabledMonitorIndices;

        if (mode == "Span")
        {
            // Create one large wallpaper spanning all monitors
            var combined = CreateWallpaperForAllMonitors(backgroundPath, quote, settings);
            var monitors = _monitorService.GetAllMonitors();
            
            // For span mode, we create one wallpaper but need to apply it to all monitors
            // Windows will handle the spanning automatically if the image matches the combined resolution
            foreach (var monitor in monitors)
            {
                wallpapers[monitor.Index] = combined;
            }
        }
        else if (mode == "All")
        {
            // Create individual wallpapers for each monitor
            var monitors = _monitorService.GetAllMonitors();
            foreach (var monitor in monitors)
            {
                if (enabledIndices.Count == 0 || enabledIndices.Contains(monitor.Index))
                {
                    wallpapers[monitor.Index] = CreateWallpaperForMonitor(backgroundPath, quote, monitor.Index, settings);
                }
            }
        }
        else // "Primary" mode
        {
            // Create wallpaper only for primary monitor
            var primary = _monitorService.GetPrimaryMonitor();
            if (primary != null)
            {
                wallpapers[primary.Index] = CreateWallpaperForMonitor(backgroundPath, quote, primary.Index, settings);
            }
        }

        return wallpapers;
    }
}