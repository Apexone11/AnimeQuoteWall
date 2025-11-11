using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using ImageMagick;

namespace AnimeQuoteWall.Core.Services;

/// <summary>
/// Service for generating thumbnails from video files (MP4, WebM, MOV).
/// </summary>
public class VideoThumbnailService
{
    private readonly string _thumbnailsDirectory;

    public VideoThumbnailService()
    {
        _thumbnailsDirectory = Path.Combine(
            AnimeQuoteWall.Core.Configuration.AppConfiguration.DefaultBaseDirectory,
            "thumbnails");
        Directory.CreateDirectory(_thumbnailsDirectory);
    }

    /// <summary>
    /// Gets or generates a thumbnail for a video file.
    /// </summary>
    /// <param name="videoPath">Path to the video file</param>
    /// <param name="width">Thumbnail width (default: 256)</param>
    /// <param name="height">Thumbnail height (default: 144)</param>
    /// <returns>Path to the thumbnail image, or null if generation failed</returns>
    public string? GetOrCreateThumbnail(string videoPath, int width = 256, int height = 144)
    {
        if (string.IsNullOrWhiteSpace(videoPath) || !File.Exists(videoPath))
            return null;

        try
        {
            // Generate thumbnail filename based on video file
            var videoFileName = Path.GetFileNameWithoutExtension(videoPath);
            var videoHash = GetFileHash(videoPath);
            var thumbnailFileName = $"{videoFileName}_{videoHash}.jpg";
            var thumbnailPath = Path.Combine(_thumbnailsDirectory, thumbnailFileName);

            // Return cached thumbnail if it exists
            if (File.Exists(thumbnailPath))
            {
                return thumbnailPath;
            }

            // Generate thumbnail using ImageMagick
            var result = GenerateThumbnail(videoPath, thumbnailPath, width, height);
            if (result != null)
            {
                // Enforce cache size limit after creating a new thumbnail
                EnforceCacheSizeLimit(200 * 1024 * 1024); // 200 MB
            }
            return result;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error generating thumbnail: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Generates a thumbnail from a video file.
    /// </summary>
    private string? GenerateThumbnail(string videoPath, string thumbnailPath, int width, int height)
    {
        try
        {
            // Use ImageMagick to extract frame from video
            using var collection = new MagickImageCollection();
            collection.Read(videoPath);
            
            if (collection.Count == 0)
                return null;

            // Get frame at 10% of video duration (or first frame)
            var frameIndex = Math.Min((int)(collection.Count * 0.1), collection.Count - 1);
            var frame = collection[frameIndex];

            // Resize to thumbnail dimensions with high quality
            frame.Resize(new MagickGeometry((uint)width, (uint)height)
            {
                FillArea = true,
                IgnoreAspectRatio = false
            });

            // Enhance quality (slightly reduced to save disk)
            frame.Quality = 80;
            frame.FilterType = FilterType.Lanczos;
            frame.Write(thumbnailPath);

            return thumbnailPath;
        }
        catch
        {
            // Fallback: try using first frame directly
            try
            {
                using var image = new MagickImage(videoPath);
                image.Resize(new MagickGeometry((uint)width, (uint)height) 
                { 
                    FillArea = true,
                    IgnoreAspectRatio = false
                });
                image.Quality = 80;
                image.FilterType = FilterType.Lanczos;
                image.Write(thumbnailPath);
                return thumbnailPath;
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Enforces a maximum total size for the thumbnails cache directory by deleting oldest files.
    /// </summary>
    /// <param name="maxBytes">Maximum allowed total size in bytes</param>
    private void EnforceCacheSizeLimit(long maxBytes)
    {
        try
        {
            if (!Directory.Exists(_thumbnailsDirectory))
                return;

            var files = new DirectoryInfo(_thumbnailsDirectory).GetFiles("*.jpg");
            long total = 0;
            foreach (var f in files)
                total += f.Length;

            if (total <= maxBytes)
                return;

            // Delete oldest first until under limit
            foreach (var file in files.OrderBy(f => f.LastWriteTime))
            {
                try
                {
                    file.Delete();
                    total -= file.Length;
                    if (total <= maxBytes)
                        break;
                }
                catch
                {
                    // Ignore deletion failures
                }
            }
        }
        catch
        {
            // Ignore enforcement errors
        }
    }

    /// <summary>
    /// Gets a simple hash of the file for cache key.
    /// </summary>
    private string GetFileHash(string filePath)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);
            // Use file size and last write time as hash
            return $"{fileInfo.Length}_{fileInfo.LastWriteTime.Ticks}".GetHashCode().ToString("X");
        }
        catch
        {
            return Path.GetFileName(filePath).GetHashCode().ToString("X");
        }
    }

    /// <summary>
    /// Clears old thumbnails that are no longer needed.
    /// </summary>
    public void CleanupOldThumbnails(int daysOld = 30)
    {
        try
        {
            if (!Directory.Exists(_thumbnailsDirectory))
                return;

            var cutoffDate = DateTime.Now.AddDays(-daysOld);
            var files = Directory.GetFiles(_thumbnailsDirectory, "*.jpg");

            foreach (var file in files)
            {
                try
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.LastWriteTime < cutoffDate)
                    {
                        File.Delete(file);
                    }
                }
                catch
                {
                    // Ignore individual file errors
                }
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}

