using System;
using System.Threading.Tasks;
using Velopack;
using Velopack.Sources;

namespace AnimeQuoteWall.GUI.Services;

/// <summary>
/// Checks for and applies application updates via Velopack. It is a no-op for builds that were not
/// installed by Velopack (e.g. `dotnet run` or a raw build), and is intended to be skipped on the
/// Microsoft Store and Steam channels (which update the app themselves). Updates are fetched over
/// HTTPS from GitHub Releases (CLAUDE.md Section 13: never auto-execute from a non-HTTPS source).
/// </summary>
public sealed class UpdateService
{
    // The GitHub repository hosting the Velopack release feed. Adjust to the real repository
    // before publishing direct (non-Store/Steam) builds.
    private const string GithubRepoUrl = "https://github.com/Apexone11/AnimeQuoteWall";

    private UpdateManager? _manager;
    private UpdateInfo? _pendingUpdate;

    /// <summary>Raised (marshal to the UI thread) once an update has been downloaded and is ready to apply.</summary>
    public event EventHandler<UpdateInfo>? UpdateReady;

    /// <summary>
    /// Checks for an update in the background, downloading it if found. Safe on any build: returns
    /// immediately when the app was not installed by Velopack. Raises <see cref="UpdateReady"/> on
    /// success. Never throws to the caller.
    /// </summary>
    public async Task CheckAsync()
    {
        try
        {
            _manager = new UpdateManager(new GithubSource(GithubRepoUrl, null, false));

            // Dev builds / portable copies are not "installed" by Velopack - nothing to update.
            if (!_manager.IsInstalled)
                return;

            var info = await _manager.CheckForUpdatesAsync().ConfigureAwait(false);
            if (info == null)
                return; // already up to date

            await _manager.DownloadUpdatesAsync(info).ConfigureAwait(false);
            _pendingUpdate = info;
            UpdateReady?.Invoke(this, info);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"UpdateService.CheckAsync: {ex.Message}");
        }
    }

    /// <summary>Applies the downloaded update and restarts the app. Call after user confirmation.</summary>
    public void ApplyAndRestart()
    {
        try
        {
            if (_manager != null && _pendingUpdate != null)
                _manager.ApplyUpdatesAndRestart(_pendingUpdate);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"UpdateService.ApplyAndRestart: {ex.Message}");
        }
    }
}
