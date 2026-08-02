# Changelog

All notable changes to Anime Quote Wallpaper Manager will be documented in this file.

## [2.0.0] - 2026-08-02

Major release. Two full adversarial bug hunts (38 verified findings, all fixed),
a security and dependency overhaul, an installer plus auto-updater pipeline, and
a run of new user-facing features. Build is 0 errors / 0 warnings across GUI,
Core, and CLI, with no vulnerable packages.

### Security

- **CVE response**: upgraded Magick.NET-Q8-AnyCPU from 14.10.4 to 14.13.1, resolving 23 ImageMagick vulnerabilities (heap buffer overflows in MVG, MNG, JP2, JXL, FTXT decoders/encoders; stack overflows in XML parsing; etc.), then to 14.16.0, clearing a further 328 advisories (24 of them high severity) published against 14.13.1. `dotnet list package --vulnerable --include-transitive` now reports zero vulnerable packages in both Core and GUI.
- **License hygiene**: removed SixLabors.ImageSharp (Split License) from Core. It was unused and its license is incompatible with the closed-source store and Steam distribution. Dependencies are now permissive-only (MIT / Apache-2.0 / BSD / MS-PL).
- **Update channel**: the in-app Velopack updater only fetches over HTTPS and is skipped entirely under `--steam` and `--store`, where the platform patches the app.
- **Path-traversal protection**: new `AnimeQuoteWall.Core.Services.SafePath` enforces canonicalization plus root containment plus reparse-point rejection. Wired into `PlaylistService.GetPlaylistFilePath`, `WallpaperHistoryService.LoadHistoryEntriesAsync` / `DeleteFromHistoryAsync`, `HistoryPage.RestoreButton_Click`, and the backup import/export pipeline.
- **Process argument-injection protection**: every `ffmpeg` and Wallpaper Engine `Process.Start` call migrated from string-concatenated `Arguments =` to `ProcessStartInfo.ArgumentList`. Also refuses to fall back to FFmpeg on PATH; only the bundled binary is invoked.
- **ImageMagick hardening**: `ImageMagickHardening.Apply()` runs at startup and caps image width/height (16K), memory (512 MiB), disk (1 GiB), and worker threads.
- **CodeProtection fail-closed**: integrity checks now fail closed in Release builds; only Debug fails open for developer convenience.
- **Settings backup-on-corruption**: corrupt `settings.json` is preserved as `settings.json.corrupt-<timestamp>` instead of being silently overwritten.
- **Shared HttpClient**: `AnimatedWallpaperService` now uses a single static `HttpClient` with a 5 s timeout, 3 s connect timeout, no auto-redirect; fixes socket exhaustion under load.
- New `SECURITY.md` with disclosure policy and hardening posture.

### Added

- **Auto-update**: Velopack (MIT) integration. A custom `Main` runs `VelopackApp.Build().Run()` before the WPF app starts to handle install, update, and uninstall hooks. `UpdateService` checks GitHub Releases over HTTPS, downloads in the background, and prompts to restart and apply. It is a no-op for dev and portable builds and is skipped under `--steam` / `--store`. The `vpk pack` flow also produces a `Setup.exe` bootstrapper that installs when the app is missing and updates when it is present.
- **System tray**: `TrayIconService` (built-in WinForms `NotifyIcon`, no new dependency) with Open / Hide / Exit, double-click to restore, and an optional minimize-to-tray mode behind a new Settings toggle.
- **Window placement memory**: main window position, size, and maximized state persist across launches, guarded so the window never reopens off a disconnected monitor.
- **Start with Windows**: `StartupService` plus a Settings toggle, writing only the per-user `HKCU` run entry.
- **Global hotkey**: system-wide Ctrl+Alt+Q toggles window visibility via `GlobalHotkeyService` (`RegisterHotKey` with an `HwndSource` hook, unregistered on exit).
- **Single instance**: a named mutex makes the app single-instance; a second launch signals the running instance over a per-user named pipe to come to the front, then exits.
- **History favorites and search**: a per-item favorite star on `HistoryPage`, a Favorites-only toggle, and a search box filtering by quote text, character, anime, or date. Filtering runs against a cached list so the virtualizing `ListBox` stays responsive.
- **Background fit modes**: a Fill / Fit / Stretch / Center picker. `WallpaperService.FitBackground` composes the source onto the canvas preserving aspect ratio; the default is now Fill (cover plus center-crop) rather than the previous distorting stretch.
- **Background filter effects**: an optional Blur / Sepia / Grayscale / Vintage filter applied to the background before the quote is drawn, wiring the previously unused `MediaEditingService.ApplyFilter` into all three generation paths.
- **Performance and power controls**: `PerformanceMonitorService` pauses wallpaper rotation on battery, when a window is maximized or fullscreen, over RDP, or when a configured app is running, plus a Low-Power mode. Surfaced as a "Performance and Power" card in Settings.
- **Test project**: `AnimeQuoteWall.Core.Tests` (xUnit), 82 tests covering the areas CLAUDE.md Section 9 requires: `SafePath` root containment and id sanitization, image magic-byte sniffing, ffmpeg and Wallpaper Engine argument construction, and per-monitor dispatch resolution. To make those testable without a process, a COM object, or a real desktop, the argument vectors and the monitor-index mapping were extracted into the pure `FfmpegArguments`, `WallpaperEngineArguments`, and `PerMonitorDispatch` helpers that the services now call.
- **`AnimeQuoteWall.sln`**: a solution covering Core, GUI, CLI, and the tests, so `dotnet build`, `dotnet test`, `dotnet format`, and the vulnerability audit run from the repo root as CLAUDE.md Section 11 specifies. Previously each had to be pointed at an individual project.
- **`RUNNING.md`**: how to run the app and build the installer without an IDE.
- **Installer**: full Inno Setup 6.3 installer pipeline at `installer/`. Modern wizard with welcome, license/TOS acceptance, install-location chooser, progress bar, finish-with-launch. `.NET 8 Desktop Runtime` detection that offers to fetch from `dotnet.microsoft.com` when missing. `installer/build.ps1` publishes, compiles, and reports SHA-256; `-Sign` switch invokes signtool.
- **Themed `AddQuoteDialog`** (XAML window) replaces the old code-behind `SimpleQuoteDialog` with hardcoded white background and emoji headers.
- **`AboutDialog`** with version detection, license, third-party credits, and GitHub links.
- **`ToastService`** for non-blocking notifications across the app. Replaces blocking `MessageBox` for transient feedback.
- **`ShellLauncher`** as the single chokepoint for opening folders in Explorer and validated URLs in the default browser.
- **`BackupService`**: export and import a ZIP of settings.json, quotes.json, playlists, and backgrounds. Import is path-traversal-safe.
- **Open data folder**, **Open log folder**, **Export backup**, **Import backup**, **About** buttons in Settings.
- **Hot theme reload**: switching theme in Settings applies immediately; no restart required.
- **Keyboard shortcuts**: Ctrl+1 through Ctrl+7 for page navigation.
- **Global `FocusVisualStyle`** in `ButtonStyles.xaml` so every button has a visible keyboard focus ring.
- New `CLAUDE.md` (contributor and AI-assistant ruleset, 16 sections), rewritten professional `README.md`, `CONTRIBUTING.md`, `docs/ARCHITECTURE.md`, `docs/RELEASE_CHECKLIST.md`.
- `Directory.Build.props` enables NuGetAudit solution-wide; new `.editorconfig`.

### Changed

- Sidebar fully wired to theme tokens (`SidebarBackground`, `SidebarBorder`, `SidebarText`, `SidebarHover`, `SidebarAccent`, `SidebarBrandHeading`). Sidebar now reflects light versus dark theme correctly.
- Settings page restart-warning and path-validation banners migrated from hardcoded amber/red palettes to `WarningColor` / `DangerColor` tokens.
- AnimatedWallpapersPage status bar migrated to `SuccessDark` / `SuccessColor` (available state) and `WarningDark` / `WarningColor` (Wallpaper Engine missing).
- HistoryPage, PlaylistsPage, AnimatedWallpapersPage, WallpaperPage hover/selected border colors migrated to `PrimaryColor` token.
- Inno Setup script updated for 6.3+: `WizardResizable` removed (obsolete), `ArchitecturesAllowed=x64compatible` (replaces deprecated `x64`).
- **Branding**: the app icon is now the original indigo quotation-mark tile, regenerated across the app icon, installer wizard art, and store tiles from `installer/assets/generate-logo.ps1`.
- **Version reconciled to 2.0.0** across `AnimeQuoteWall.GUI.csproj`, `AnimeQuoteWall.Core.csproj`, `installer/AnimeQuoteWall.iss`, and the README badge, which had drifted between 1.3.0 and 1.3.1. `installer/assets/build-banners.ps1` now reads the version from the GUI csproj instead of hardcoding it, so the wizard banner cannot drift again.

### Fixed

Findings from two five-round adversarial bug hunts (38 verified issues, all resolved):

- **Per-monitor wallpaper never worked**: the OS gate `Major >= 6 && Minor >= 2` is false on Windows 10/11 (10.0), silently disabling the feature for every supported OS. Now compares the whole `Version` against 6.2.
- **Pause policy was dead code**: `CheckPausePolicy()` was never called, so no battery / maximized / RDP / per-app / Low-Power rule had any effect. The monitor loop is now its sole evaluator and `PlaylistWorker` reads the aggregate `ShouldPause` and re-applies settings each iteration, so changes take effect live.
- **History filename collisions**: multi-monitor and fast-rotation saves shared a one-second timestamp, overwriting images and producing duplicate entries. Names now use millisecond precision plus a GUID, and the metadata read-modify-write is serialized with a `SemaphoreSlim`.
- **Window placement lost on tray exit**: `Application.Shutdown` skips `Window.Closing`, so tray Exit now closes the window instead. Tray restore also preserves a prior maximized state.
- **Off-screen guard used the wrong coordinate space**: it compared DIP coordinates against physical-pixel screen bounds, misbehaving under DPI scaling. Now uses the DIP-space virtual screen.
- **Virtualization was defeated**: `HistoryPage` used a plain `ItemsControl` plus `UniformGrid` (now a virtualizing `ListBox`), and the outer `ScrollViewer`s on `BackgroundsPage` / `QuotesPage` prevented their `VirtualizingStackPanel`s from engaging.
- **Playlist interval overflow**: long intervals broke on an `int` millisecond overflow. Input is clamped to 5..86400 seconds and converted with `TimeSpan.FromSeconds`.
- **`AppConfiguration` data race**: `LoadSettings` and the per-monitor dictionary accessors now run under the settings lock, ending "collection modified" failures during serialization and torn reads against the `PlaylistWorker` thread.
- **Thumbnail file locks**: `HistoryPage` thumbnails route through `ImagePathConverter` (`OnLoad` plus `DecodePixelWidth` plus `Freeze`), so Delete now works. `ImagePathConverter` also no longer sets both `DecodePixelWidth` and `DecodePixelHeight`, which distorted aspect ratios.
- **Settings page side effects on load**: programmatic control initialization was rewriting the `HKCU` startup entry and re-applying the theme on every visit. Handlers are suppressed during initialization, and bool config setters skip saves when the value is unchanged.
- `ImageCacheService` leaked a GDI+ bitmap clone and double-counted memory on a cache-miss race; `TrayIconService` disposed the shared `SystemIcons.Application` handle and leaked its `ContextMenuStrip`; `AnimatedWallpapersPage` and `BackgroundsPage` leaked `CancellationTokenSource` instances on reassign and unload.
- Replaced blocking `Dispatcher.Invoke` with `InvokeAsync` in the History, Quotes, and Playlists load paths; logged the previously empty history-save catch in `PlaylistWorker`.
- Shuffle used a per-call time-seeded `Random` and could repeat the same entry; now uses `Random.Shared` and avoids immediate repeats.
- `HistoryPage.DeleteButton_Click` no longer calls `.Wait()` on the UI dispatcher (latent deadlock).
- `WallpaperPage.SetWallpaper` sync wrapper deleted; all call sites are now `async`.
- Build now produces 0 errors and 0 warnings on both GUI and CLI projects.
- Removed unused fields `_currentZoom`, `_isShowingPrevious`, `_previousWallpaperPath` in `WallpaperPage`.
- Replaced 11 empty `catch {}` blocks with logged catches across SettingsPage, WallpaperPage, BackgroundsPage, AnimatedWallpapersPage, SimpleMainWindow, HistoryPage.
- Emojis removed from `AnimeQuoteWall.CLI/Program.cs`, `Controls/ToastNotification.xaml`, and `Pages/QuotesPage.xaml` (replaced with PackIconMaterial where iconography was needed).

### Removed

- The "Animation framerate cap" and "Render scale" sliders in Settings. Neither had a consumer in the GUI (the animation-frame API is CLI-only), so they were non-functional controls. The config fields remain for the planned animation-export feature.
- `SixLabors.ImageSharp` from `AnimeQuoteWall.Core` (unused; Split License).
- `*.backup` files across the GUI project.
- `AnimeQuoteWall.CLI/TestConsole.cs` (conflicted with `Program.cs` top-level statements).
- Code-behind `SimpleQuoteDialog` in `SimpleMainWindow.xaml.cs` (replaced by themed XAML dialog).

## [1.3.1] - 2025-01-27

### Fixed
- **Settings Page Crash**: Fixed critical crash when loading Settings page by adding comprehensive null checks for all UI elements
- **XAML Fill Error**: Fixed "Failed to create a 'Fill' from the text 'None'" error by changing `Fill="None"` to `Fill="{x:Null}"` in CheckBox style
- **Exception Handling**: Improved global exception handling with user-friendly error messages and file logging
- **Image Library Loading**: Fixed Image Library page not loading properly with async loading and proper error handling
- **Animated Library Loading**: Fixed Animated Library page with cancellation token support and better error recovery

### Added
- **Loading Indicators**: Added loading indicators and empty state UI to Image Library and Animated Library pages
- **Exception Logging**: Added automatic error logging to `%LocalAppData%/AnimeQuoteWall/logs/` directory
- **Experimental Feature Toggles**: Added Settings page toggles for experimental features (Animated Apply, Per-Monitor Apply)
- **Async Library Loading**: Implemented async loading for both Image Library and Animated Library with cancellation support
- **Virtualization**: Improved list virtualization for better performance with large image collections

### Changed
- **Dependency Updates**: Updated NuGet packages to latest compatible versions
  - MahApps.Metro.IconPacks: 4.11.0 → 4.12.0
  - Magick.NET-Q8-AnyCPU: 13.7.0 → 14.1.0
  - System.Drawing.Common: 9.0.10 → 9.0.0
  - System.Text.Json: 9.0.10 → 9.0.0
- **Error Messages**: Improved error messages to be more user-friendly and actionable
- **Image Thumbnails**: Optimized thumbnail generation (256x144) for better performance and memory usage
- **Style Unification**: Removed duplicate styles from App.xaml, centralized in ButtonStyles.xaml

### Technical
- Fixed Magick.NET API compatibility (uint conversions for Geometry and AnimationDelay)
- Added missing `using System.IO;` in App.xaml.cs
- Improved null safety throughout SettingsPage.xaml.cs
- Enhanced error recovery in library loading operations

## [1.3.0] - 2025-01-XX

### Added
- **Remove Animated Wallpaper Button**: Added a "Remove" button in the Animated Wallpapers page that allows users to revert from an applied animated wallpaper back to the previous static wallpaper
- **Remove Generated Wallpaper Button**: Added a "Remove Generated" button in the Static Wallpaper Generator page that deletes the current generated wallpaper and restores the previous one if available

### Changed
- **Improved UI Spacing**: Increased spacing between headers and action buttons for better visual hierarchy
  - Animated Wallpapers page: Header margin increased from 16px to 24px
  - Static Wallpaper Generator page: Header margin increased from 20px to 28px, action bar margin increased from 8px to 12px
- **High-Quality App Icon**: Regenerated app icon as a high-quality multi-resolution ICO file (256x256) from the source PNG for better display quality across all Windows contexts

### Technical
- Updated version to 1.3.0 in both GUI and Core projects
- Icon generation script creates optimized multi-resolution ICO files

## [1.2.0] - Previous Release

Previous version features and improvements.

