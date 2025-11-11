using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AnimeQuoteWall.Core.Models;
using AnimeQuoteWall.Core.Protection;
using ImageMagick;
using System.Diagnostics;

namespace AnimeQuoteWall.Core.Services;

/// <summary>
/// Service for generating animation frames and exporting animated wallpapers.
/// 
/// This service handles:
/// - Generating multiple frames for animations (GIF/MP4)
/// - Applying animation effects (fade, slide, etc.)
/// - Exporting frames to GIF format using ImageMagick
/// - Exporting frames to MP4 format using FFmpeg
/// 
/// The service uses easing functions to create smooth animations and properly
/// disposes of bitmaps to manage memory efficiently.
/// </summary>
public class AnimationService
{
    /// <summary>
    /// Reference to the wallpaper service for creating individual frames.
    /// </summary>
    private readonly WallpaperService _wallpaperService;

    /// <summary>
    /// Initializes a new instance of the AnimationService.
    /// </summary>
    public AnimationService()
    {
        _wallpaperService = new WallpaperService();
    }

    /// <summary>
    /// Generates animation frames for creating animated wallpapers.
    /// Protected method - proprietary animation algorithm.
    /// 
    /// Process:
    /// 1. Calculate total frames needed (fps * duration)
    /// 2. For each frame:
    ///    - Calculate animation progress (0.0 to 1.0)
    ///    - Apply easing function for smooth motion
    ///    - Create wallpaper image with animation effects
    ///    - Save frame to disk
    ///    - Dispose bitmap to free memory
    /// 3. Return list of frame file paths
    /// 
    /// </summary>
    /// <param name="backgroundPath">Path to background image</param>
    /// <param name="quote">Quote to animate</param>
    /// <param name="settings">Wallpaper visual settings</param>
    /// <param name="profile">Animation profile (fps, duration, easing, motion type)</param>
    /// <param name="outputDirectory">Directory to save frame images</param>
    /// <param name="progress">Progress reporter (0.0 to 1.0)</param>
    /// <param name="cancellationToken">Cancellation token for canceling generation</param>
    /// <returns>List of paths to generated frame images</returns>
    [System.Diagnostics.DebuggerStepThrough]
    public async Task<IReadOnlyList<string>> GenerateFramesAsync(
        string? backgroundPath,
        Quote quote,
        WallpaperSettings settings,
        AnimationProfile profile,
        string outputDirectory,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        // Calculate total frames needed: frames per second * duration in seconds
        int totalFrames = profile.FramesPerSecond * profile.DurationSeconds;
        var frames = new List<string>(capacity: totalFrames);

        // Ensure output directory exists
        Directory.CreateDirectory(outputDirectory);

        // Generate each frame
        for (int frameIndex = 0; frameIndex < totalFrames; frameIndex++)
        {
            // Check for cancellation request
            cancellationToken.ThrowIfCancellationRequested();

            // Calculate animation progress: 0.0 (start) to 1.0 (end)
            float t = totalFrames <= 1 ? 0f : frameIndex / (float)(totalFrames - 1);
            // Apply easing function for smooth motion (easeIn, easeOut, linear)
            float eased = ApplyEasing(t, profile.EasingType);

            Bitmap? bitmap = null;
            try
            {
                // Create wallpaper image for this frame with enhanced features
                bitmap = CreateWallpaperImageWithEffects(backgroundPath, quote, settings, profile, eased);
                
                // Apply particle and other overlay effects
                ApplyEnhancedAnimationEffects(bitmap, settings, profile, eased, null);

                // Save frame to disk with zero-padded filename (frame_000.png, frame_001.png, etc.)
                string framePath = Path.Combine(outputDirectory, $"frame_{frameIndex:D3}.png");
                await _wallpaperService.SaveImageAsync(bitmap, framePath).ConfigureAwait(false);

                frames.Add(framePath);
                // Report progress (0.0 to 1.0)
                progress?.Report((frameIndex + 1) / (double)totalFrames);
            }
            finally
            {
                // Dispose bitmap immediately after saving to free memory
                // This is critical for large animations to prevent memory issues
                bitmap?.Dispose();
            }
        }

        return frames;
    }

    /// <summary>
    /// Exports animation frames to a GIF file using ImageMagick.
    /// 
    /// Process:
    /// 1. Load all frame images into MagickImageCollection
    /// 2. Set frame delay based on FPS
    /// 3. Configure looping if requested
    /// 4. Optimize collection to reduce file size
    /// 5. Write GIF file
    /// 
    /// </summary>
    /// <param name="frames">List of paths to frame images</param>
    /// <param name="outputPath">Path where GIF file will be saved</param>
    /// <param name="profile">Animation profile containing FPS and loop settings</param>
    /// <param name="progress">Progress reporter (0.0 to 1.0)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task ExportGifAsync(
        IReadOnlyList<string> frames,
        string outputPath,
        AnimationProfile profile,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (frames == null || frames.Count == 0)
            throw new ArgumentException("No frames to export.");

        // Calculate frame delay in hundredths of a second
        // Example: 24 fps = 100/24 ≈ 4.17 hundredths per frame
        int delayInHundredths = (int)Math.Max(1, 100.0 / profile.FramesPerSecond);

        // Create image collection for GIF
        using var collection = new MagickImageCollection();

        // Load each frame into the collection
        for (int i = 0; i < frames.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var framePath = frames[i];
            MagickImage? image = null;
            try
            {
                image = new MagickImage(framePath);

                // Set frame delay for GIF animation timing
                image.AnimationDelay = (uint)delayInHundredths;

                // Set quality to reduce file size (90 is a good balance)
                image.Quality = 90;

                // Add to collection (collection takes ownership)
                collection.Add(image);
                image = null; // Don't dispose, collection owns it now
                progress?.Report((i + 1) / (double)frames.Count);
            }
            catch
            {
                image?.Dispose();
                throw;
            }
        }

        // Configure looping: 0 = infinite loop, 1+ = loop count
        if (profile.Loop)
        {
            collection[0].AnimationIterations = 0; // Infinite loop
        }

        // Optimize collection to reduce GIF file size
        // This removes duplicate pixels and optimizes color palette
        collection.Optimize();

        // Write GIF file asynchronously
        await Task.Run(() =>
        {
            collection.Write(outputPath);
        }, cancellationToken).ConfigureAwait(false);
    }

    // Step 3: MP4 export using ffmpeg
    public async Task ExportMp4Async(
        IReadOnlyList<string> frames,
        string outputPath,
        AnimationProfile profile,
        string ffmpegPath,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (frames == null || frames.Count == 0)
            throw new ArgumentException("No frames to export.");

        if (string.IsNullOrWhiteSpace(ffmpegPath) || !File.Exists(ffmpegPath))
            throw new FileNotFoundException("FFmpeg executable not found.", ffmpegPath);

        string tempDir = Path.Combine(Path.GetTempPath(), "AnimeQuoteFrames_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            // Copy frames into temp with sequential naming
            for (int i = 0; i < frames.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string dest = Path.Combine(tempDir, $"frame_{i:D3}.png");
                File.Copy(frames[i], dest, overwrite: true);
                progress?.Report((i + 1) / (double)frames.Count * 0.5); // first half progress
            }

            // ffmpeg command
            string inputPattern = Path.Combine(tempDir, "frame_%03d.png");
            string arguments = $"-y -framerate {profile.FramesPerSecond} -i \"{inputPattern}\" -pix_fmt yuv420p -crf 18 -preset veryfast \"{outputPath}\"";

            var psi = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = psi };
            process.Start();

            // Optionally read output to avoid deadlocks
            var stdOutTask = process.StandardOutput.ReadToEndAsync();
            var stdErrTask = process.StandardError.ReadToEndAsync();

            // Rough progress bump during encode (second half)
            progress?.Report(0.75);

            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

            // Complete reading
            await Task.WhenAll(stdOutTask, stdErrTask).ConfigureAwait(false);

            if (process.ExitCode != 0)
            {
                string err = await stdErrTask.ConfigureAwait(false);
                throw new InvalidOperationException($"FFmpeg failed (code {process.ExitCode}). {err}");
            }

            progress?.Report(1.0);
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { /* ignore cleanup errors */ }
        }
    }

    // Simple easing for beginners (works with arrays/methods)
    private float ApplyEasing(float t, string easing)
    {
        // keep it simple, you can add more later
        // t is 0..1
        if (easing.Equals("easeIn", StringComparison.OrdinalIgnoreCase)) return t * t;
        if (easing.Equals("easeOut", StringComparison.OrdinalIgnoreCase)) return 1f - (1f - t) * (1f - t);
        return t; // linear
    }

    // A stub to apply animation effects to the bitmap
    private void ApplyAnimationEffects(System.Drawing.Bitmap bitmap, WallpaperSettings settings, AnimationProfile profile, float eased)
    {
        // Legacy method - kept for compatibility
        ApplyEnhancedAnimationEffects(bitmap, settings, profile, eased, null);
    }

    /// <summary>
    /// Creates a wallpaper image with enhanced animation effects.
    /// </summary>
    private System.Drawing.Bitmap CreateWallpaperImageWithEffects(string? backgroundPath, Quote quote, WallpaperSettings settings, AnimationProfile profile, float eased)
    {
        // Load background
        using var background = _wallpaperService.LoadBackgroundBitmap(backgroundPath, settings);
        var bitmap = new System.Drawing.Bitmap(background.Width, background.Height);
        
        using var graphics = System.Drawing.Graphics.FromImage(bitmap);
        WallpaperService.ConfigureGraphicsQuality(graphics);
        
        // Draw background
        graphics.DrawImage(background, 0, 0, bitmap.Width, bitmap.Height);
        
        // Draw quote with enhanced animations
        _wallpaperService.DrawAnimatedQuote(graphics, quote, bitmap.Width, bitmap.Height, settings, eased, 
            profile.TextAnimationType, profile.MotionEffects);
        
        return bitmap;
    }

    /// <summary>
    /// Applies enhanced animation effects including particles, interactive effects, and text animations.
    /// </summary>
    private void ApplyEnhancedAnimationEffects(System.Drawing.Bitmap bitmap, WallpaperSettings settings, AnimationProfile profile, float eased, System.Drawing.Graphics? graphics)
    {
        if (bitmap == null) return;

        using var g = graphics ?? System.Drawing.Graphics.FromImage(bitmap);
        if (graphics == null)
        {
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
        }

        // Apply particle effects if enabled
        if (profile.ParticleSettings != null && profile.ParticleSettings.Enabled && profile.ParticleSettings.ParticleType != "None")
        {
            var particleService = new ParticleSystemService();
            var emitter = new ParticleEmitter
            {
                ParticleType = profile.ParticleSettings.ParticleType,
                SpawnRate = profile.ParticleSettings.ParticleCount / 10f, // Adjust spawn rate based on count
                SpawnArea = new System.Drawing.RectangleF(0, 0, bitmap.Width, bitmap.Height),
                ParticleColor = profile.ParticleSettings.ParticleColor,
                MinSpeed = profile.ParticleSettings.ParticleSpeed * 0.5f,
                MaxSpeed = profile.ParticleSettings.ParticleSpeed
            };
            
            // Update particles (deltaTime based on frame progress)
            var deltaTime = 1f / profile.FramesPerSecond;
            particleService.UpdateParticles(deltaTime, bitmap.Width, bitmap.Height, emitter);
            particleService.DrawParticles(g);
        }
    }
}
