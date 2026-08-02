using System;
using System.Collections.Generic;

namespace AnimeQuoteWall.Core.Services;

/// <summary>
/// Builds the argument vectors handed to the Wallpaper Engine command-line executable.
/// </summary>
/// <remarks>
/// Wallpaper Engine has changed its control verbs between releases, so each builder returns
/// several variants to try in order. Like <see cref="FfmpegArguments"/>, these are pure values
/// so they can be asserted without spawning a process, and every element is a single argument
/// destined for <see cref="System.Diagnostics.ProcessStartInfo.ArgumentList"/>.
/// </remarks>
public static class WallpaperEngineArguments
{
    /// <summary>
    /// Builds the candidate argument vectors that apply a video as the active wallpaper.
    /// </summary>
    /// <param name="videoPath">Path to the video to apply.</param>
    /// <returns>Variants to attempt in order, most specific first.</returns>
    /// <exception cref="ArgumentException"><paramref name="videoPath"/> is null, empty, or whitespace.</exception>
    public static IReadOnlyList<IReadOnlyList<string>> BuildApplyVariants(string videoPath)
    {
        if (string.IsNullOrWhiteSpace(videoPath))
            throw new ArgumentException("Path must not be null or whitespace.", nameof(videoPath));

        return new IReadOnlyList<string>[]
        {
            new[] { "-control", "openWallpaper", "-file", videoPath },
            new[] { "-control", "applyWallpaper", "-file", videoPath },
            new[] { "-file", videoPath }
        };
    }

    /// <summary>
    /// Builds the candidate argument vectors that stop the active wallpaper.
    /// </summary>
    /// <returns>Variants to attempt in order.</returns>
    public static IReadOnlyList<IReadOnlyList<string>> BuildStopVariants()
    {
        return new IReadOnlyList<string>[]
        {
            new[] { "-control", "closeWallpaper" },
            new[] { "-control", "stopWallpaper" },
            new[] { "-control", "clearWallpaper" }
        };
    }
}
