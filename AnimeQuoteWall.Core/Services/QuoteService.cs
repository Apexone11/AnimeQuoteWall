using System.Text.Json;
using AnimeQuoteWall.Core.Interfaces;
using AnimeQuoteWall.Core.Models;

namespace AnimeQuoteWall.Core.Services;

/// <summary>
/// Service for managing anime quotes.
/// Handles loading, saving, and selecting quotes from JSON files.
/// 
/// FOR BEGINNERS: This class does 4 main things:
/// 1. Load quotes from a JSON file
/// 2. Save quotes to a JSON file  
/// 3. Pick a random quote from a list
/// 4. Create a sample quotes file if one doesn't exist
/// </summary>
public class QuoteService : IQuoteService
{
    // Thread-safe random number generator (safe to use from multiple threads)
    // In .NET 6+, Random.Shared is the recommended way for shared Random instances
    private static readonly Random _random = Random.Shared;

    /// <summary>
    /// Loads all quotes from a JSON file.
    /// Only returns valid quotes (ones that have text, character, and anime filled in).
    /// </summary>
    /// <param name="filePath">Full path to the quotes.json file</param>
    /// <returns>List of valid quotes (empty list if file doesn't exist)</returns>
    public async Task<List<Quote>> LoadQuotesAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return new List<Quote>();
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
            
            // Use options that handle missing properties gracefully (for backward compatibility)
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };
            
            var quotes = JsonSerializer.Deserialize<List<Quote>>(json, options) ?? new List<Quote>();
            
            // Ensure backward compatibility: initialize Categories and Tags if null
            foreach (var quote in quotes)
            {
                quote.Categories ??= new List<string>();
                quote.Tags ??= new List<string>();
            }
            
            return quotes.Where(q => q.IsValid()).ToList();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to load quotes from {filePath}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Saves a list of quotes to a JSON file with nice formatting.
    /// </summary>
    /// <param name="quotes">The quotes to save</param>
    /// <param name="filePath">Where to save the file</param>
    public async Task SaveQuotesAsync(List<Quote> quotes, string filePath)
    {
        try
        {
            // Configure JSON formatting to be readable (indented, lowercase properties)
            var options = new JsonSerializerOptions 
            { 
                WriteIndented = true,                           // Makes JSON pretty with line breaks
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase  // Uses camelCase for properties
            };
            
            // Convert quotes to JSON text
            var json = JsonSerializer.Serialize(quotes, options);
            
            // Save to file
            await File.WriteAllTextAsync(filePath, json).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to save quotes to {filePath}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Picks a random quote from the list.
    /// </summary>
    /// <param name="quotes">List of quotes to choose from</param>
    /// <returns>One randomly selected quote</returns>
    public Quote GetRandomQuote(List<Quote> quotes)
    {
        // Safety check: make sure we have quotes to choose from
        if (quotes == null || quotes.Count == 0)
        {
            throw new ArgumentException("Quote list cannot be null or empty.", nameof(quotes));
        }

        // Pick a random index and return that quote
        // _random.Next(quotes.Count) returns a number from 0 to (count-1)
        return quotes[_random.Next(quotes.Count)];
    }

    /// <summary>
    /// Creates a quotes.json file with sample quotes if it doesn't exist.
    /// This prevents errors when running the app for the first time.
    /// </summary>
    /// <param name="filePath">Path where the quotes file should be</param>
    public async Task EnsureQuotesFileAsync(string filePath)
    {
        if (File.Exists(filePath))
        {
            return;
        }

        var sampleQuotes = new List<Quote>
        {
            new()
            {
                Text = "The world isn't perfect, but it's there for us, trying the best it can. That's what makes it so damn beautiful.",
                Character = "Roy Mustang",
                Anime = "Fullmetal Alchemist"
            },
            new()
            {
                Text = "If you don't take risks, you can't create a future!",
                Character = "Monkey D. Luffy",
                Anime = "One Piece"
            },
            new()
            {
                Text = "Hard work is necessary, but talent is also important.",
                Character = "Senku Ishigami",
                Anime = "Dr. Stone"
            }
        };

        await SaveQuotesAsync(sampleQuotes, filePath).ConfigureAwait(false);
    }
}