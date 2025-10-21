namespace AnimeQuoteWall.Core.Interfaces;

/// <summary>
/// Interface for background image management.
/// </summary>
public interface IBackgroundService
{
    /// <summary>
    /// Gets a random background image path from the backgrounds directory.
    /// </summary>
    /// <param name="backgroundsDirectory">Path to the backgrounds directory.</param>
    /// <returns>Path to a random background image, or null if none found.</returns>
    string? GetRandomBackgroundImage(string backgroundsDirectory);

    /// <summary>
    /// Gets all background image paths from the backgrounds directory.
    /// </summary>
    /// <param name="backgroundsDirectory">Path to the backgrounds directory.</param>
    /// <returns>List of background image paths.</returns>
    List<string> GetAllBackgroundImages(string backgroundsDirectory);

    /// <summary>
    /// Validates if a file is a supported image format.
    /// </summary>
    /// <param name="filePath">Path to the file to validate.</param>
    /// <returns>True if the file is a supported image format.</returns>
    bool IsValidImageFile(string filePath);

    /// <summary>
    /// Ensures the backgrounds directory exists.
    /// </summary>
    /// <param name="backgroundsDirectory">Path to the backgrounds directory.</param>
    void EnsureBackgroundsDirectory(string backgroundsDirectory);
}