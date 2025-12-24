using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using ImageMagick;

namespace AnimeQuoteWall.Core.Services;

/// <summary>
/// Service for editing images and videos (crop, resize, filters, text overlay).
/// </summary>
public class MediaEditingService
{
    /// <summary>
    /// Loads an image from file path.
    /// </summary>
    public Bitmap LoadImage(string imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
            throw new FileNotFoundException($"Image file not found: {imagePath}");

        return new Bitmap(imagePath);
    }

    /// <summary>
    /// Loads a video file and extracts frames for editing.
    /// </summary>
    public List<Bitmap> ExtractVideoFrames(string videoPath, int maxFrames = 10)
    {
        if (string.IsNullOrWhiteSpace(videoPath) || !File.Exists(videoPath))
            throw new FileNotFoundException($"Video file not found: {videoPath}");

        var frames = new List<Bitmap>();
        
        try
        {
            // Use ImageMagick to extract frames from video/GIF
            using var collection = new MagickImageCollection();
            collection.Read(videoPath);

            int frameCount = Math.Min(collection.Count, maxFrames);
            for (int i = 0; i < frameCount; i++)
            {
                var frame = collection[i];
                using var ms = new MemoryStream();
                frame.Write(ms, MagickFormat.Bmp);
                ms.Position = 0;
                
                // Create bitmap from stream and clone it to ensure it's independent of the stream
                using var tempBitmap = new Bitmap(ms);
                var bitmap = new Bitmap(tempBitmap);
                frames.Add(bitmap);
            }
        }
        catch
        {
            // Fallback: try to load as single image
            try
            {
                var bitmap = LoadImage(videoPath);
                frames.Add(bitmap);
            }
            catch { }
        }

        return frames;
    }

    /// <summary>
    /// Applies a filter effect to an image.
    /// </summary>
    public Bitmap ApplyFilter(Bitmap image, string filterType, float intensity = 1.0f)
    {
        if (image == null)
            throw new ArgumentNullException(nameof(image));

        var result = new Bitmap(image.Width, image.Height);
        
        using (var graphics = Graphics.FromImage(result))
        {
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;

            switch (filterType.ToLowerInvariant())
            {
                case "blur":
                    ApplyBlurFilter(image, result, graphics, intensity);
                    break;
                case "glow":
                    ApplyGlowFilter(image, result, graphics, intensity);
                    break;
                case "sepia":
                    ApplySepiaFilter(image, result);
                    break;
                case "grayscale":
                    ApplyGrayscaleFilter(image, result);
                    break;
                case "vintage":
                    ApplyVintageFilter(image, result);
                    break;
                case "brightness":
                    ApplyBrightnessFilter(image, result, intensity);
                    break;
                case "contrast":
                    ApplyContrastFilter(image, result, intensity);
                    break;
                default:
                    graphics.DrawImage(image, 0, 0);
                    break;
            }
        }

        return result;
    }

    /// <summary>
    /// Crops an image to the specified rectangle.
    /// </summary>
    public Bitmap CropImage(Bitmap image, Rectangle cropArea)
    {
        if (image == null)
            throw new ArgumentNullException(nameof(image));

        // Ensure crop area is within image bounds
        cropArea.X = Math.Max(0, Math.Min(cropArea.X, image.Width - 1));
        cropArea.Y = Math.Max(0, Math.Min(cropArea.Y, image.Height - 1));
        cropArea.Width = Math.Min(cropArea.Width, image.Width - cropArea.X);
        cropArea.Height = Math.Min(cropArea.Height, image.Height - cropArea.Y);

        var cropped = new Bitmap(cropArea.Width, cropArea.Height);
        using (var graphics = Graphics.FromImage(cropped))
        {
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            graphics.DrawImage(image, new Rectangle(0, 0, cropArea.Width, cropArea.Height), cropArea, GraphicsUnit.Pixel);
        }

        return cropped;
    }

    /// <summary>
    /// Resizes an image to the specified dimensions.
    /// </summary>
    public Bitmap ResizeImage(Bitmap image, int width, int height)
    {
        if (image == null)
            throw new ArgumentNullException(nameof(image));

        var resized = new Bitmap(width, height);
        using (var graphics = Graphics.FromImage(resized))
        {
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            graphics.DrawImage(image, 0, 0, width, height);
        }

        return resized;
    }

    /// <summary>
    /// Adds text overlay to an image using quote and settings.
    /// </summary>
    public Bitmap AddTextOverlay(Bitmap image, Core.Models.Quote quote, Core.Models.WallpaperSettings settings)
    {
        if (image == null)
            throw new ArgumentNullException(nameof(image));
        if (quote == null)
            throw new ArgumentNullException(nameof(quote));

        var result = new Bitmap(image);
        var wallpaperService = new WallpaperService();
        
        // Use WallpaperService to draw the quote overlay
        using (var graphics = Graphics.FromImage(result))
        {
            WallpaperService.ConfigureGraphicsQuality(graphics);
            wallpaperService.DrawQuote(graphics, quote, result.Width, result.Height, settings);
        }

        return result;
    }

    #region Filter Implementations

    private void ApplyBlurFilter(Bitmap source, Bitmap destination, Graphics graphics, float intensity)
    {
        // Simple blur using ImageMagick for better quality
        try
        {
            using var ms = new MemoryStream();
            source.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            ms.Position = 0;
            using var magickImage = new MagickImage(ms);
            magickImage.Blur(0, (int)(intensity * 5));
            using var blurredMs = new MemoryStream();
            magickImage.Write(blurredMs);
            blurredMs.Position = 0;
            var blurred = new Bitmap(blurredMs);
            graphics.DrawImage(blurred, 0, 0);
            blurred.Dispose();
        }
        catch
        {
            // Fallback: draw original
            graphics.DrawImage(source, 0, 0);
        }
    }

    private void ApplyGlowFilter(Bitmap source, Bitmap destination, Graphics graphics, float intensity)
    {
        // Draw original image
        graphics.DrawImage(source, 0, 0);
        
        // Apply glow effect (simplified - would need more complex implementation for true glow)
        // For now, just draw a slightly brighter version
        using var glowImage = new Bitmap(source);
        var colorMatrix = new ColorMatrix(new float[][]
        {
            new float[] { 1.2f, 0, 0, 0, 0 },
            new float[] { 0, 1.2f, 0, 0, 0 },
            new float[] { 0, 0, 1.2f, 0, 0 },
            new float[] { 0, 0, 0, 1, 0 },
            new float[] { 0.1f, 0.1f, 0.1f, 0, 1 }
        });

        var imageAttributes = new ImageAttributes();
        imageAttributes.SetColorMatrix(colorMatrix);
        graphics.DrawImage(glowImage, new Rectangle(0, 0, source.Width, source.Height), 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, imageAttributes);
    }

    private void ApplySepiaFilter(Bitmap source, Bitmap destination)
    {
        var colorMatrix = new ColorMatrix(new float[][]
        {
            new float[] { 0.393f, 0.769f, 0.189f, 0, 0 },
            new float[] { 0.349f, 0.686f, 0.168f, 0, 0 },
            new float[] { 0.272f, 0.534f, 0.131f, 0, 0 },
            new float[] { 0, 0, 0, 1, 0 },
            new float[] { 0, 0, 0, 0, 1 }
        });

        using var graphics = Graphics.FromImage(destination);
        var imageAttributes = new ImageAttributes();
        imageAttributes.SetColorMatrix(colorMatrix);
        graphics.DrawImage(source, new Rectangle(0, 0, source.Width, source.Height), 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, imageAttributes);
    }

    private void ApplyGrayscaleFilter(Bitmap source, Bitmap destination)
    {
        var colorMatrix = new ColorMatrix(new float[][]
        {
            new float[] { 0.299f, 0.299f, 0.299f, 0, 0 },
            new float[] { 0.587f, 0.587f, 0.587f, 0, 0 },
            new float[] { 0.114f, 0.114f, 0.114f, 0, 0 },
            new float[] { 0, 0, 0, 1, 0 },
            new float[] { 0, 0, 0, 0, 1 }
        });

        using var graphics = Graphics.FromImage(destination);
        var imageAttributes = new ImageAttributes();
        imageAttributes.SetColorMatrix(colorMatrix);
        graphics.DrawImage(source, new Rectangle(0, 0, source.Width, source.Height), 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, imageAttributes);
    }

    private void ApplyVintageFilter(Bitmap source, Bitmap destination)
    {
        // Combine sepia with slight desaturation and vignette effect
        var colorMatrix = new ColorMatrix(new float[][]
        {
            new float[] { 0.9f, 0.5f, 0.1f, 0, 0 },
            new float[] { 0.3f, 0.7f, 0.1f, 0, 0 },
            new float[] { 0.2f, 0.3f, 0.5f, 0, 0 },
            new float[] { 0, 0, 0, 1, 0 },
            new float[] { 0.05f, 0.05f, 0.05f, 0, 1 }
        });

        using var graphics = Graphics.FromImage(destination);
        var imageAttributes = new ImageAttributes();
        imageAttributes.SetColorMatrix(colorMatrix);
        graphics.DrawImage(source, new Rectangle(0, 0, source.Width, source.Height), 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, imageAttributes);
    }

    private void ApplyBrightnessFilter(Bitmap source, Bitmap destination, float intensity)
    {
        var brightness = (intensity - 1.0f) * 50; // Convert to -50 to +50 range
        var colorMatrix = new ColorMatrix(new float[][]
        {
            new float[] { 1, 0, 0, 0, 0 },
            new float[] { 0, 1, 0, 0, 0 },
            new float[] { 0, 0, 1, 0, 0 },
            new float[] { 0, 0, 0, 1, 0 },
            new float[] { brightness / 255f, brightness / 255f, brightness / 255f, 0, 1 }
        });

        using var graphics = Graphics.FromImage(destination);
        var imageAttributes = new ImageAttributes();
        imageAttributes.SetColorMatrix(colorMatrix);
        graphics.DrawImage(source, new Rectangle(0, 0, source.Width, source.Height), 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, imageAttributes);
    }

    private void ApplyContrastFilter(Bitmap source, Bitmap destination, float intensity)
    {
        var contrast = intensity; // 1.0 = no change, 2.0 = double contrast
        var colorMatrix = new ColorMatrix(new float[][]
        {
            new float[] { contrast, 0, 0, 0, 0 },
            new float[] { 0, contrast, 0, 0, 0 },
            new float[] { 0, 0, contrast, 0, 0 },
            new float[] { 0, 0, 0, 1, 0 },
            new float[] { (1 - contrast) / 2f, (1 - contrast) / 2f, (1 - contrast) / 2f, 0, 1 }
        });

        using var graphics = Graphics.FromImage(destination);
        var imageAttributes = new ImageAttributes();
        imageAttributes.SetColorMatrix(colorMatrix);
        graphics.DrawImage(source, new Rectangle(0, 0, source.Width, source.Height), 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, imageAttributes);
    }

    #endregion
}

