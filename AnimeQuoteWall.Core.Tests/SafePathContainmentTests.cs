using System.Security;
using AnimeQuoteWall.Core.Services;
using Xunit;

namespace AnimeQuoteWall.Core.Tests;

/// <summary>
/// Root-containment behaviour of <see cref="SafePath"/>. These are the guards standing between
/// a crafted history or playlist entry and an arbitrary-file write, so each escape shape gets
/// its own test.
/// </summary>
public class SafePathContainmentTests
{
    private static readonly string Root = Path.Combine(Path.GetTempPath(), "aqw-root");

    [Fact]
    public void IsInsideRoot_FileDirectlyInRoot_ReturnsTrue()
    {
        bool result = SafePath.IsInsideRoot(Path.Combine(Root, "wallpaper.png"), Root);

        Assert.True(result);
    }

    [Fact]
    public void IsInsideRoot_FileInNestedSubdirectory_ReturnsTrue()
    {
        bool result = SafePath.IsInsideRoot(Path.Combine(Root, "cache", "thumbs", "a.png"), Root);

        Assert.True(result);
    }

    [Fact]
    public void IsInsideRoot_RootItself_ReturnsTrue()
    {
        bool result = SafePath.IsInsideRoot(Root, Root);

        Assert.True(result);
    }

    [Fact]
    public void IsInsideRoot_TrailingSeparatorOnRoot_StillMatches()
    {
        bool result = SafePath.IsInsideRoot(Path.Combine(Root, "a.png"), Root + Path.DirectorySeparatorChar);

        Assert.True(result);
    }

    [Fact]
    public void IsInsideRoot_DotDotEscapingRoot_ReturnsFalse()
    {
        string escape = Path.Combine(Root, "..", "elsewhere", "evil.png");

        bool result = SafePath.IsInsideRoot(escape, Root);

        Assert.False(result);
    }

    [Fact]
    public void IsInsideRoot_DotDotThatStaysInsideRoot_ReturnsTrue()
    {
        string stillInside = Path.Combine(Root, "cache", "..", "wallpaper.png");

        bool result = SafePath.IsInsideRoot(stillInside, Root);

        Assert.True(result);
    }

    [Fact]
    public void IsInsideRoot_SiblingDirectoryWithRootAsNamePrefix_ReturnsFalse()
    {
        // "aqw-root-evil" starts with "aqw-root" as a string but is a different directory.
        // A naive StartsWith check without the separator test would accept this.
        bool result = SafePath.IsInsideRoot(Root + "-evil" + Path.DirectorySeparatorChar + "x.png", Root);

        Assert.False(result);
    }

    [Fact]
    public void IsInsideRoot_UnrelatedAbsolutePath_ReturnsFalse()
    {
        bool result = SafePath.IsInsideRoot(@"C:\Windows\System32\drivers\etc\hosts", Root);

        Assert.False(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void IsInsideRoot_BlankCandidate_ReturnsFalse(string candidate)
    {
        bool result = SafePath.IsInsideRoot(candidate, Root);

        Assert.False(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void IsInsideRoot_BlankRoot_ReturnsFalse(string root)
    {
        bool result = SafePath.IsInsideRoot(Path.Combine(Root, "a.png"), root);

        Assert.False(result);
    }

    [Fact]
    public void IsInsideRoot_DifferentCasing_ReturnsTrue()
    {
        bool result = SafePath.IsInsideRoot(Path.Combine(Root.ToUpperInvariant(), "A.PNG"), Root);

        Assert.True(result);
    }

    [Fact]
    public void RequireInsideRoot_PathInsideRoot_ReturnsFullPath()
    {
        string candidate = Path.Combine(Root, "cache", "..", "wallpaper.png");

        string result = SafePath.RequireInsideRoot(candidate, Root, "test-sink");

        Assert.Equal(Path.GetFullPath(candidate), result);
    }

    [Fact]
    public void RequireInsideRoot_PathOutsideRoot_ThrowsSecurityException()
    {
        string escape = Path.Combine(Root, "..", "elsewhere", "evil.png");

        Assert.Throws<SecurityException>(() => SafePath.RequireInsideRoot(escape, Root, "test-sink"));
    }

    [Fact]
    public void RequireInsideRoot_ThrowMessageNamesTheSink()
    {
        string escape = Path.Combine(Root, "..", "evil.png");

        var ex = Assert.Throws<SecurityException>(
            () => SafePath.RequireInsideRoot(escape, Root, "history-delete"));

        Assert.Contains("history-delete", ex.Message, StringComparison.Ordinal);
    }
}
