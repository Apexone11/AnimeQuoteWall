using AnimeQuoteWall.Core.Configuration;
using AnimeQuoteWall.Core.Interfaces;

namespace AnimeQuoteWall.Core.Services;

/// <summary>
/// Service for managing background images.
/// 
/// This service provides:
/// - Getting random background images
/// - Listing all available background images
/// - Validating image files
/// - Ensuring backgrounds directory exists
/// 
/// </summary>
public class BackgroundService : IBackgroundService
{
    /// <summary>
    /// Random number generator for selecting random backgrounds.
    /// Using Random.Shared for thread-safe random number generation (.NET 6+).
    /// </summary>
    private static readonly Random _random = Random.Shared;

    /// <summary>
    /// Gets a random background image from the specified directory.
    /// </summary>
    /// <param name="backgroundsDirectory">Directory containing background images</param>
    /// <returns>Path to a random background image, or null if none found</returns>
    public string? GetRandomBackgroundImage(string backgroundsDirectory)
    {
        var images = GetAllBackgroundImages(backgroundsDirectory);
        // Return random image if available, otherwise null
        return images.Count > 0 ? images[_random.Next(images.Count)] : null;
    }

    /// <summary>
    /// Gets all valid background images from the specified directory.
    /// 
    /// Searches for files with supported image extensions (.jpg, .jpeg, .png, .bmp, .gif)
    /// and validates them to ensure they are actual image files.
    /// Removes duplicates based on file content hash (MD5).
    /// 
    /// </summary>
    /// <param name="backgroundsDirectory">Directory to search for images</param>
    /// <returns>List of paths to valid image files (deduplicated)</returns>
    public List<string> GetAllBackgroundImages(string backgroundsDirectory)
    {
        // Return empty list if directory doesn't exist
        if (!Directory.Exists(backgroundsDirectory))
        {
            return new List<string>();
        }

        var images = new List<string>();
        
        // Search for files with supported image extensions
        foreach (var extension in AppConfiguration.SupportedImageExtensions)
        {
            var pattern = $"*{extension}";
            // Only search top-level directory (not subdirectories)
            images.AddRange(Directory.GetFiles(backgroundsDirectory, pattern, SearchOption.TopDirectoryOnly));
        }

        // Filter to only valid image files
        var validImages = images.Where(IsValidImageFile).ToList();
        
        // Deduplicate by file content hash (keep first occurrence)
        return DeduplicateByContentHash(validImages);
    }

    /// <summary>
    /// Removes duplicate images based on file content hash (MD5).
    /// Keeps the first occurrence of each unique file.
    /// </summary>
    /// <param name="imagePaths">List of image file paths</param>
    /// <returns>Deduplicated list of image paths</returns>
    private List<string> DeduplicateByContentHash(List<string> imagePaths)
    {
        if (imagePaths == null || imagePaths.Count == 0)
            return new List<string>();

        var seenHashes = new HashSet<string>();
        var deduplicated = new List<string>();

        foreach (var imagePath in imagePaths)
        {
            try
            {
                // Compute MD5 hash of file content
                using var md5 = System.Security.Cryptography.MD5.Create();
                using var stream = File.OpenRead(imagePath);
                var hashBytes = md5.ComputeHash(stream);
                var hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

                // Only add if we haven't seen this hash before
                if (seenHashes.Add(hashString))
                {
                    deduplicated.Add(imagePath);
                }
            }
            catch
            {
                // If we can't read the file, skip it (might be locked or corrupted)
                continue;
            }
        }

        return deduplicated;
    }

    /// <summary>
    /// Validates that a file path points to a valid image file.
    /// Checks file existence and extension.
    /// </summary>
    /// <param name="filePath">Path to the file to validate</param>
    /// <returns>True if the file exists and has a supported image extension</returns>
    public bool IsValidImageFile(string filePath)
    {
        // Check file exists and path is not empty
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return false;
        }

        // Check extension is in supported list
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return AppConfiguration.SupportedImageExtensions.Contains(extension);
    }

    /// <summary>
    /// Ensures the backgrounds directory exists.
    /// Creates the directory if it doesn't exist.
    /// </summary>
    /// <param name="backgroundsDirectory">Directory path to ensure exists</param>
    public void EnsureBackgroundsDirectory(string backgroundsDirectory)
    {
        Directory.CreateDirectory(backgroundsDirectory);
    }
}