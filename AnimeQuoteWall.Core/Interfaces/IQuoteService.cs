using AnimeQuoteWall.Core.Models;

namespace AnimeQuoteWall.Core.Interfaces;

/// <summary>
/// Interface for quote management operations.
/// </summary>
public interface IQuoteService
{
    /// <summary>
    /// Loads quotes from the specified file path.
    /// </summary>
    /// <param name="filePath">Path to the quotes JSON file.</param>
    /// <returns>A list of valid quotes.</returns>
    Task<List<Quote>> LoadQuotesAsync(string filePath);

    /// <summary>
    /// Saves quotes to the specified file path.
    /// </summary>
    /// <param name="quotes">The quotes to save.</param>
    /// <param name="filePath">Path to save the quotes JSON file.</param>
    Task SaveQuotesAsync(List<Quote> quotes, string filePath);

    /// <summary>
    /// Gets a random quote from the provided list.
    /// </summary>
    /// <param name="quotes">List of quotes to choose from.</param>
    /// <returns>A randomly selected quote.</returns>
    Quote GetRandomQuote(List<Quote> quotes);

    /// <summary>
    /// Ensures the quotes file exists with sample data if it doesn't exist.
    /// </summary>
    /// <param name="filePath">Path to the quotes file.</param>
    Task EnsureQuotesFileAsync(string filePath);
}