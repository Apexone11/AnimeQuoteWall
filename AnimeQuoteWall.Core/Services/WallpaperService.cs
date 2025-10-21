using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using AnimeQuoteWall.Core.Interfaces;
using AnimeQuoteWall.Core.Models;

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
public class WallpaperService : IWallpaperService
{
    /// <summary>
    /// Creates a wallpaper image by combining a background with a quote.
    /// </summary>
    /// <param name="backgroundPath">Path to background image (or null for solid color)</param>
    /// <param name="quote">The quote to display</param>
    /// <param name="settings">Visual settings (size, colors, fonts, etc.)</param>
    /// <returns>A bitmap image ready to be saved as wallpaper</returns>
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
    private static void ConfigureGraphicsQuality(Graphics graphics)
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
        await Task.Run(() => bitmap.Save(filePath, ImageFormat.Png));
    }

    /// <inheritdoc />
    public Bitmap LoadBackgroundBitmap(string? backgroundPath, WallpaperSettings settings)
    {
        if (!string.IsNullOrEmpty(backgroundPath) && File.Exists(backgroundPath))
        {
            try
            {
                using var originalImage = Image.FromFile(backgroundPath);
                var resized = new Bitmap(settings.Width, settings.Height);
                
                using var resizeGraphics = Graphics.FromImage(resized);
                resizeGraphics.SmoothingMode = SmoothingMode.HighQuality;
                resizeGraphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                resizeGraphics.CompositingQuality = CompositingQuality.HighQuality;
                
                resizeGraphics.DrawImage(originalImage, 0, 0, settings.Width, settings.Height);
                return resized;
            }
            catch
            {
                // Fall back to solid color if image loading fails
            }
        }

        // Create solid color background
        var solidBackground = new Bitmap(settings.Width, settings.Height);
        using var graphics = Graphics.FromImage(solidBackground);
        using var brush = new SolidBrush(ColorTranslator.FromHtml(settings.BackgroundColor));
        graphics.FillRectangle(brush, 0, 0, settings.Width, settings.Height);
        
        return solidBackground;
    }

    private void DrawQuote(Graphics graphics, Quote quote, int imageWidth, int imageHeight, WallpaperSettings settings)
    {
        DrawAnimatedQuote(graphics, quote, imageWidth, imageHeight, settings, 0.5f);
    }

    private void DrawAnimatedQuote(Graphics graphics, Quote quote, int imageWidth, int imageHeight, WallpaperSettings settings, float animationProgress)
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

        var panelX = (imageWidth - panelWidth) / 2 + (int)pulseOffset;
        var panelY = (imageHeight - panelHeight) / 2;

        // Draw panel with glow effect
        DrawRoundedPanel(graphics, panelX, panelY, panelWidth, panelHeight, settings, glowIntensity);

        // Draw quote text with better positioning
        var textY = panelY + 40;
        var textColor = ColorTranslator.FromHtml(settings.TextColor);
        var outlineColor = ColorTranslator.FromHtml(settings.OutlineColor);

        foreach (var line in wrappedLines)
        {
            var textSize = graphics.MeasureString(line, font);
            var textX = panelX + (panelWidth - textSize.Width) / 2;

            // Draw text outline (shadow effect)
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
            textY += lineHeight + 10; // Add spacing between lines
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
        using var characterAnimeBrush = new SolidBrush(Color.FromArgb(230, textColor));
        graphics.DrawString(characterAnimeText, smallerFont, characterAnimeBrush, characterAnimeX, characterAnimeY);
    }

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
}