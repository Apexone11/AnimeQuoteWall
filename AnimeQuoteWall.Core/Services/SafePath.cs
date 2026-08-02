using System;
using System.IO;
using System.Linq;
using System.Security;

namespace AnimeQuoteWall.Core.Services;

/// <summary>
/// Centralized filesystem-input validation. Every user- or JSON-supplied path that the
/// app reads, writes, copies, or deletes must pass through one of these helpers so a
/// crafted history entry or playlist file cannot escape the app's data root.
/// </summary>
public static class SafePath
{
    private static readonly char[] InvalidIdChars =
        Path.GetInvalidFileNameChars().Concat(new[] { '.', ' ', '/', '\\' }).Distinct().ToArray();

    /// <summary>
    /// Returns true when <paramref name="candidate"/> resolves to a path strictly
    /// inside (or equal to) <paramref name="root"/>. Comparison is case-insensitive on
    /// Windows. Reparse points (symlinks/junctions) in the candidate are rejected to
    /// avoid junction-pointing attacks.
    /// </summary>
    public static bool IsInsideRoot(string candidate, string root)
    {
        if (string.IsNullOrWhiteSpace(candidate) || string.IsNullOrWhiteSpace(root))
            return false;

        try
        {
            var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar);
            var fullCandidate = Path.GetFullPath(candidate).TrimEnd(Path.DirectorySeparatorChar);

            if (!fullCandidate.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
                return false;

            if (fullCandidate.Length > fullRoot.Length
                && fullCandidate[fullRoot.Length] != Path.DirectorySeparatorChar
                && fullCandidate[fullRoot.Length] != Path.AltDirectorySeparatorChar)
                return false;

            if (File.Exists(fullCandidate) && File.GetAttributes(fullCandidate).HasFlag(FileAttributes.ReparsePoint))
                return false;

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SafePath.IsInsideRoot: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Throws if <paramref name="candidate"/> is not inside <paramref name="root"/>.
    /// Use at sinks that read, write, copy, or delete files supplied by untrusted
    /// metadata (history JSON, playlist JSON, per-monitor settings).
    /// </summary>
    public static string RequireInsideRoot(string candidate, string root, string sinkName)
    {
        if (!IsInsideRoot(candidate, root))
            throw new SecurityException($"{sinkName}: path is outside the allowed root.");
        return Path.GetFullPath(candidate);
    }

    /// <summary>
    /// Sanitizes a free-form identifier (playlist id, quote id) into a safe filename
    /// stem by stripping every non-alphanumeric/dash character. Length capped at 64.
    /// </summary>
    public static string SanitizeId(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "unknown";

        var span = raw.AsSpan();
        Span<char> buffer = stackalloc char[Math.Min(span.Length, 64)];
        int j = 0;
        for (int i = 0; i < span.Length && j < buffer.Length; i++)
        {
            char c = span[i];
            bool keep = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '-' || c == '_';
            if (keep) buffer[j++] = c;
        }
        return j == 0 ? "unknown" : new string(buffer[..j]);
    }

    /// <summary>
    /// Returns true if the file at <paramref name="path"/> begins with a magic-byte
    /// signature for a supported still-image format (JPEG, PNG, GIF, BMP, WebP).
    /// Used at import time to reject polyglot or extension-mismatched uploads.
    /// </summary>
    public static bool LooksLikeSupportedImage(string path)
    {
        try
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            Span<byte> head = stackalloc byte[16];
            int n = fs.Read(head);
            if (n < 4) return false;

            if (head[0] == 0xFF && head[1] == 0xD8 && head[2] == 0xFF) return true;
            if (n >= 8 && head[0] == 0x89 && head[1] == 0x50 && head[2] == 0x4E && head[3] == 0x47
                && head[4] == 0x0D && head[5] == 0x0A && head[6] == 0x1A && head[7] == 0x0A) return true;
            if (head[0] == 0x47 && head[1] == 0x49 && head[2] == 0x46 && head[3] == 0x38) return true;
            if (head[0] == 0x42 && head[1] == 0x4D) return true;
            if (n >= 12 && head[0] == 0x52 && head[1] == 0x49 && head[2] == 0x46 && head[3] == 0x46
                && head[8] == 0x57 && head[9] == 0x45 && head[10] == 0x42 && head[11] == 0x50) return true;

            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SafePath.LooksLikeSupportedImage: {ex.Message}");
            return false;
        }
    }
}
