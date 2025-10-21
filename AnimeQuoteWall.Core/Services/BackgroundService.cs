using AnimeQuoteWall.Core.Configuration;
using AnimeQuoteWall.Core.Interfaces;

namespace AnimeQuoteWall.Core.Services;

/// <summary>
/// Service for managing background images.
/// </summary>
public class BackgroundService : IBackgroundService
{
    private readonly Random _random = new();

    /// <inheritdoc />
    public string? GetRandomBackgroundImage(string backgroundsDirectory)
    {
        var images = GetAllBackgroundImages(backgroundsDirectory);
        return images.Count > 0 ? images[_random.Next(images.Count)] : null;
    }

    /// <inheritdoc />
    public List<string> GetAllBackgroundImages(string backgroundsDirectory)
    {
        if (!Directory.Exists(backgroundsDirectory))
        {
            return new List<string>();
        }

        var images = new List<string>();
        
        foreach (var extension in AppConfiguration.SupportedImageExtensions)
        {
            var pattern = $"*{extension}";
            images.AddRange(Directory.GetFiles(backgroundsDirectory, pattern, SearchOption.TopDirectoryOnly));
        }

        return images.Where(IsValidImageFile).ToList();
    }

    /// <inheritdoc />
    public bool IsValidImageFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return false;
        }

        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return AppConfiguration.SupportedImageExtensions.Contains(extension);
    }

    /// <inheritdoc />
    public void EnsureBackgroundsDirectory(string backgroundsDirectory)
    {
        Directory.CreateDirectory(backgroundsDirectory);
    }
}