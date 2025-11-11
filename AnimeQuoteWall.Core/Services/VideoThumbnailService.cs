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
    /// <param name="width">Thumbnail width (default: 320)</param>
    /// <param name="height">Thumbnail height (default: 180)</param>
    /// <returns>Path to the thumbnail image, or null if generation failed</returns>
    public string? GetOrCreateThumbnail(string videoPath, int width = 320, int height = 180)
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
            return GenerateThumbnail(videoPath, thumbnailPath, width, height);
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
            frame.Resize(new MagickGeometry(width, height)
            {
                FillArea = true,
                IgnoreAspectRatio = false
            });

            // Enhance quality
            frame.Quality = 90;
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
                image.Resize(new MagickGeometry(width, height) 
                { 
                    FillArea = true,
                    IgnoreAspectRatio = false
                });
                image.Quality = 90;
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

