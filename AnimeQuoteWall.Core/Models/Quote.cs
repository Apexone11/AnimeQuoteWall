using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace AnimeQuoteWall.Core.Models;

/// <summary>
/// Represents an anime quote with associated character and anime information.
/// Includes support for categories, tags, favorites, and ratings.
/// </summary>
public class Quote
{
    /// <summary>
    /// Gets or sets the text content of the quote.
    /// </summary>
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the character who said the quote.
    /// </summary>
    [JsonPropertyName("character")]
    public string Character { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the anime series.
    /// </summary>
    [JsonPropertyName("anime")]
    public string Anime { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of categories for this quote.
    /// Categories are used for grouping and filtering quotes (e.g., "Action", "Romance", "Comedy").
    /// </summary>
    [JsonPropertyName("categories")]
    public List<string> Categories { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of tags for this quote.
    /// Tags are flexible keywords for searching and organizing quotes.
    /// </summary>
    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Gets or sets whether this quote is marked as a favorite.
    /// </summary>
    [JsonPropertyName("isFavorite")]
    public bool IsFavorite { get; set; } = false;

    /// <summary>
    /// Gets or sets the rating for this quote (1-5 stars).
    /// 0 means unrated. Valid range is 0-5.
    /// </summary>
    [JsonPropertyName("rating")]
    public int Rating { get; set; } = 0;

    /// <summary>
    /// Determines whether the quote has valid content.
    /// </summary>
    /// <returns>True if the quote has non-empty text, character, and anime properties.</returns>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Text) &&
               !string.IsNullOrWhiteSpace(Character) &&
               !string.IsNullOrWhiteSpace(Anime);
    }

    /// <summary>
    /// Returns a string representation of the quote.
    /// </summary>
    public override string ToString()
    {
        return $"\"{Text}\" - {Character} ({Anime})";
    }

    /// <summary>
    /// Checks if the quote belongs to any of the specified categories.
    /// </summary>
    /// <param name="categoryNames">List of category names to check</param>
    /// <returns>True if the quote belongs to any of the specified categories</returns>
    public bool HasAnyCategory(IEnumerable<string> categoryNames)
    {
        if (Categories == null || !Categories.Any()) return false;
        return Categories.Any(c => categoryNames.Contains(c, System.StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Checks if the quote has any of the specified tags.
    /// </summary>
    /// <param name="tagNames">List of tag names to check</param>
    /// <returns>True if the quote has any of the specified tags</returns>
    public bool HasAnyTag(IEnumerable<string> tagNames)
    {
        if (Tags == null || !Tags.Any()) return false;
        return Tags.Any(t => tagNames.Contains(t, System.StringComparer.OrdinalIgnoreCase));
    }
}