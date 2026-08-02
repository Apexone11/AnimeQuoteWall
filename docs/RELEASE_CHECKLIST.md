# Release Checklist

Run this checklist before tagging a new release of AnimeQuoteWall.

## Pre-flight

- [ ] All open release-blocker issues are closed.
- [ ] `CHANGELOG.md` has an entry for the new version.
- [ ] `<Version>` in `AnimeQuoteWall.GUI/AnimeQuoteWall.GUI.csproj` and `AnimeQuoteWall.Core/AnimeQuoteWall.Core.csproj` matches the target version.
- [ ] `installer/AnimeQuoteWall.iss` `MyAppVersion` matches.

## Build verification

- [ ] `dotnet restore` succeeds.
- [ ] `dotnet build AnimeQuoteWall.GUI/AnimeQuoteWall.GUI.csproj -c Release --no-incremental` returns 0 errors and 0 warnings. As of 2.0.0 the former CA1416 warning set is gone; any warning is a regression.
- [ ] `dotnet format --verify-no-changes` is clean.
- [ ] `dotnet list package --vulnerable --include-transitive` shows no entries at all (2.0.0 baseline is zero, not merely zero High/Critical).

If `--no-incremental` fails with `BG1002: ... .baml cannot be found`, the `obj`
directory is stale (common when the repo lives in a synced folder). Delete
`AnimeQuoteWall.GUI/obj` and `bin` and rebuild; it is not a code error.

## Source audit (use Grep, not eyeballs)

- [ ] No emojis in production code, XAML, comments, or docs (regex: `[\x{1F300}-\x{1FAFF}]|[\x{2600}-\x{27BF}]`).
- [ ] No hardcoded hex colors in `AnimeQuoteWall.GUI/Pages/**.xaml` or `Resources/**.xaml` outside the Theme dictionaries.
- [ ] No `StaticResource` referencing theme color keys (`TextPrimary`, `CardBackground`, `BorderColor`, `PrimaryColor`, etc.).
- [ ] No empty `catch {}` blocks remain.
- [ ] No `.Wait()`, `.Result`, or `GetAwaiter().GetResult()` on the UI dispatcher.
- [ ] No `Process.Start` using `Arguments =` string concatenation; all external invocations use `ArgumentList`.
- [ ] No `UseShellExecute = true` in production code paths.

## Manual smoke test (run the actual app)

For each test, record pass/fail in the PR or release notes.

### Static wallpaper

- [ ] Launch the app. Default page loads to Static Generator without errors.
- [ ] Click Generate. A new wallpaper preview appears within 5 seconds.
- [ ] Click Apply. Desktop wallpaper updates immediately.
- [ ] Generate again. Preview refreshes and Apply still works.

### Multi-monitor

- [ ] On a multi-monitor system, choose a non-primary monitor from the dropdown and Generate. The preview persists.
- [ ] Click "Generate All Monitors". Each monitor receives a distinct wallpaper.

### Quotes

- [ ] Open the Quotes page. Existing quotes load.
- [ ] Add a new quote via the dialog (Add Quote button). Dialog opens with theme tokens (no white-only background).
- [ ] Cancel with Esc. Enter on the OK button submits.
- [ ] Filter by category. Favorite/unfavorite a quote.

### Backgrounds

- [ ] Open the Image Library page. Existing images load.
- [ ] Add a background via the Add button.
- [ ] Try adding a non-image file (e.g., a `.txt` renamed to `.png`). It must be rejected by `SafePath.LooksLikeSupportedImage`.

### Animated wallpapers

- [ ] Open the Animated Library page. Status bar reflects whether Wallpaper Engine is detected.
- [ ] Add a GIF. Apply Animated. The static first frame is set as the wallpaper (or Wallpaper Engine handles it).
- [ ] Add an MP4 with Wallpaper Engine running; it animates. Stop animation.

### History

- [ ] Generate two wallpapers. Open History page. Both appear.
- [ ] Restore an older entry. Desktop wallpaper changes.
- [ ] Delete a single entry. Confirmation prompt appears; entry vanishes after Yes.
- [ ] Clear All. All entries gone.
- [ ] Star an entry. The star turns gold and survives an app restart.
- [ ] Toggle Favorites. Only starred entries show; the empty state reflects the filter.
- [ ] Type in the search box. Filtering matches quote text, character, anime, and date.
- [ ] Required after any Magick.NET upgrade (CLAUDE.md Section 12): thumbnails render, and Delete succeeds immediately after a thumbnail was displayed (no file lock).
- [ ] Generate on all monitors twice in quick succession. Every image is distinct; no entry overwrites another.

### Background fit and effects

- [ ] Set Background fit to each of Fill / Fit / Stretch / Center and generate. Fill centre-crops, Fit letterboxes, Center pads, none distort except Stretch.
- [ ] Set Background effect to each of None / Blur / Sepia / Grayscale / Vintage and generate. The effect applies under the quote text, which stays legible.

### Tray, window, and startup

- [ ] Enable minimize-to-tray. Minimize; the window leaves the taskbar and the tray icon remains.
- [ ] Double-click the tray icon. The window restores, preserving a previously maximized state.
- [ ] Tray menu Open / Hide / Exit all work. After Exit no tray icon lingers.
- [ ] Move and resize the window, exit via the tray, relaunch. Position and size are restored.
- [ ] Disconnect the monitor the window was on, relaunch. The window appears on-screen, not off it.
- [ ] Toggle Start with Windows on and off. The `HKCU` run entry is created and removed. Visiting Settings again does not rewrite it.
- [ ] Press Ctrl+Alt+Q from another application. The window toggles visibility.
- [ ] Launch a second instance. It does not start a new window; the existing one comes to the front.

### Performance and power

- [ ] With a playlist rotating, unplug AC power with "pause on battery" enabled. Rotation pauses; replugging resumes it.
- [ ] Maximize another window with "pause when maximized" enabled. Rotation pauses.
- [ ] Toggle a pause rule while a playlist runs. The change takes effect without restarting the app.

### Updater

- [ ] Run a dev or portable build. The updater is a no-op and logs no error.
- [ ] Launch with `--steam` and with `--store`. The updater is skipped in both.
- [ ] With a newer version published, an installed build detects it, downloads in the background, and prompts to restart and apply.

### Playlists

- [ ] Create a playlist. Enable it. Set rotation interval to 1 minute. Confirm a wallpaper rotation occurs after the timer fires.

### Settings

- [ ] Switch theme from System to Light. UI updates immediately (no restart).
- [ ] Switch to Dark. UI updates immediately.
- [ ] Change a path; restart banner appears (only restart-requiring change).
- [ ] Reset to defaults. Banner clears; paths are reset.

### Accessibility

- [ ] Press Tab from the title bar. Focus moves through nav buttons with a visible focus ring.
- [ ] Press Ctrl+1 through Ctrl+7. Each shortcut navigates to the expected page.
- [ ] Run Accessibility Insights for Windows on the main window. Resolve any new failures.

## Installer

- [ ] Re-run `installer/assets/build-banners.ps1` after the version bump. It reads `<Version>` from `AnimeQuoteWall.GUI.csproj`, so the side banner shows the new version. The `.bmp` files are gitignored build artifacts and must be regenerated on a clean checkout.
- [ ] `installer/assets/wizard-side.bmp` (164x314 24-bit), `wizard-small.bmp` (55x58 24-bit), and `setup-icon.ico` (multi-resolution) are present.
- [ ] `./installer/build.ps1` succeeds and produces `installer/dist/AnimeQuoteWall-Setup-<version>.exe`.
- [ ] Run the installer on a clean Windows 10/11 VM:
  - [ ] Welcome page renders.
  - [ ] License page shows the RTF and "I accept" is required to proceed.
  - [ ] Install directory page allows custom paths.
  - [ ] Progress bar advances; install completes.
  - [ ] Finish page offers "Launch AnimeQuoteWall". App opens.
- [ ] Uninstall via Settings -> Apps. App is removed; user data under `%LOCALAPPDATA%\AnimeQuoteWall\` is preserved (only the cache subfolder is removed).

## Signing

- [ ] `./installer/build.ps1 -Sign` succeeds.
- [ ] `signtool verify /pa /v installer\dist\AnimeQuoteWall-Setup-<version>.exe` shows a valid Authenticode signature with a timestamp.
- [ ] The signed installer launches without a SmartScreen "Don't run" block (or shows only the "Less common app" badge for new certs).

## Release

- [ ] Tag the commit: `git tag -s v<version> -m "v<version>"`.
- [ ] Push the tag.
- [ ] Upload the signed `AnimeQuoteWall-Setup-<version>.exe` to GitHub Releases with the changelog excerpt as the body.
- [ ] Publish the SHA-256 hash in the release notes.
- [ ] Run `vpk pack` (see `RUNNING.md`) and upload the Velopack output, including `Setup.exe`, to the same release. `Setup.exe` is the install-if-missing / update-if-present entry point that the in-app updater feed points at.
- [ ] Verify an existing installed build actually sees the new release and updates.
- [ ] Update Steamworks build (for Steam-bound releases).
