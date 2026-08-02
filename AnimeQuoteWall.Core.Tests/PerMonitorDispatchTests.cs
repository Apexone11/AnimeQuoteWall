using AnimeQuoteWall.Core.Services;
using Xunit;

namespace AnimeQuoteWall.Core.Tests;

/// <summary>
/// Per-monitor dispatch resolution. The wallpaper is never actually applied here; only the
/// index-to-device-path decision is under test (CLAUDE.md Section 9).
/// </summary>
public class PerMonitorDispatchTests
{
    private static readonly string[] ThreeMonitors =
    {
        @"\\.\DISPLAY1",
        @"\\.\DISPLAY2",
        @"\\.\DISPLAY3"
    };

    private static bool AllExist(string _) => true;
    private static bool NoneExist(string _) => false;

    [Fact]
    public void Resolve_SingleValidEntry_MapsIndexToDevicePath()
    {
        var map = new Dictionary<int, string> { [1] = @"C:\a.png" };

        var result = PerMonitorDispatch.Resolve(map, ThreeMonitors, AllExist);

        Assert.Equal(@"\\.\DISPLAY2", Assert.Single(result).MonitorDevicePath);
    }

    [Fact]
    public void Resolve_AllMonitorsAssigned_ReturnsOnePerMonitor()
    {
        var map = new Dictionary<int, string>
        {
            [0] = @"C:\a.png",
            [1] = @"C:\b.png",
            [2] = @"C:\c.png"
        };

        var result = PerMonitorDispatch.Resolve(map, ThreeMonitors, AllExist);

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void Resolve_UnorderedKeys_ReturnsAscendingMonitorOrder()
    {
        var map = new Dictionary<int, string>
        {
            [2] = @"C:\c.png",
            [0] = @"C:\a.png",
            [1] = @"C:\b.png"
        };

        var result = PerMonitorDispatch.Resolve(map, ThreeMonitors, AllExist);

        Assert.Equal(new[] { 0, 1, 2 }, result.Select(a => a.MonitorIndex));
    }

    [Fact]
    public void Resolve_IndexBeyondMonitorCount_IsSkipped()
    {
        var map = new Dictionary<int, string> { [7] = @"C:\a.png" };

        var result = PerMonitorDispatch.Resolve(map, ThreeMonitors, AllExist);

        Assert.Empty(result);
    }

    [Fact]
    public void Resolve_NegativeIndex_IsSkipped()
    {
        var map = new Dictionary<int, string> { [-1] = @"C:\a.png" };

        var result = PerMonitorDispatch.Resolve(map, ThreeMonitors, AllExist);

        Assert.Empty(result);
    }

    [Fact]
    public void Resolve_MissingImageFile_IsSkipped()
    {
        var map = new Dictionary<int, string> { [0] = @"C:\gone.png" };

        var result = PerMonitorDispatch.Resolve(map, ThreeMonitors, NoneExist);

        Assert.Empty(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Resolve_BlankImagePath_IsSkipped(string wallpaperPath)
    {
        var map = new Dictionary<int, string> { [0] = wallpaperPath };

        var result = PerMonitorDispatch.Resolve(map, ThreeMonitors, AllExist);

        Assert.Empty(result);
    }

    [Fact]
    public void Resolve_OneBadEntry_DoesNotDropTheGoodOnes()
    {
        var map = new Dictionary<int, string>
        {
            [0] = @"C:\a.png",
            [9] = @"C:\out-of-range.png",
            [2] = @"C:\c.png"
        };

        var result = PerMonitorDispatch.Resolve(map, ThreeMonitors, AllExist);

        Assert.Equal(new[] { 0, 2 }, result.Select(a => a.MonitorIndex));
    }

    [Fact]
    public void Resolve_RelativeImagePath_IsMadeAbsolute()
    {
        var map = new Dictionary<int, string> { [0] = "relative.png" };

        var result = PerMonitorDispatch.Resolve(map, ThreeMonitors, AllExist);

        Assert.Equal(Path.GetFullPath("relative.png"), Assert.Single(result).WallpaperPath);
    }

    [Fact]
    public void Resolve_NoMonitorsAvailable_ReturnsEmpty()
    {
        var map = new Dictionary<int, string> { [0] = @"C:\a.png" };

        var result = PerMonitorDispatch.Resolve(map, Array.Empty<string>(), AllExist);

        Assert.Empty(result);
    }

    [Fact]
    public void Resolve_EmptyMap_ReturnsEmpty()
    {
        var result = PerMonitorDispatch.Resolve(new Dictionary<int, string>(), ThreeMonitors, AllExist);

        Assert.Empty(result);
    }

    [Fact]
    public void Resolve_NullMap_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => PerMonitorDispatch.Resolve(null!, ThreeMonitors, AllExist));
    }

    [Fact]
    public void Resolve_NullDevicePaths_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => PerMonitorDispatch.Resolve(new Dictionary<int, string>(), null!, AllExist));
    }

    [Fact]
    public void Resolve_NullFileExistsProbe_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => PerMonitorDispatch.Resolve(new Dictionary<int, string>(), ThreeMonitors, null!));
    }
}
