using AnimeQuoteWall.Core.Services;
using Xunit;

namespace AnimeQuoteWall.Core.Tests;

/// <summary>
/// ffmpeg argument-vector construction. The point of these tests is that a hostile filename
/// stays exactly one argument: if any of them starts failing because a path was split or
/// concatenated, argument injection is back (CLAUDE.md Section 14).
/// </summary>
public class FfmpegArgumentsTests
{
    [Fact]
    public void BuildFirstFrameExtraction_ProducesExpectedVector()
    {
        var args = FfmpegArguments.BuildFirstFrameExtraction(@"C:\videos\clip.mp4", @"C:\frames\out.png");

        Assert.Equal(
            new[] { "-i", @"C:\videos\clip.mp4", "-vframes", "1", "-y", @"C:\frames\out.png" },
            args);
    }

    [Fact]
    public void BuildFirstFrameExtraction_PathWithSpaces_StaysOneArgument()
    {
        string video = @"C:\my videos\my clip.mp4";

        var args = FfmpegArguments.BuildFirstFrameExtraction(video, @"C:\out.png");

        Assert.Single(args, a => a == video);
    }

    [Fact]
    public void BuildFirstFrameExtraction_PathContainingInjectionAttempt_StaysOneArgument()
    {
        // If this were concatenated into a command string, "-vf" would become a real argument.
        string hostile = @"C:\clip.mp4" + "\" -vf \"drawtext=text='pwned'";

        var args = FfmpegArguments.BuildFirstFrameExtraction(hostile, @"C:\out.png");

        Assert.Single(args, a => a == hostile);
    }

    [Fact]
    public void BuildFirstFrameExtraction_OutputIsLastArgument()
    {
        var args = FfmpegArguments.BuildFirstFrameExtraction(@"C:\clip.mp4", @"C:\out.png");

        Assert.Equal(@"C:\out.png", args[^1]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BuildFirstFrameExtraction_BlankVideoPath_Throws(string? videoPath)
    {
        Assert.Throws<ArgumentException>(
            () => FfmpegArguments.BuildFirstFrameExtraction(videoPath!, @"C:\out.png"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BuildFirstFrameExtraction_BlankFramePath_Throws(string? framePath)
    {
        Assert.Throws<ArgumentException>(
            () => FfmpegArguments.BuildFirstFrameExtraction(@"C:\clip.mp4", framePath!));
    }

    [Fact]
    public void BuildAnimationEncode_ProducesExpectedVector()
    {
        var args = FfmpegArguments.BuildAnimationEncode(@"C:\tmp\frame_%03d.png", 30, @"C:\out.mp4");

        Assert.Equal(
            new[]
            {
                "-y",
                "-framerate", "30",
                "-i", @"C:\tmp\frame_%03d.png",
                "-pix_fmt", "yuv420p",
                "-crf", "18",
                "-preset", "veryfast",
                @"C:\out.mp4"
            },
            args);
    }

    [Fact]
    public void BuildAnimationEncode_FrameRateIsInvariantCulture()
    {
        var original = Thread.CurrentThread.CurrentCulture;
        try
        {
            // A culture whose number formatting differs must not leak into the argument.
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("de-DE");

            var args = FfmpegArguments.BuildAnimationEncode(@"C:\f_%03d.png", 24, @"C:\out.mp4");

            Assert.Equal("24", args[2]);
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = original;
        }
    }

    [Fact]
    public void BuildAnimationEncode_OutputIsLastArgument()
    {
        var args = FfmpegArguments.BuildAnimationEncode(@"C:\f_%03d.png", 30, @"C:\out.mp4");

        Assert.Equal(@"C:\out.mp4", args[^1]);
    }

    [Fact]
    public void BuildAnimationEncode_PathWithSpaces_StaysOneArgument()
    {
        string output = @"C:\my videos\my export.mp4";

        var args = FfmpegArguments.BuildAnimationEncode(@"C:\f_%03d.png", 30, output);

        Assert.Single(args, a => a == output);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void BuildAnimationEncode_NonPositiveFrameRate_Throws(int fps)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => FfmpegArguments.BuildAnimationEncode(@"C:\f_%03d.png", fps, @"C:\out.mp4"));
    }

    [Fact]
    public void BuildAnimationEncode_NoArgumentContainsAFilterGraph()
    {
        // Filter graphs must be constants, never derived from caller input (CLAUDE.md Section 14).
        var args = FfmpegArguments.BuildAnimationEncode(@"C:\f_%03d.png", 30, @"C:\out.mp4");

        Assert.DoesNotContain("-vf", args);
        Assert.DoesNotContain("-filter_complex", args);
    }
}
