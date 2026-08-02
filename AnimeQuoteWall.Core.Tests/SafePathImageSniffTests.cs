using AnimeQuoteWall.Core.Services;
using Xunit;

namespace AnimeQuoteWall.Core.Tests;

/// <summary>
/// Magic-byte sniffing. Extension checks alone let a polyglot or renamed payload reach
/// ImageMagick and ffmpeg, so the header is what decides (CLAUDE.md Section 14).
/// </summary>
public class SafePathImageSniffTests : IDisposable
{
    private readonly string _dir;

    public SafePathImageSniffTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "aqw-sniff-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_dir, recursive: true);
        }
        catch (IOException)
        {
            // A leftover temp directory is not worth failing a test run over.
        }
        GC.SuppressFinalize(this);
    }

    private string WriteFile(string name, params byte[] bytes)
    {
        string path = Path.Combine(_dir, name);
        File.WriteAllBytes(path, bytes);
        return path;
    }

    // Each header is padded past 12 bytes so the WebP branch has a full window to read.
    private static byte[] Pad(params byte[] head)
    {
        var buffer = new byte[16];
        head.CopyTo(buffer, 0);
        return buffer;
    }

    [Fact]
    public void LooksLikeSupportedImage_JpegHeader_ReturnsTrue()
    {
        string path = WriteFile("a.jpg", Pad(0xFF, 0xD8, 0xFF, 0xE0));

        Assert.True(SafePath.LooksLikeSupportedImage(path));
    }

    [Fact]
    public void LooksLikeSupportedImage_PngHeader_ReturnsTrue()
    {
        string path = WriteFile("a.png", Pad(0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A));

        Assert.True(SafePath.LooksLikeSupportedImage(path));
    }

    [Fact]
    public void LooksLikeSupportedImage_GifHeader_ReturnsTrue()
    {
        string path = WriteFile("a.gif", Pad(0x47, 0x49, 0x46, 0x38, 0x39, 0x61));

        Assert.True(SafePath.LooksLikeSupportedImage(path));
    }

    [Fact]
    public void LooksLikeSupportedImage_BmpHeader_ReturnsTrue()
    {
        string path = WriteFile("a.bmp", Pad(0x42, 0x4D, 0x36, 0x00));

        Assert.True(SafePath.LooksLikeSupportedImage(path));
    }

    [Fact]
    public void LooksLikeSupportedImage_WebpHeader_ReturnsTrue()
    {
        string path = WriteFile("a.webp", Pad(
            0x52, 0x49, 0x46, 0x46,     // "RIFF"
            0x00, 0x00, 0x00, 0x00,     // chunk size
            0x57, 0x45, 0x42, 0x50));   // "WEBP"

        Assert.True(SafePath.LooksLikeSupportedImage(path));
    }

    [Fact]
    public void LooksLikeSupportedImage_TextFileRenamedToPng_ReturnsFalse()
    {
        string path = Path.Combine(_dir, "not-really.png");
        File.WriteAllText(path, "This is plain text pretending to be an image.");

        Assert.False(SafePath.LooksLikeSupportedImage(path));
    }

    [Fact]
    public void LooksLikeSupportedImage_RiffThatIsNotWebp_ReturnsFalse()
    {
        // A WAV file is also RIFF-tagged; only the WEBP form-type may pass.
        string path = WriteFile("a.webp", Pad(
            0x52, 0x49, 0x46, 0x46,
            0x00, 0x00, 0x00, 0x00,
            0x57, 0x41, 0x56, 0x45));   // "WAVE"

        Assert.False(SafePath.LooksLikeSupportedImage(path));
    }

    [Fact]
    public void LooksLikeSupportedImage_TruncatedPngHeader_ReturnsFalse()
    {
        // First four PNG bytes only; the 8-byte signature is incomplete.
        string path = WriteFile("a.png", 0x89, 0x50, 0x4E, 0x47);

        Assert.False(SafePath.LooksLikeSupportedImage(path));
    }

    [Fact]
    public void LooksLikeSupportedImage_FileShorterThanFourBytes_ReturnsFalse()
    {
        string path = WriteFile("a.png", 0xFF, 0xD8);

        Assert.False(SafePath.LooksLikeSupportedImage(path));
    }

    [Fact]
    public void LooksLikeSupportedImage_EmptyFile_ReturnsFalse()
    {
        string path = WriteFile("empty.png");

        Assert.False(SafePath.LooksLikeSupportedImage(path));
    }

    [Fact]
    public void LooksLikeSupportedImage_MissingFile_ReturnsFalse()
    {
        string path = Path.Combine(_dir, "does-not-exist.png");

        Assert.False(SafePath.LooksLikeSupportedImage(path));
    }

    [Fact]
    public void LooksLikeSupportedImage_DirectoryPath_ReturnsFalse()
    {
        Assert.False(SafePath.LooksLikeSupportedImage(_dir));
    }

    [Fact]
    public void LooksLikeSupportedImage_ExecutableWithImageExtension_ReturnsFalse()
    {
        // "MZ" - a PE binary renamed to .jpg.
        string path = WriteFile("payload.jpg", Pad(0x4D, 0x5A, 0x90, 0x00));

        Assert.False(SafePath.LooksLikeSupportedImage(path));
    }
}
