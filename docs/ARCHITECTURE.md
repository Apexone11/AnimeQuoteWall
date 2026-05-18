# Architecture

AnimeQuoteWall is a WPF .NET 8 desktop application targeting Windows 10 1809 and later. The solution is intentionally small, with a clean separation between a service layer (`AnimeQuoteWall.Core`) and a WPF host (`AnimeQuoteWall.GUI`).

## Solution layout

```
AnimeQuoteWall/
├── AnimeQuoteWall.Core/        # Service layer (no WPF refs)
├── AnimeQuoteWall.GUI/         # WPF host
├── AnimeQuoteWall.CLI/         # Legacy console entry
├── installer/                  # Inno Setup script + build script
├── docs/                       # Documentation
├── .github/                    # CI workflows, issue templates, Dependabot
├── CLAUDE.md                   # Contributor rules
├── CONTRIBUTING.md
├── README.md
└── SECURITY.md
```

## AnimeQuoteWall.Core

| Folder | Purpose |
|---|---|
| `Configuration/` | `AppConfiguration` static service: paths, settings persistence, directory creation |
| `Interfaces/` | Service contracts (IBackgroundService, IQuoteService, IWallpaperService) |
| `Models/` | `Quote`, `Playlist`, `WallpaperSettings`, `WallpaperHistoryEntry`, etc. |
| `Protection/` | `CodeProtection`, `StringEncryption` (not load-bearing; for the Steam build) |
| `Services/` | All business logic |

### Key services

- **`WallpaperService`** - composites a background image with a quote using `System.Drawing`. Produces the final PNG.
- **`WallpaperHistoryService`** - persists generated wallpapers and their metadata. All file paths it consumes are validated by `SafePath`.
- **`PerMonitorWallpaperService`** - assigns a wallpaper to a specific monitor index via `IDesktopWallpaper` COM.
- **`AnimatedWallpaperService`** - integrates with Wallpaper Engine at `http://127.0.0.1:7070`; falls back to extracting the first frame of a GIF for the static wallpaper.
- **`PlaylistService` + `PlaylistWorker`** - manage scheduled rotation. The worker holds a single `Timer` and round-robins through enabled playlists.
- **`QuoteService`** - load/save the quote JSON library; preserves user-added quotes across upgrades.
- **`BackgroundService`** - scans the user's backgrounds folder; deduplication by file hash.
- **`MonitorService`** - enumerates physical displays.
- **`VideoThumbnailService`** - extracts a thumbnail from MP4/WebM via Magick.NET.
- **`SafePath`** - centralized path validation. Every JSON-derived or user-supplied path must pass through this.
- **`ImageMagickHardening`** - one-shot startup hook to set ImageMagick resource limits.

## AnimeQuoteWall.GUI

| Folder | Purpose |
|---|---|
| `Pages/` | Navigated pages: Wallpaper, AnimatedWallpapers, Quotes, Backgrounds, History, Playlists, Settings |
| `Dialogs/` | Modal windows: AddQuoteDialog |
| `Controls/` | Reusable UserControls (ToastNotification) |
| `Converters/` | IValueConverter implementations |
| `Resources/Themes/` | Light/Dark theme dictionaries |
| `Resources/ButtonStyles.xaml` | Modern button and card styles |
| `Services/` | WPF-side services (ToastService, BackgroundTaskManager) |

### Navigation

The shell is `SimpleMainWindow.xaml`. A `Frame` (`ContentFrame`) hosts the active `Page`. Each navigation creates a fresh page instance to avoid stale state from the WPF journal. Selected nav-button state is encoded in the `Button.Tag` property (`"Selected"`) and surfaced by the `SidebarNavButton` `ControlTemplate.Triggers`.

Keyboard shortcuts Ctrl+1 through Ctrl+7 map to the seven pages and are handled by `SimpleMainWindow.Window_PreviewKeyDown`.

### Theming

`ThemeManager.ApplyTheme()` swaps the active `ResourceDictionary` between `Theme.Light.xaml` and `Theme.Dark.xaml`. Both dictionaries expose the same brush keys; pages reference them via `DynamicResource` so theme changes propagate immediately without restart. The system theme is observed via `Microsoft.Win32.SystemEvents.UserPreferenceChanged` when `ThemeMode = System`.

### Toasts

Non-blocking user feedback flows through `AnimeQuoteWall.GUI.Services.ToastService`. It marshals to the UI thread, finds the active window, and appends a `ToastNotification` to a `ToastHost` `Grid` inserted into the window's root panel.

## Data layout

User data and configuration live under `%LOCALAPPDATA%\AnimeQuoteWall\`.

```
%LOCALAPPDATA%\AnimeQuoteWall\
├── settings.json
├── quotes.json
├── backgrounds\
├── wallpapers\
│   ├── current.png
│   └── current_monitor_<n>.png
├── history\
│   ├── metadata.json
│   └── <generated images>.png
├── playlists\
│   └── <sanitized-playlist-id>.json
├── thumbnails\
└── logs\
    └── error_<yyyymmdd>.txt
```

`AppConfiguration.EnsureDirectories()` runs at startup on a background task and creates any missing directory.

## Startup sequence

1. `App.OnStartup`
   - Wire global exception handlers.
   - `CodeProtection.Initialize()`.
   - `ImageMagickHardening.Apply()` - set MagickResourceLimits.
   - `base.OnStartup(e)`.
   - `ThemeManager.ApplyTheme()` plus `StartSystemThemeWatch()`.
   - `AppConfiguration.EnsureDirectories()` on a background task.
   - Construct `SimpleMainWindow` and show.
   - Cleanup old thumbnails on a background task.
2. `SimpleMainWindow.Loaded`
   - Wait for `ContentFrame.Loaded`, then `NavigateToPage("Wallpaper")`.

## Background work

- `PlaylistWorker` holds a single `System.Threading.Timer` and rotates wallpapers when an enabled playlist is due.
- `BackgroundTaskManager` (in GUI) coordinates one-off long-running tasks (history cleanup, thumbnail extraction) and ensures they never run on the UI thread.

## Security boundaries

See `SECURITY.md` for the threat model. Every JSON-supplied or user-supplied filesystem path must be validated by `SafePath`. Every external process invocation must use `ProcessStartInfo.ArgumentList`. Network traffic is limited to `127.0.0.1:7070` (Wallpaper Engine) plus a one-time URL launch to the .NET runtime download page from the installer.
