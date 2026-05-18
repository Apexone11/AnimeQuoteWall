# CLAUDE.md - AnimeQuoteWall Contributor and AI Assistant Rules

This file is the source of truth for human contributors and AI coding assistants working on AnimeQuoteWall (WPF .NET 8, multi-project, Steam-bound). Every rule here is mechanically applicable. When a rule conflicts with "looks cleaner," the rule wins.

---

## 0. Hard Bans (apply everywhere, no exceptions)

- **No emojis** anywhere in source code, XAML strings, comments, log messages, commit messages, PR titles or bodies, or markdown docs. Shipping target is Steam plus a signed Windows binary; emoji glyphs render inconsistently across Windows shells, console hosts, and signed-installer dialogs, and they break grep tooling.
- **No hardcoded hex or `#RRGGBB` colors** in XAML or code-behind. Use `DynamicResource` against the theme dictionaries in `AnimeQuoteWall.GUI/Resources/Themes/Theme.Light.xaml` and `Theme.Dark.xaml`. The app supports runtime theme switching; static literals freeze a theme and silently break dark/light parity.
- **No `StaticResource` for theme color/brush keys** (`TextPrimary`, `CardBackground`, `BorderColor`, `PrimaryColor`, `SidebarBackground`, etc.). `StaticResource` is allowed ONLY for style keys (`ModernPrimaryButton`, `ModernCard`, etc.). `StaticResource` resolves once at load; theme switches won't propagate.
- **No empty `catch {}` blocks.** Every catch logs to the per-day error log under `%LOCALAPPDATA%\AnimeQuoteWall\logs\` and either rethrows or returns a typed failure. Silent swallows were the root cause of multiple shipped bugs.
- **No `--no-verify`, `--no-gpg-sign`, or `git push --force`** to `main`. Bypasses signing and CI gates that exist for the Steam release.
- **No half-finished work merged.** If you cannot complete a feature in the current change, revert your partial work. No commented-out stubs, no `// TODO` without a tracked issue link, no buttons that throw NotImplemented.

---

## 1. .NET 8 / WPF Async, Lifecycle, and Architecture

- Public async APIs in `AnimeQuoteWall.Core` MUST accept a `CancellationToken` (optional with default) and pass it down; private helpers MUST take it as a required parameter. Prevents orphaned wallpaper/ffmpeg jobs when the user navigates away.
- In Core libraries, use `await ... .ConfigureAwait(false)`. In GUI event handlers and code-behind, do NOT add `.ConfigureAwait(false)` because the continuation must run on the UI dispatcher. WPF still has a SynchronizationContext; getting this wrong causes UI access exceptions or deadlocks.
- Never call `.Result`, `.Wait()`, or `GetAwaiter().GetResult()` on a `Task` from UI code. Deadlocks the dispatcher.
- Fire-and-forget MUST use the project pattern: `_ = Task.Run(...).ContinueWith(t => { if (t.IsFaulted) /* log */ ; }, TaskScheduler.Default);`. Never `TaskScheduler.Current` (captures the UI scheduler and re-enters the dispatcher).
- Any type holding ffmpeg processes, file streams, or `HttpClient` aggregates MUST implement `IAsyncDisposable` AND `IDisposable`. Prevents zombie ffmpeg child processes on shutdown.
- Even though some pages still use code-behind today, all non-trivial logic MUST live in `AnimeQuoteWall.Core` services testable without WPF. Keeps the Steam-shipped binary surface testable in CI.

---

## 2. WPF Security

- Never load XAML from disk, network, registry, or any user-controlled source with `XamlReader.Load`. XAML can construct arbitrary objects and via markup extensions execute code paths equivalent to running untrusted code.
- `Process.Start` MUST use `ProcessStartInfo` with `UseShellExecute = false` and `ArgumentList` (not `Arguments`) for ffmpeg, wallpaper apply, and Wallpaper Engine calls. `ArgumentList` escapes per-argument; string concatenation enables argument-injection (ImageTragick-style).
- `UseShellExecute = true` is forbidden in production code paths EXCEPT for opening validated `http`/`https` URLs in the user's default browser (see `AnimeQuoteWall.GUI.Services.ShellLauncher.OpenUrl`). The URL scheme MUST be checked first; never hand a user-provided string to the shell without that gate.
- Resolve `ffmpeg.exe` by absolute path bundled in `Resources/ffmpeg/`. NEVER fall back to PATH. Avoids EXE-preloading attacks where a malicious binary on PATH runs as us.
- Wallpaper Engine localhost calls MUST validate response content-type and reject non-localhost redirects.

---

## 3. Secure C# Coding (Microsoft + OWASP)

- Validate every external input against an allowlist before use. Allowlists fail closed; denylists miss novel inputs.
- Never use `BinaryFormatter`, `NetDataContractSerializer`, `SoapFormatter`, or `LosFormatter`. For config and quote data use `System.Text.Json` with `JsonSerializerOptions` that DO NOT enable `TypeNameHandling`-equivalents.
- For hashing of file integrity or cache keys, use `SHA256` or `XxHash64`. Never `MD5` or `SHA1` for security purposes.
- Secrets (any future API keys) MUST come from `DPAPI`-protected user store or environment variables. Never committed, never logged.
- Catch blocks log message plus stack but NEVER log the input that caused them verbatim if the input is a file path under a user profile (redact home dir to `~\...`) or a network URL with query string.

---

## 4. Path-Traversal and Filesystem Hardening

- Every filesystem read, write, copy, or delete of a user- or JSON-supplied path MUST go through `AnimeQuoteWall.Core.Services.SafePath.RequireInsideRoot(candidate, root, sinkName)`.
- Allowed roots are: app data dir, the configured wallpaper directory, the history cache dir. Nothing else is writable.
- Reject symlinks/junctions for write targets (`SafePath.IsInsideRoot` does this automatically by checking `FileAttributes.ReparsePoint`).
- File extensions MUST be validated AND magic bytes sniffed via `SafePath.LooksLikeSupportedImage` before passing user images to ImageMagick or ffmpeg.
- Strip image metadata via Magick.NET before persisting to history. EXIF can contain GPS/PII and crafted ICC profiles have been CVE vectors.

---

## 5. Performance and UI Responsiveness

- Any operation longer than 50 ms goes off the UI thread via `Task.Run` or async I/O.
- Marshal to UI ONLY via `Application.Current.Dispatcher.InvokeAsync(...)` with the smallest possible payload. Never `Invoke` (blocking) from a worker.
- All `ItemsControl`/`ListBox`/`ListView` displaying history thumbnails MUST keep `VirtualizingStackPanel.IsVirtualizing="True"` and `VirtualizationMode="Recycling"`. HistoryPage can hold thousands of entries.
- `BitmapImage` for thumbnails MUST set `DecodePixelWidth` (or `DecodePixelHeight`, not both) to the rendered size.
- Set `BitmapCacheOption.OnLoad` and dispose the source stream after `EndInit()`. Prevents file locks that block users from deleting their own wallpapers.

---

## 6. Accessibility (WCAG 2.2 plus UI Automation)

- Every interactive control without visible text MUST set `AutomationProperties.Name` and, where helpful, `AutomationProperties.HelpText`.
- Icon-only sidebar buttons (collapsed mode) MUST keep their `AutomationProperties.Name` even when the label is hidden. Collapsed sidebar otherwise becomes a wall of unlabeled buttons to UIA.
- Theme tokens MUST maintain a contrast ratio of at least 4.5:1 for body text and at least 3:1 for large or UI text against their backgrounds. Validate when editing `Theme.Light.xaml` or `Theme.Dark.xaml`.
- Every action reachable by mouse MUST be reachable by keyboard. Do not set `Focusable="False"` on actionable elements.
- A visible focus ring MUST be present on all custom buttons (2 px outline using `PrimaryColor` brush). `ButtonStyles.xaml` provides `ModernFocusVisual` for this.

---

## 7. Code Style

- `Nullable` is enabled solution-wide. Do not disable it on new files.
- The null-forgiving operator `!` is banned outside test code and explicitly justified migration sites with a comment `// null-forgiven: <reason>`.
- File-scoped namespaces. `var` only where the right-hand side makes the type obvious. Expression-bodied members only for one-liner properties or methods.
- Public APIs in `AnimeQuoteWall.Core` have XML doc comments on every public member.
- Pre-existing CA1416 (Windows-only platform) warnings are accepted; do not add new platform warnings without a `[SupportedOSPlatform("windows")]` annotation.

---

## 8. Logging and Telemetry

- `App.LogException` writes to `%LOCALAPPDATA%\AnimeQuoteWall\logs\error_<yyyymmdd>.txt`. Use it from every `catch` that is not user-actionable.
- Never log: full file paths under `%USERPROFILE%` (redact to `~\...`), bearer tokens, Steam ticket data, email, machine name.
- Log levels: state changes at Information; recoverable issues at Warning; failed user-visible operations at Error; only Critical when the app must shut down.
- This app does NOT collect telemetry. Do not add a telemetry pipeline without explicit user opt-in and a privacy policy update.

---

## 9. Testing

- Unit tests in a future `AnimeQuoteWall.Core.Tests` project use xUnit, Arrange-Act-Assert, one assert focus per test.
- Services with file/process I/O get integration tests behind a `[Trait("Category","Integration")]` filter so CI can opt in.
- Never write XAML snapshot tests; theme/`DynamicResource` rendering is non-deterministic across DPI.
- Always test: path validation (`SafePath`), magic-byte sniffing, ffmpeg argument construction (`ArgumentList`), theme switch round-trip, per-monitor dispatch.
- Never test: getter/setter trivia, MahApps icon rendering, system wallpaper actually applied (mock the P/Invoke).

---

## 10. Git and PR Hygiene

- Conventional Commits required: `feat:`, `fix:`, `refactor:`, `chore:`, `docs:`, `test:`, `build:`, `ci:`, `perf:`, `security:`. Breaking changes use `!:` and a `BREAKING CHANGE:` footer.
- PR titles must pass conventional-commit lint. Squash-merge into `main`; the squash message is the PR title.
- All commits to `main` MUST be signed (GPG or SSH-signed).
- No `--amend` on shared branches; create a new commit.
- One concern per PR. PRs touching more than 400 lines need a written design note in the description.

---

## 11. CI/CD

GitHub Actions workflow stages (in order, all blocking):

1. `dotnet restore`
2. `dotnet format --verify-no-changes`
3. `dotnet build -c Release --no-restore`
4. `dotnet test --collect:"XPlat Code Coverage"` (once tests exist)
5. `dotnet list package --vulnerable --include-transitive` - fail on any High or Critical

Release workflow additionally: code-sign the produced exe and installer, generate SBOM, upload to GitHub Releases, then to Steamworks.

Pin GitHub Actions to commit SHA, not `@v3`. Supply-chain hardening.

---

## 12. Dependency Hygiene

- Enable NuGet Audit globally via `Directory.Build.props`: `<NuGetAudit>true</NuGetAudit>`, `<NuGetAuditMode>all</NuGetAuditMode>`, `<NuGetAuditLevel>low</NuGetAuditLevel>`.
- Dependabot PRs: review the changelog, run the full test suite locally, never auto-merge majors. Magick.NET and MahApps require manual smoke test of HistoryPage and Sidebar respectively.
- License allowlist for new transitive packages: MIT, Apache-2.0, BSD-2/3, MS-PL. GPL/AGPL/LGPL forbidden because of Steam redistribution.
- Run `dotnet list package --vulnerable --include-transitive` locally before any PR touching a `*.csproj`.

---

## 13. Release and Install

- Production binaries are code-signed with an OV (or Azure Trusted Signing) certificate before the installer is built, then the installer is signed again.
- SmartScreen reputation takes time even with OV; budget for the first weeks of release showing a "Less common app" warning.
- Installer: Inno Setup 6.3+ at `installer/AnimeQuoteWall.iss`. Per-user install option; admin install option. Always offer to launch on finish.
- Auto-update via Velopack manifest in cloud storage plus delta check at startup. Never auto-execute an update from a non-HTTPS source. Validate the manifest signature with a pinned public key.
- For the Steam build, set a `--steam` flag and skip the in-app updater; let Steam patch.

---

## 14. Threat Model - Wallpaper Manager Specifics

- Untrusted inputs: image files (user-supplied), video files (animated wallpapers), JSON quote packs, Wallpaper Engine HTTP responses, ffmpeg stderr.
- Image files: validate magic bytes (`FF D8 FF` JPEG, `89 50 4E 47 0D 0A 1A 0A` PNG, `47 49 46 38` GIF, `42 4D` BMP, `52 49 46 46 ... 57 45 42 50` WebP). Reject anything else even if extension matches.
- Video files: ffmpeg gets paths via `ArgumentList`, NEVER stitched into `Arguments`. Filter graph strings (`-vf`, `-filter_complex`) MUST be constructed from constants - never from user input.
- Re-encode user-supplied images on import (via Magick.NET) to strip polyglot payloads and EXIF.
- Registry: only write `HKCU\Control Panel\Desktop` values `Wallpaper`, `WallpaperStyle`, `TileWallpaper`. Never touch `HKLM`, never write paths outside the validated wallpaper root.
- `SystemParametersInfo` MUST be called with `SPI_SETDESKWALLPAPER` plus `SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE` and on the user's interactive session only.
- Wallpaper Engine: connect ONLY to `127.0.0.1:7070` or `localhost:7070`; reject IPv6 mappings unless explicit; timeout 5 s; treat all responses as untrusted JSON.
- ImageMagick: call `ImageMagickHardening.Apply()` at app startup. Sets width/height limits, memory cap, disk cap; protects against amplification attacks via crafted images.

---

## 15. Pre-Commit / Pre-Done Checklist (MANDATORY before claiming done)

Before any "done", "ready for review", or `git commit`, verify ALL of these and state the result:

1. `dotnet build AnimeQuoteWall.GUI/AnimeQuoteWall.GUI.csproj --no-incremental -v minimal` produced 0 errors, no NEW warnings beyond the pre-existing CA1416 set.
2. `dotnet test` is green; new code has tests where Section 9 requires.
3. `dotnet format --verify-no-changes` is clean.
4. `dotnet list package --vulnerable --include-transitive` returns no High or Critical.
5. Grep diff for emojis (regex `[\x{1F300}-\x{1FAFF}]|[\x{2600}-\x{27BF}]`) returns zero hits.
6. Grep diff for hardcoded hex (regex `#[0-9A-Fa-f]{6,8}`) in `*.xaml` and `*.cs` returns zero hits in production XAML and code-behind.
7. Grep diff for `StaticResource` referencing known theme keys returns zero hits.
8. Grep diff for `ContinueWith(` without `TaskScheduler.Default` returns zero hits in production.
9. Grep diff for empty catches (`catch\s*\{\s*\}` or `catch\s*\(.*?\)\s*\{\s*\}`) returns zero hits.
10. Grep diff for `.Result`, `.Wait()`, `GetAwaiter().GetResult()` in async paths returns zero hits.
11. Grep diff for `UseShellExecute\s*=\s*true` returns zero hits.
12. Grep diff for `Process.Start` using `Arguments =` string concatenation: replace with `ArgumentList`.
13. Grep diff for full home-directory paths in `LogInformation` or `LogError` strings: redacted.
14. Any new public API in Core has XML docs and accepts `CancellationToken`.
15. Commit message uses Conventional Commits; PR title likewise; no emojis; signed.

If ANY item fails, the work is NOT done. Fix it; do not paper over it.

---

## 16. Feature-Completion Checklist (for new features)

Every new user-facing feature MUST satisfy these before being considered shippable:

- Backend service exists in `AnimeQuoteWall.Core/Services/` with XML doc comments.
- Page or dialog uses `DynamicResource` for every color and includes empty/loading/error states.
- Page actions provide non-blocking user feedback via `ToastService` (success, error, warning) - not `MessageBox`.
- Page is keyboard navigable; primary actions have `AutomationProperties.Name`.
- New buttons follow the existing button-style hierarchy: `ModernPrimaryButton`, `ModernSecondaryButton`, `ModernSuccessButton`, `ModernDangerButton`, `ModernWarningButton`, `ModernIconButton`, `ModernPillButton`.
- Async operations propagate `CancellationToken`.
- File operations go through `SafePath`.
- New external dependencies (NuGet, Process.Start binaries, HTTP endpoints) added to the threat model section above.
- Manual smoke test recorded in `docs/RELEASE_CHECKLIST.md`.

---

## Quick Reference - Project Layout

```text
AnimeQuoteWall/
├── AnimeQuoteWall.Core/         # Service layer, models, configuration, no WPF refs
│   ├── Configuration/           # AppConfiguration, settings persistence
│   ├── Interfaces/              # Service contracts
│   ├── Models/                  # Quote, Playlist, WallpaperSettings, etc.
│   ├── Protection/              # Code integrity helpers (not load-bearing)
│   └── Services/                # All business logic. SafePath, ImageMagickHardening, etc.
├── AnimeQuoteWall.GUI/          # WPF host
│   ├── Controls/                # Reusable controls (ToastNotification)
│   ├── Converters/              # IValueConverter implementations
│   ├── Dialogs/                 # Modal windows (AddQuoteDialog)
│   ├── Pages/                   # Navigated pages
│   ├── Resources/Themes/        # Theme.Light.xaml, Theme.Dark.xaml
│   ├── Resources/ButtonStyles.xaml
│   ├── Services/                # WPF-only services (ToastService, BackgroundTaskManager)
│   ├── App.xaml(.cs)
│   ├── SimpleMainWindow.xaml(.cs)
│   └── ThemeManager.cs
├── AnimeQuoteWall.CLI/          # Console entry point (legacy quotes.json updater)
├── installer/                   # Inno Setup script, license RTF, build script, assets
├── docs/                        # Architecture overview, release checklist
├── .github/                     # Issue templates, Dependabot, workflows
├── CLAUDE.md                    # This file
├── README.md                    # Public-facing intro
├── SECURITY.md                  # Disclosure policy
└── CONTRIBUTING.md              # Conventional commits, dev workflow
```
