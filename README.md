# Anime Quote Wallpaper Manager

A professional Windows desktop application for generating, organizing, and applying anime-quote wallpapers across single or multiple monitors. Supports static images, animated wallpapers, scheduled rotations, and a personal quote library.

![Version](https://img.shields.io/badge/version-1.3.1-blue)
![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-lightgrey)
![License](https://img.shields.io/badge/license-MIT-green)

---

## Overview

Anime Quote Wallpaper Manager renders custom desktop wallpapers by compositing a chosen background image with a quote drawn from a curated library. Once generated, wallpapers can be applied immediately, saved into a history, organized into auto-rotating playlists, or exported as animated GIFs and MP4s.

The app targets contributors and end users who want a polished, theme-aware tool that respects local data, avoids telemetry, and ships with a signed installer.

---

## Highlights

- Generate custom wallpapers from a personal quote library and background pool
- Apply per-monitor wallpapers across any number of displays
- Animated wallpaper support (GIF, MP4, WebM, MOV) with optional Wallpaper Engine integration
- Wallpaper history with restore, delete, and bulk-clear operations
- Playlists with scheduled auto-rotation
- Light and dark themes with hot-reload switching
- Accessibility-first UI: keyboard navigation, focus rings, automation properties
- No telemetry; all user data is stored locally under `%LOCALAPPDATA%\AnimeQuoteWall\`

---

## Requirements

- Windows 10 1809 (build 17763) or newer, x64
- .NET 8 Desktop Runtime (the installer offers to fetch it if missing): <https://dotnet.microsoft.com/en-us/download/dotnet/8.0/runtime>
- Optional: Wallpaper Engine from Steam (enables MP4/WebM animated wallpapers)
- Optional: FFmpeg binary at `Resources/ffmpeg/ffmpeg.exe` for video thumbnail extraction (bundled in the installer)

---

## Installation

### End-user (signed installer)

1. Download `AnimeQuoteWall-Setup-<version>.exe` from the Releases page.
2. Double-click the installer. Accept the license, choose an install directory, and let the wizard complete.
3. Launch the app from the Start menu or the desktop shortcut.

### From source (developers)

```powershell
git clone https://github.com/Apexone11/AnimeQuoteWall.git
cd AnimeQuoteWall
dotnet restore
dotnet build AnimeQuoteWall.GUI/AnimeQuoteWall.GUI.csproj -c Debug
dotnet run --project AnimeQuoteWall.GUI/AnimeQuoteWall.GUI.csproj
```

To produce a release installer locally:

```powershell
cd installer
./build.ps1
```

`build.ps1` publishes the app framework-dependent (`win-x64`), then invokes Inno Setup to produce `installer/dist/AnimeQuoteWall-Setup-<version>.exe`.

---

## Usage

### Generating wallpapers

1. Open the Static Generator page (Ctrl+1).
2. Choose a target monitor or accept the default.
3. Click "Generate" to render a new wallpaper using a random quote and background.
4. Click "Apply" to set it as the system wallpaper.

### Managing your quote library

1. Open the Quotes page (Ctrl+3).
2. Click "Add Quote" to open the themed dialog.
3. Provide the quote text, character name, and an optional anime name.
4. Use category filters or favorites to organize.

### Animated wallpapers

1. Open the Animated Library page (Ctrl+2).
2. Add GIF, MP4, WebM, or MOV files.
3. Click "Apply Animated" to set the selected entry. MP4 and WebM require Wallpaper Engine; GIF falls back to a static first-frame.

### Playlists and rotation

1. Open the Playlists page (Ctrl+6).
2. Create a playlist; add saved history entries or wallpaper packs.
3. Enable the playlist and configure the rotation interval. The background worker will rotate the wallpaper automatically.

### History

The History page (Ctrl+5) lists every generated wallpaper, sorted newest first. Restore a previous wallpaper with one click, delete individual entries, or clear the entire history.

### Keyboard shortcuts

| Shortcut | Action |
|----------|--------|
| Ctrl+1 | Static Wallpaper Generator |
| Ctrl+2 | Animated Library |
| Ctrl+3 | Quotes |
| Ctrl+4 | Image Library |
| Ctrl+5 | History |
| Ctrl+6 | Playlists |
| Ctrl+7 | Settings |

---

## Configuration

User data and configuration live under `%LOCALAPPDATA%\AnimeQuoteWall\`:

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
│   └── <playlist-id>.json
├── thumbnails\
└── logs\
    └── error_<yyyymmdd>.txt
```

All directories are created lazily at startup. Paths can be redirected from the Settings page.

---

## Architecture

The solution is split into three projects:

- `AnimeQuoteWall.Core` - Service layer, models, and configuration. No WPF references; can be unit-tested in isolation.
- `AnimeQuoteWall.GUI` - WPF host. Pages, dialogs, themes, and code-behind.
- `AnimeQuoteWall.CLI` - Console entry point for headless quote management (legacy).

Key services:

- `WallpaperService` - renders the final wallpaper image using `System.Drawing`.
- `WallpaperHistoryService` - persists and restores generated wallpapers (with `SafePath` enforcement).
- `PerMonitorWallpaperService` - dispatches a wallpaper to a specific monitor index.
- `AnimatedWallpaperService` - integrates with Wallpaper Engine and falls back to first-frame static rendering for GIFs.
- `PlaylistService` and `PlaylistWorker` - manage rotation lists and the background timer that drives them.
- `SafePath` - centralized filesystem path validation (path traversal protection).
- `ImageMagickHardening` - sets ImageMagick resource limits at startup.

See `docs/ARCHITECTURE.md` for a deeper dive.

---

## Security and privacy

- No telemetry, no analytics, no remote endpoints other than the optional Wallpaper Engine localhost API.
- All user files are kept under `%LOCALAPPDATA%\AnimeQuoteWall\`.
- Image imports are validated by magic-byte sniffing before being passed to ImageMagick.
- External processes (FFmpeg, Wallpaper Engine) are invoked through `ProcessStartInfo.ArgumentList` to prevent argument injection.
- See `SECURITY.md` for the disclosure policy.

---

## Contributing

See `CONTRIBUTING.md`. The short version:

1. Fork and create a feature branch.
2. Read `CLAUDE.md` - the project has hard rules around emojis, hardcoded colors, async patterns, and security.
3. Follow Conventional Commits (`feat:`, `fix:`, `docs:`, `refactor:`, etc.).
4. Run the pre-commit checklist in `CLAUDE.md` Section 15 before opening a PR.

---

## License

MIT. See `LICENSE`.

The bundled FFmpeg binary is provided under its own license (LGPL v2.1+). MahApps.Metro.IconPacks (MIT), Magick.NET (Apache-2.0), SixLabors.ImageSharp (Apache-2.0/Six Labors Split License).

---

## Links

- Repository: <https://github.com/Apexone11/AnimeQuoteWall>
- Issues: <https://github.com/Apexone11/AnimeQuoteWall/issues>
- Releases: <https://github.com/Apexone11/AnimeQuoteWall/releases>
