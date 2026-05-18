# Changelog

All notable changes to Anime Quote Wallpaper Manager will be documented in this file.

## [Unreleased] - 2026-05-17

### Security

- **CVE response**: upgraded Magick.NET-Q8-AnyCPU from 14.10.4 to 14.13.1, resolving 23 ImageMagick vulnerabilities (heap buffer overflows in MVG, MNG, JP2, JXL, FTXT decoders/encoders; stack overflows in XML parsing; etc.). `dotnet list package --vulnerable --include-transitive` now reports zero vulnerable packages in both Core and GUI.
- **Path-traversal protection**: new `AnimeQuoteWall.Core.Services.SafePath` enforces canonicalization plus root containment plus reparse-point rejection. Wired into `PlaylistService.GetPlaylistFilePath`, `WallpaperHistoryService.LoadHistoryEntriesAsync` / `DeleteFromHistoryAsync`, `HistoryPage.RestoreButton_Click`, and the backup import/export pipeline.
- **Process argument-injection protection**: every `ffmpeg` and Wallpaper Engine `Process.Start` call migrated from string-concatenated `Arguments =` to `ProcessStartInfo.ArgumentList`. Also refuses to fall back to FFmpeg on PATH; only the bundled binary is invoked.
- **ImageMagick hardening**: `ImageMagickHardening.Apply()` runs at startup and caps image width/height (16K), memory (512 MiB), disk (1 GiB), and worker threads.
- **CodeProtection fail-closed**: integrity checks now fail closed in Release builds; only Debug fails open for developer convenience.
- **Settings backup-on-corruption**: corrupt `settings.json` is preserved as `settings.json.corrupt-<timestamp>` instead of being silently overwritten.
- **Shared HttpClient**: `AnimatedWallpaperService` now uses a single static `HttpClient` with a 5 s timeout, 3 s connect timeout, no auto-redirect; fixes socket exhaustion under load.
- New `SECURITY.md` with disclosure policy and hardening posture.

### Added

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

### Fixed

- `HistoryPage.DeleteButton_Click` no longer calls `.Wait()` on the UI dispatcher (latent deadlock).
- `WallpaperPage.SetWallpaper` sync wrapper deleted; all call sites are now `async`.
- Build now produces 0 errors and 0 warnings on both GUI and CLI projects.
- Removed unused fields `_currentZoom`, `_isShowingPrevious`, `_previousWallpaperPath` in `WallpaperPage`.
- Replaced 11 empty `catch {}` blocks with logged catches across SettingsPage, WallpaperPage, BackgroundsPage, AnimatedWallpapersPage, SimpleMainWindow, HistoryPage.
- Emojis removed from `AnimeQuoteWall.CLI/Program.cs`, `Controls/ToastNotification.xaml`, and `Pages/QuotesPage.xaml` (replaced with PackIconMaterial where iconography was needed).

### Removed

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

