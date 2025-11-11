using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AnimeQuoteWall.Core.Services;

/// <summary>
/// Service for caching images to improve performance and reduce memory usage.
/// Implements LRU (Least Recently Used) cache eviction strategy.
/// 
/// This service helps reduce disk I/O by keeping frequently used images in memory.
/// When an image is requested, it first checks the cache. If found, it returns the cached version.
/// If not found, it loads the image from disk, caches it, and returns it.
/// 
/// The cache automatically evicts least recently used items when:
/// - The maximum number of cached items is reached
/// - The maximum memory usage is exceeded
/// </summary>
public class ImageCacheService : IDisposable
{
    /// <summary>
    /// Singleton instance of the image cache service.
    /// Using singleton pattern ensures all parts of the application share the same cache.
    /// </summary>
    private static ImageCacheService? _instance;
    
    /// <summary>
    /// Gets the singleton instance of the ImageCacheService.
    /// Creates a new instance if one doesn't exist.
    /// </summary>
    public static ImageCacheService Instance => _instance ??= new ImageCacheService();

    /// <summary>
    /// Dictionary storing cached images, keyed by cache key (path + size).
    /// </summary>
    private readonly Dictionary<string, CachedImage> _cache = new();
    
    /// <summary>
    /// Lock object for thread-safe access to the cache dictionary.
    /// </summary>
    private readonly object _lock = new();
    
    /// <summary>
    /// Maximum number of images to cache before eviction starts.
    /// </summary>
    private readonly int _maxCacheSize;
    
    /// <summary>
    /// Maximum memory usage in bytes before eviction starts.
    /// </summary>
    private readonly long _maxMemoryBytes;
    
    /// <summary>
    /// Current memory usage in bytes by all cached images.
    /// </summary>
    private long _currentMemoryUsage;

    /// <summary>
    /// Default maximum number of images to cache.
    /// </summary>
    private const int DefaultMaxCacheSize = 50;
    
    /// <summary>
    /// Default maximum memory usage: 500MB.
    /// </summary>
    private const long DefaultMaxMemoryBytes = 500 * 1024 * 1024; // 500MB

    /// <summary>
    /// Initializes a new instance of the ImageCacheService.
    /// </summary>
    /// <param name="maxCacheSize">Maximum number of images to cache (default: 50)</param>
    /// <param name="maxMemoryBytes">Maximum memory usage in bytes (default: 500MB)</param>
    public ImageCacheService(int maxCacheSize = DefaultMaxCacheSize, long maxMemoryBytes = DefaultMaxMemoryBytes)
    {
        _maxCacheSize = maxCacheSize;
        _maxMemoryBytes = maxMemoryBytes;
    }

    /// <summary>
    /// Gets a cached image or loads it asynchronously if not in cache.
    /// This method is thread-safe and will return a cloned bitmap to prevent cache corruption.
    /// </summary>
    /// <param name="imagePath">Path to the image file</param>
    /// <param name="width">Optional target width for resizing</param>
    /// <param name="height">Optional target height for resizing</param>
    /// <returns>Cached or newly loaded bitmap, or null if loading fails</returns>
    public async Task<Bitmap?> GetOrLoadImageAsync(string imagePath, int? width = null, int? height = null)
    {
        if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
            return null;

        var cacheKey = GetCacheKey(imagePath, width, height);

        lock (_lock)
        {
            if (_cache.TryGetValue(cacheKey, out var cached))
            {
                // Update access time for LRU
                cached.LastAccessed = DateTime.UtcNow;
                return CloneBitmap(cached.Image);
            }
        }

        // Load image asynchronously
        var bitmap = await Task.Run(() => LoadImage(imagePath, width, height)).ConfigureAwait(false);
        
        if (bitmap == null)
            return null;

        lock (_lock)
        {
            // Check if we need to evict items
            EvictIfNeeded();

            // Calculate memory usage
            var memoryUsage = EstimateMemoryUsage(bitmap);
            
            // Don't cache if single image exceeds memory limit
            if (memoryUsage > _maxMemoryBytes)
            {
                return bitmap; // Return without caching
            }

            // Add to cache
            _cache[cacheKey] = new CachedImage
            {
                Image = CloneBitmap(bitmap),
                LastAccessed = DateTime.UtcNow,
                MemoryUsage = memoryUsage
            };

            _currentMemoryUsage += memoryUsage;
        }

        return bitmap;
    }

    /// <summary>
    /// Gets a cached image synchronously (for compatibility).
    /// </summary>
    public Bitmap? GetOrLoadImage(string imagePath, int? width = null, int? height = null)
    {
        if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
            return null;

        var cacheKey = GetCacheKey(imagePath, width, height);

        lock (_lock)
        {
            if (_cache.TryGetValue(cacheKey, out var cached))
            {
                cached.LastAccessed = DateTime.UtcNow;
                return CloneBitmap(cached.Image);
            }
        }

        // Load synchronously
        var bitmap = LoadImage(imagePath, width, height);
        
        if (bitmap == null)
            return null;

        lock (_lock)
        {
            EvictIfNeeded();

            var memoryUsage = EstimateMemoryUsage(bitmap);
            
            if (memoryUsage > _maxMemoryBytes)
            {
                return bitmap;
            }

            _cache[cacheKey] = new CachedImage
            {
                Image = CloneBitmap(bitmap),
                LastAccessed = DateTime.UtcNow,
                MemoryUsage = memoryUsage
            };

            _currentMemoryUsage += memoryUsage;
        }

        return bitmap;
    }

    /// <summary>
    /// Clears the cache and frees memory.
    /// </summary>
    public void ClearCache()
    {
        lock (_lock)
        {
            foreach (var item in _cache.Values)
            {
                item.Image?.Dispose();
            }
            _cache.Clear();
            _currentMemoryUsage = 0;
            GC.Collect();
        }
    }

    /// <summary>
    /// Removes a specific image from cache.
    /// </summary>
    public void RemoveFromCache(string imagePath, int? width = null, int? height = null)
    {
        var cacheKey = GetCacheKey(imagePath, width, height);
        
        lock (_lock)
        {
            if (_cache.TryGetValue(cacheKey, out var cached))
            {
                _currentMemoryUsage -= cached.MemoryUsage;
                cached.Image?.Dispose();
                _cache.Remove(cacheKey);
            }
        }
    }

    private string GetCacheKey(string imagePath, int? width, int? height)
    {
        var normalizedPath = Path.GetFullPath(imagePath).ToLowerInvariant();
        var sizeKey = width.HasValue || height.HasValue ? $"{width}x{height}" : "original";
        return $"{normalizedPath}|{sizeKey}";
    }

    private Bitmap? LoadImage(string imagePath, int? width, int? height)
    {
        try
        {
            using var original = Image.FromFile(imagePath);
            
            if (!width.HasValue && !height.HasValue)
            {
                // Return original size
                return CloneBitmap((Bitmap)original);
            }

            var targetWidth = width ?? original.Width;
            var targetHeight = height ?? original.Height;

            // Maintain aspect ratio if only one dimension specified
            if (width.HasValue && !height.HasValue)
            {
                var ratio = (double)original.Height / original.Width;
                targetHeight = (int)(targetWidth * ratio);
            }
            else if (!width.HasValue && height.HasValue)
            {
                var ratio = (double)original.Width / original.Height;
                targetWidth = (int)(targetHeight * ratio);
            }

            var resized = new Bitmap(targetWidth, targetHeight);
            using var graphics = System.Drawing.Graphics.FromImage(resized);
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            
            graphics.DrawImage(original, 0, 0, targetWidth, targetHeight);
            return resized;
        }
        catch
        {
            return null;
        }
    }

    private Bitmap CloneBitmap(Bitmap source)
    {
        return new Bitmap(source);
    }

    private long EstimateMemoryUsage(Bitmap bitmap)
    {
        // Rough estimate: width * height * 4 bytes (RGBA)
        return (long)bitmap.Width * bitmap.Height * 4;
    }

    private void EvictIfNeeded()
    {
        // Evict by count
        while (_cache.Count >= _maxCacheSize)
        {
            var oldest = _cache.OrderBy(kvp => kvp.Value.LastAccessed).First();
            _currentMemoryUsage -= oldest.Value.MemoryUsage;
            oldest.Value.Image?.Dispose();
            _cache.Remove(oldest.Key);
        }

        // Evict by memory
        while (_currentMemoryUsage > _maxMemoryBytes && _cache.Count > 0)
        {
            var oldest = _cache.OrderBy(kvp => kvp.Value.LastAccessed).First();
            _currentMemoryUsage -= oldest.Value.MemoryUsage;
            oldest.Value.Image?.Dispose();
            _cache.Remove(oldest.Key);
        }
    }

    /// <summary>
    /// Disposes of all cached images and frees memory.
    /// </summary>
    public void Dispose()
    {
        ClearCache();
    }

    /// <summary>
    /// Internal class representing a cached image with metadata.
    /// </summary>
    private class CachedImage
    {
        /// <summary>
        /// The cached bitmap image.
        /// </summary>
        public Bitmap Image { get; set; } = null!;
        
        /// <summary>
        /// Timestamp of last access (for LRU eviction).
        /// </summary>
        public DateTime LastAccessed { get; set; }
        
        /// <summary>
        /// Estimated memory usage in bytes.
        /// </summary>
        public long MemoryUsage { get; set; }
    }
}

