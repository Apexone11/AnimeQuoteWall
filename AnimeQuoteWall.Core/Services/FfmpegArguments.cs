using System;
using System.Collections.Generic;
using System.Globalization;

namespace AnimeQuoteWall.Core.Services;

/// <summary>
/// Builds the argument vectors handed to the bundled ffmpeg binary.
/// </summary>
/// <remarks>
/// These live apart from the services that spawn ffmpeg so the argument vector is a pure,
/// testable value (CLAUDE.md Sections 9 and 14). Every element is one argument: callers must
/// copy them into <see cref="System.Diagnostics.ProcessStartInfo.ArgumentList"/> so the runtime
/// escapes each one, never into <see cref="System.Diagnostics.ProcessStartInfo.Arguments"/>,
/// which would let a crafted filename inject additional arguments.
/// Filter-graph strings are constants here and are never built from user input.
/// </remarks>
public static class FfmpegArguments
{
    /// <summary>
    /// Builds the argument vector that extracts the first frame of a video as a PNG.
    /// </summary>
    /// <param name="videoPath">Source video path.</param>
    /// <param name="framePath">Destination PNG path. Overwritten if it exists.</param>
    /// <returns>One argument per element, in ffmpeg's expected order.</returns>
    /// <exception cref="ArgumentException">A path is null, empty, or whitespace.</exception>
    public static IReadOnlyList<string> BuildFirstFrameExtraction(string videoPath, string framePath)
    {
        RequireValue(videoPath, nameof(videoPath));
        RequireValue(framePath, nameof(framePath));

        return new[]
        {
            "-i", videoPath,
            "-vframes", "1",
            "-y", framePath
        };
    }

    /// <summary>
    /// Builds the argument vector that encodes a numbered PNG frame sequence into an MP4.
    /// </summary>
    /// <param name="inputPattern">Printf-style frame pattern, for example <c>frame_%03d.png</c>.</param>
    /// <param name="framesPerSecond">Output frame rate. Must be greater than zero.</param>
    /// <param name="outputPath">Destination video path. Overwritten if it exists.</param>
    /// <returns>One argument per element, in ffmpeg's expected order.</returns>
    /// <exception cref="ArgumentException">A path is null, empty, or whitespace.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="framesPerSecond"/> is not positive.</exception>
    public static IReadOnlyList<string> BuildAnimationEncode(string inputPattern, int framesPerSecond, string outputPath)
    {
        RequireValue(inputPattern, nameof(inputPattern));
        RequireValue(outputPath, nameof(outputPath));
        if (framesPerSecond <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(framesPerSecond), framesPerSecond, "Frame rate must be greater than zero.");
        }

        return new[]
        {
            "-y",
            "-framerate", framesPerSecond.ToString(CultureInfo.InvariantCulture),
            "-i", inputPattern,
            "-pix_fmt", "yuv420p",
            "-crf", "18",
            "-preset", "veryfast",
            outputPath
        };
    }

    private static void RequireValue(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Path must not be null or whitespace.", parameterName);
    }
}
