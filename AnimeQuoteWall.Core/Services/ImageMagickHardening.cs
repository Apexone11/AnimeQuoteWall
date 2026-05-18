using System;
using ImageMagick;

namespace AnimeQuoteWall.Core.Services;

/// <summary>
/// Constrains ImageMagick at process startup so a single hostile image cannot exhaust
/// memory or disk. Call <see cref="Apply"/> once before any image is decoded. Coder
/// restrictions are enforced at the file-extension allowlist boundary in
/// <c>BackgroundService</c> and <c>SafePath</c>, since Magick.NET 14.x removed
/// programmatic access to the legacy policy XML.
/// </summary>
public static class ImageMagickHardening
{
    private static bool _applied;

    public static void Apply()
    {
        if (_applied) return;
        _applied = true;

        try
        {
            ResourceLimits.Width = 16_384;
            ResourceLimits.Height = 16_384;
            ResourceLimits.Memory = 512UL * 1024 * 1024;
            ResourceLimits.Disk = 1024UL * 1024 * 1024;
            ResourceLimits.Thread = (ulong)Math.Max(1, Environment.ProcessorCount / 2);
            ResourceLimits.Throttle = 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ImageMagickHardening.Apply: {ex.Message}");
        }
    }
}
