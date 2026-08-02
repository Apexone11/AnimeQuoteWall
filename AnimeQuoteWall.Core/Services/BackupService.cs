using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using AnimeQuoteWall.Core.Configuration;

namespace AnimeQuoteWall.Core.Services;

/// <summary>
/// Bundles the user data folder (settings, quotes, backgrounds, history metadata,
/// playlists) into a single ZIP archive for export, and restores from one. Backup
/// archives never include the wallpaper cache or thumbnails to keep file sizes
/// reasonable. The caller chooses the destination path and is responsible for keeping
/// the archive somewhere the user can find it.
/// </summary>
public sealed class BackupService
{
    private static readonly string[] FoldersToInclude =
    {
        "playlists",
        "backgrounds"
    };

    private static readonly string[] FilesToInclude =
    {
        "settings.json",
        "quotes.json"
    };

    public async Task ExportAsync(string destinationZipPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(destinationZipPath))
            throw new ArgumentException("Destination path is required.", nameof(destinationZipPath));

        var baseDir = AppConfiguration.BaseDirectory;
        if (!Directory.Exists(baseDir))
            throw new DirectoryNotFoundException($"App data folder not found: {baseDir}");

        var tempDir = Path.Combine(Path.GetTempPath(), $"AnimeQuoteWall_Backup_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            foreach (var fileName in FilesToInclude)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var src = Path.Combine(baseDir, fileName);
                if (File.Exists(src))
                    File.Copy(src, Path.Combine(tempDir, fileName), overwrite: true);
            }

            foreach (var subFolder in FoldersToInclude)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var src = Path.Combine(baseDir, subFolder);
                if (!Directory.Exists(src)) continue;

                var dst = Path.Combine(tempDir, subFolder);
                Directory.CreateDirectory(dst);
                foreach (var file in Directory.EnumerateFiles(src, "*", SearchOption.AllDirectories))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var rel = Path.GetRelativePath(src, file);
                    var copyTo = Path.Combine(dst, rel);
                    Directory.CreateDirectory(Path.GetDirectoryName(copyTo)!);
                    File.Copy(file, copyTo, overwrite: true);
                }
            }

            if (File.Exists(destinationZipPath))
                File.Delete(destinationZipPath);

            await Task.Run(() => ZipFile.CreateFromDirectory(tempDir, destinationZipPath, CompressionLevel.Optimal, includeBaseDirectory: false), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"BackupService.ExportAsync cleanup: {ex.Message}"); }
        }
    }

    public async Task ImportAsync(string sourceZipPath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(sourceZipPath))
            throw new FileNotFoundException("Backup archive not found.", sourceZipPath);

        var baseDir = AppConfiguration.BaseDirectory;
        Directory.CreateDirectory(baseDir);

        var extractDir = Path.Combine(Path.GetTempPath(), $"AnimeQuoteWall_Restore_{Guid.NewGuid():N}");
        Directory.CreateDirectory(extractDir);

        try
        {
            await Task.Run(() => ZipFile.ExtractToDirectory(sourceZipPath, extractDir, overwriteFiles: true), cancellationToken).ConfigureAwait(false);

            foreach (var fileName in FilesToInclude)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var src = Path.Combine(extractDir, fileName);
                if (!File.Exists(src)) continue;
                if (!SafePath.IsInsideRoot(src, extractDir)) continue;

                var dst = Path.Combine(baseDir, fileName);
                File.Copy(src, dst, overwrite: true);
            }

            foreach (var subFolder in FoldersToInclude)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var src = Path.Combine(extractDir, subFolder);
                if (!Directory.Exists(src)) continue;

                var dst = Path.Combine(baseDir, subFolder);
                Directory.CreateDirectory(dst);

                foreach (var file in Directory.EnumerateFiles(src, "*", SearchOption.AllDirectories))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var rel = Path.GetRelativePath(src, file);
                    var copyTo = Path.Combine(dst, rel);

                    if (!SafePath.IsInsideRoot(copyTo, dst)) continue;
                    Directory.CreateDirectory(Path.GetDirectoryName(copyTo)!);
                    File.Copy(file, copyTo, overwrite: true);
                }
            }
        }
        finally
        {
            try { Directory.Delete(extractDir, recursive: true); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"BackupService.ImportAsync cleanup: {ex.Message}"); }
        }
    }
}
