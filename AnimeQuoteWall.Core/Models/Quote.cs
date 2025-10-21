using System.Text.Json.Serialization;

namespace AnimeQuoteWall.Core.Models;

/// <summary>
/// Represents an anime quote with associated character and anime information.
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
}