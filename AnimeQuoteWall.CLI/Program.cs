using System.Runtime.InteropServices;
using AnimeQuoteWall.Core.Configuration;
using AnimeQuoteWall.Core.Interfaces;
using AnimeQuoteWall.Core.Models;
using AnimeQuoteWall.Core.Services;

namespace AnimeQuoteWall.Console;

/// <summary>
/// Console application entry point for AnimeQuoteWall.
/// This program generates a custom desktop wallpaper with anime quotes.
/// 
/// How it works:
/// 1. Loads anime quotes from a JSON file
/// 2. Picks a random quote
/// 3. Finds a random background image (or uses a solid color)
/// 4. Creates a beautiful wallpaper with the quote overlaid
/// 5. Sets it as your Windows desktop wallpaper
/// </summary>
class Program
{
    #region Windows API - DO NOT MODIFY (used to set wallpaper)
    
    // This is a Windows system call that lets us change the desktop wallpaper
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    static extern bool SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);
    
    // Constants for the Windows API call
    const int SPI_SETDESKWALLPAPER = 0x0014;      // Command to set wallpaper
    const int SPIF_UPDATEINIFILE = 0x01;          // Save change to user profile
    const int SPIF_SENDWININICHANGE = 0x02;       // Notify all windows of the change
    
    #endregion

    #region Services - These handle all the business logic
    
    // QuoteService: Loads and manages anime quotes
    private static readonly IQuoteService _quoteService = new QuoteService();
    
    // BackgroundService: Finds background images
    private static readonly IBackgroundService _backgroundService = new BackgroundService();
    
    // WallpaperService: Creates the actual wallpaper image
    private static readonly IWallpaperService _wallpaperService = new WallpaperService();
    
    #endregion

    /// <summary>
    /// Main entry point - this is where the program starts running.
    /// </summary>
    static async Task Main()
    {
        try
        {
            System.Console.WriteLine("AnimeQuoteWall - Starting wallpaper generation...");
            System.Console.WriteLine();

            AppConfiguration.EnsureDirectories();
            System.Console.WriteLine($"Working directory: {AppConfiguration.BaseDirectory}");
            System.Console.WriteLine();

            await _quoteService.EnsureQuotesFileAsync(AppConfiguration.QuotesFilePath);

            var quotes = await _quoteService.LoadQuotesAsync(AppConfiguration.QuotesFilePath);

            if (quotes == null || quotes.Count == 0)
            {
                throw new InvalidOperationException(
                    "No quotes found. Check your quotes.json file.\n" +
                    $"Expected location: {AppConfiguration.QuotesFilePath}");
            }

            System.Console.WriteLine($"Loaded {quotes.Count} quotes");

            var selectedQuote = _quoteService.GetRandomQuote(quotes);
            System.Console.WriteLine($"Selected: \"{selectedQuote.Text}\"");
            System.Console.WriteLine($"  - {selectedQuote.Character} ({selectedQuote.Anime})");
            System.Console.WriteLine();

            var backgroundPath = _backgroundService.GetRandomBackgroundImage(AppConfiguration.BackgroundsDirectory);

            if (backgroundPath != null)
            {
                System.Console.WriteLine($"Background: {Path.GetFileName(backgroundPath)}");
            }
            else
            {
                System.Console.WriteLine("No background images found; will use a solid color.");
                System.Console.WriteLine($"  Tip: add images to {AppConfiguration.BackgroundsDirectory}");
            }
            System.Console.WriteLine();

            var settings = new WallpaperSettings();

            System.Console.WriteLine("Creating wallpaper...");
            using var wallpaperBitmap = _wallpaperService.CreateWallpaperImage(backgroundPath, selectedQuote, settings);

            await _wallpaperService.SaveImageAsync(wallpaperBitmap, AppConfiguration.CurrentWallpaperPath);
            System.Console.WriteLine($"Saved: {AppConfiguration.CurrentWallpaperPath}");

            var success = SetDesktopWallpaper(AppConfiguration.CurrentWallpaperPath);
            if (success)
            {
                System.Console.WriteLine("Desktop wallpaper updated.");
            }
            else
            {
                System.Console.WriteLine("Failed to set desktop wallpaper (but the image was saved).");
            }
            System.Console.WriteLine();

            try
            {
                System.Console.WriteLine("Generating animation frames...");
                var frames = await _wallpaperService.GenerateAnimationFramesAsync(
                    backgroundPath, selectedQuote, settings, AppConfiguration.FramesDirectory);
                System.Console.WriteLine($"Generated {frames.Count} animation frames.");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Frame generation skipped: {ex.Message}");
            }

            System.Console.WriteLine();
            System.Console.WriteLine("Done.");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine();
            System.Console.WriteLine("ERROR:");
            System.Console.WriteLine($"  {ex.Message}");
            System.Console.WriteLine();
            System.Console.WriteLine("   Need help? Check:");
            System.Console.WriteLine($"   - Is quotes.json file present at: {AppConfiguration.QuotesFilePath}");
            System.Console.WriteLine($"   - Do you have write permissions in: {AppConfiguration.BaseDirectory}");
            System.Console.WriteLine();
            
            // Exit with error code so automated tools know something went wrong
            Environment.Exit(1); 
        }
    }

    /// <summary>
    /// Sets the desktop wallpaper using Windows API.
    /// </summary>
    /// <param name="imagePath">Path to the image file to set as wallpaper.</param>
    /// <returns>True if successful, false otherwise.</returns>
    private static bool SetDesktopWallpaper(string imagePath)
    {
        if (!File.Exists(imagePath))
        {
            System.Console.WriteLine($"Wallpaper file not found: {imagePath}");
            return false;
        }

        try
        {
            return SystemParametersInfo(
                SPI_SETDESKWALLPAPER, 
                0, 
                imagePath, 
                SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE);
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Failed to set wallpaper: {ex.Message}");
            return false;
        }
    }
} 
