using AnimeQuoteWall.Core.Services;
using Xunit;

namespace AnimeQuoteWall.Core.Tests;

/// <summary>
/// Wallpaper Engine control-argument construction. As with ffmpeg, the video path must survive
/// as exactly one argument no matter what it contains.
/// </summary>
public class WallpaperEngineArgumentsTests
{
    [Fact]
    public void BuildApplyVariants_FirstVariantUsesOpenWallpaper()
    {
        var variants = WallpaperEngineArguments.BuildApplyVariants(@"C:\clip.mp4");

        Assert.Equal(
            new[] { "-control", "openWallpaper", "-file", @"C:\clip.mp4" },
            variants[0]);
    }

    [Fact]
    public void BuildApplyVariants_ReturnsThreeFallbacks()
    {
        var variants = WallpaperEngineArguments.BuildApplyVariants(@"C:\clip.mp4");

        Assert.Equal(3, variants.Count);
    }

    [Fact]
    public void BuildApplyVariants_EveryVariantCarriesThePath()
    {
        string video = @"C:\my videos\clip.mp4";

        var variants = WallpaperEngineArguments.BuildApplyVariants(video);

        Assert.All(variants, v => Assert.Single(v, a => a == video));
    }

    [Fact]
    public void BuildApplyVariants_PathContainingInjectionAttempt_StaysOneArgument()
    {
        string hostile = @"C:\clip.mp4" + "\" -control closeWallpaper";

        var variants = WallpaperEngineArguments.BuildApplyVariants(hostile);

        Assert.All(variants, v => Assert.Single(v, a => a == hostile));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BuildApplyVariants_BlankPath_Throws(string? videoPath)
    {
        Assert.Throws<ArgumentException>(() => WallpaperEngineArguments.BuildApplyVariants(videoPath!));
    }

    [Fact]
    public void BuildStopVariants_ReturnsThreeFallbacks()
    {
        var variants = WallpaperEngineArguments.BuildStopVariants();

        Assert.Equal(3, variants.Count);
    }

    [Fact]
    public void BuildStopVariants_FirstVariantUsesCloseWallpaper()
    {
        var variants = WallpaperEngineArguments.BuildStopVariants();

        Assert.Equal(new[] { "-control", "closeWallpaper" }, variants[0]);
    }

    [Fact]
    public void BuildStopVariants_CarriesNoFileArgument()
    {
        var variants = WallpaperEngineArguments.BuildStopVariants();

        Assert.All(variants, v => Assert.DoesNotContain("-file", v));
    }
}
