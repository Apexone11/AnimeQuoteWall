using AnimeQuoteWall.Core.Services;
using AnimeQuoteWall.Core.Models;

var quoteService = new QuoteService();
var backgroundService = new BackgroundService();
var wallpaperService = new WallpaperService();

Console.WriteLine("Testing AnimeQuoteWall functionality...");

// Test quote loading
var quotes = await quoteService.LoadQuotesAsync("quotes.json");
Console.WriteLine($"Loaded {quotes.Count} quotes");

if (quotes.Count > 0)
{
    var randomQuote = quotes[new Random().Next(quotes.Count)];
    Console.WriteLine($"Random quote: \"{randomQuote.Text}\" - {randomQuote.Character} ({randomQuote.Anime})");
    
    // Test wallpaper generation
    var settings = new WallpaperSettings
    {
        Width = 1920,
        Height = 1080
    };
    
    try
    {
        var wallpaper = wallpaperService.CreateWallpaperImage(null, randomQuote, settings);
        Console.WriteLine("Wallpaper generated successfully!");
        
        // Save the wallpaper
        wallpaper.Save("current.png", System.Drawing.Imaging.ImageFormat.Png);
        
        if (File.Exists("current.png"))
        {
            var fileInfo = new FileInfo("current.png");
            Console.WriteLine($"Wallpaper size: {fileInfo.Length / 1024}KB");
        }
        
        wallpaper.Dispose();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error generating wallpaper: {ex.Message}");
    }
}

Console.WriteLine("Test completed!");