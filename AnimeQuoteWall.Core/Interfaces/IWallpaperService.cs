using System.Drawing;
using AnimeQuoteWall.Core.Models;

namespace AnimeQuoteWall.Core.Interfaces;

/// <summary>
/// Interface for wallpaper generation and image processing.
/// </summary>
public interface IWallpaperService
{
    /// <summary>
    /// Creates a wallpaper image with the specified quote and background.
    /// </summary>
    /// <param name="backgroundPath">Path to the background image (optional).</param>
    /// <param name="quote">The quote to render on the wallpaper.</param>
    /// <param name="settings">Wallpaper generation settings.</param>
    /// <returns>A bitmap containing the generated wallpaper.</returns>
    Bitmap CreateWallpaperImage(string? backgroundPath, Quote quote, WallpaperSettings settings);

    /// <summary>
    /// Generates animation frames for the specified quote and background.
    /// </summary>
    /// <param name="backgroundPath">Path to the background image (optional).</param>
    /// <param name="quote">The quote to render in the frames.</param>
    /// <param name="settings">Wallpaper generation settings.</param>
    /// <param name="outputDirectory">Directory to save the frames.</param>
    /// <returns>List of generated frame file paths.</returns>
    Task<List<string>> GenerateAnimationFramesAsync(string? backgroundPath, Quote quote, WallpaperSettings settings, string outputDirectory);

    /// <summary>
    /// Saves a bitmap to the specified path.
    /// </summary>
    /// <param name="bitmap">The bitmap to save.</param>
    /// <param name="filePath">Path to save the image.</param>
    Task SaveImageAsync(Bitmap bitmap, string filePath);

    /// <summary>
    /// Loads a background bitmap from the specified path or creates a solid color bitmap.
    /// </summary>
    /// <param name="backgroundPath">Path to the background image (optional).</param>
    /// <param name="settings">Wallpaper settings for default dimensions and color.</param>
    /// <returns>A bitmap for use as background.</returns>
    Bitmap LoadBackgroundBitmap(string? backgroundPath, WallpaperSettings settings);
}