using AnimeQuoteWall.Core.Services;
using Xunit;

namespace AnimeQuoteWall.Core.Tests;

/// <summary>
/// Identifier sanitization. Playlist and quote ids reach the filesystem as filename stems, so
/// separators and traversal sequences must not survive.
/// </summary>
public class SafePathSanitizeIdTests
{
    [Fact]
    public void SanitizeId_AlreadySafeId_IsUnchanged()
    {
        string result = SafePath.SanitizeId("playlist-01_Main");

        Assert.Equal("playlist-01_Main", result);
    }

    [Fact]
    public void SanitizeId_StripsPathSeparators()
    {
        string result = SafePath.SanitizeId(@"sub\dir/id");

        Assert.Equal("subdirid", result);
    }

    [Fact]
    public void SanitizeId_StripsTraversalSequence()
    {
        string result = SafePath.SanitizeId("../../etc/passwd");

        Assert.Equal("etcpasswd", result);
    }

    [Fact]
    public void SanitizeId_StripsDriveColon()
    {
        string result = SafePath.SanitizeId(@"C:\Windows\System32");

        Assert.Equal("CWindowsSystem32", result);
    }

    [Fact]
    public void SanitizeId_StripsSpacesAndPunctuation()
    {
        string result = SafePath.SanitizeId("my playlist (2026)!");

        Assert.Equal("myplaylist2026", result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("...")]
    [InlineData("///")]
    public void SanitizeId_NothingSafeSurvives_ReturnsUnknown(string raw)
    {
        string result = SafePath.SanitizeId(raw);

        Assert.Equal("unknown", result);
    }

    [Fact]
    public void SanitizeId_LongInput_IsCappedAt64Characters()
    {
        string result = SafePath.SanitizeId(new string('a', 200));

        Assert.Equal(64, result.Length);
    }

    [Fact]
    public void SanitizeId_ResultContainsNoInvalidFileNameCharacters()
    {
        string result = SafePath.SanitizeId(@"a<b>c:d""e|f?g*h\i/j");

        Assert.Equal(-1, result.IndexOfAny(Path.GetInvalidFileNameChars()));
    }
}
