# Changelog

All notable changes to AnimeQuoteWall will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.3.0] - 2025-01-27

### Added
- **Playlist System**: Complete playlist management with automatic wallpaper rotation
  - Create, edit, and delete playlists
  - Multiple schedule types (Interval/Hourly/Daily/Custom)
  - Shuffle mode for random wallpaper order
  - One active playlist at a time
  - Playlist persistence in JSON format
- **Multi-Monitor Support**: Full support for multiple displays
  - Primary monitor mode
  - All monitors mode (extended wallpaper)
  - Per-monitor mode (different wallpapers)
  - Automatic monitor detection
  - Dynamic monitor connection/disconnection support
- **Performance Optimization**: Smart performance features
  - Fullscreen detection using Windows API
  - Auto-pause wallpaper changes when fullscreen apps running
  - Background services for non-intrusive operation
  - Resource optimization
- **Code Protection**: Comprehensive code protection framework
  - String encryption for critical strings
  - Code integrity validation
  - Anti-tampering detection
  - Method protection with `[DebuggerStepThrough]` attributes
  - Distribution framework
- **Windows Compatibility**: Comprehensive Windows support
  - Windows 7, 8, 8.1, 10, and 11 support
  - Automatic version detection
  - Version-specific optimizations
  - Fallback mechanisms for older systems
  - Hardware compatibility across configurations
- **Animated Wallpapers**: Full support for video wallpapers (MP4, WebM, MOV)
  - Wallpaper Engine integration (Web API and CLI support)
  - Automatic thumbnail generation for video files
  - Video format detection and validation
  - Fallback to static frame extraction if Wallpaper Engine unavailable
  - Detailed user feedback about Wallpaper Engine status
- **Media Editing**: Comprehensive image and video editing tools
  - Crop tool with X, Y, Width, Height selection
  - Resize tool with dimension configuration
  - Filter effects: Blur, Glow, Sepia, Grayscale, Vintage, Brightness, Contrast
  - Text overlay support for adding quotes to images/videos
  - ImageMagick integration for advanced processing
- **Interactive Effects**: Dynamic effects based on user interaction and time
  - Mouse tracking and parallax effects
  - Time-based color shifts (day/night transitions)
  - Opacity changes based on time of day
  - Smooth transitions and animations
- **Particle Effects**: Visual enhancement effects
  - Snow particles
  - Star particles
  - Sparkle particles
  - Rain particles
  - Configurable count, speed, and color
- **Performance Improvements**: Enhanced loading and responsiveness
  - Asynchronous loading with batch processing
  - Incremental UI updates for better perceived performance
  - Loading indicators for long operations
  - Optimized image loading with thumbnail caching
  - Faster application startup with deferred initialization
  - Background thumbnail cleanup
- **GUI Improvements**: Enhanced user experience
  - Video thumbnail display (replaces gray placeholders)
  - Play icon overlay for video files
  - Loading indicators for async operations
  - Optimized spacing and layout
  - High-quality image rendering with caching
- **New Services**:
  - `PlaylistService`: Playlist CRUD operations
  - `ScheduleService`: Schedule calculation and validation
  - `PlaylistWorker`: Background wallpaper rotation
  - `MonitorService`: Multi-monitor detection and management
  - `PerformanceMonitorService`: Fullscreen detection
  - `WindowsCompatibilityHelper`: Windows version compatibility
  - `WallpaperSettingHelper`: Wallpaper setting with validation
  - `AnimatedWallpaperService`: Animated wallpaper management with Wallpaper Engine integration
  - `VideoThumbnailService`: Video thumbnail generation and caching
  - `MediaEditingService`: Image and video editing (crop, resize, filters, text overlay)
  - `MouseTrackingService`: Mouse parallax effects
  - `TimeEffectService`: Time-based color and opacity effects
  - `ParticleSystemService`: Particle effect generation
- **New Models**:
  - `Playlist`: Playlist data model with scheduling
  - `AnimationProfile`: Animation configuration with enhanced effects
  - `WallpaperHistoryEntry`: History tracking
  - `ParticleSettings`: Particle effect configuration
  - `InteractiveSettings`: Interactive effects configuration
  - `ImageEffectSettings`: Image effect configuration
- **New Pages**:
  - `PlaylistsPage`: Complete playlist management UI
  - `HistoryPage`: Wallpaper history browser
  - `AnimatedWallpapersPage`: Animated wallpaper management with thumbnail display
- **New Converters**:
  - `ImagePathConverter`: Safe image loading with error handling and performance optimization
  - `FormatToVisibilityConverter`: Format-based visibility for video play icons
- **Protection Framework**:
  - `StringEncryption`: String encryption utility
  - `CodeProtection`: Code integrity validation
  - `AssemblyInfo`: Assembly protection metadata

### Changed
- Enhanced `AppConfiguration.cs` with playlist and multi-monitor settings
- Updated `WallpaperService` to support multi-monitor scenarios and enhanced animations
- Improved error handling across all services
- Enhanced settings UI with new options
- Updated build configuration for code protection
- Optimized `SimpleMainWindow` for faster startup with deferred initialization
- Enhanced `AnimatedWallpapersPage` with async loading and thumbnail generation
- Improved `AnimationPage` with media editing tools and interactive effects
- Updated `AnimationService` to support particle and interactive effects
- Enhanced `ImagePathConverter` with performance optimizations (DecodePixelWidth/Height, DelayCreation)
- Improved application startup with background directory creation and thumbnail cleanup

### Security
- Implemented string encryption for sensitive data
- Added code integrity validation on startup
- Added anti-tampering detection
- Protected critical algorithms with method attributes
- Enhanced path validation
- Improved Windows compatibility security

### Performance
- Async loading with batch processing (5 files at a time)
- Incremental UI updates for better perceived performance
- Optimized image loading with thumbnail caching
- Faster application startup (deferred initialization)
- Background thumbnail cleanup (30-day retention)
- Reduced memory usage with optimized image decoding

### Documentation
- Updated README.md with all new features (animated wallpapers, media editing, interactive effects, particle effects)
- Updated SECURITY.md with code protection details
- Added PROTECTION_GUIDE.md
- Added STEAM_RELEASE_CHECKLIST.md
- Added WINDOWS_COMPATIBILITY.md
- Updated CHANGELOG.md with comprehensive feature list
- Updated all documentation files

## [1.2.0] - 2025-XX-XX

### Added
- **Animation Export**: Create animated GIFs and MP4 videos from wallpapers
  - GIF export using ImageMagick
  - MP4 export using FFmpeg
  - Animation settings (FPS, duration, motion type, easing)
  - Frame generation with progress reporting
- **Modern Sidebar Navigation**: Clean sidebar navigation with page-based UI
- **Animation Settings**: Configure FPS, duration, motion type, and easing
- **Multiple Export Formats**: Export as GIF (animated) or MP4 (video)
- **Improved Navigation**: Smooth page transitions and navigation between sections

### Changed
- Refactored UI to use page-based navigation
- Enhanced animation generation with better effects
- Improved frame generation performance

### Technical
- Added ImageMagick integration for GIF export
- Added FFmpeg support for MP4 export
- Enhanced animation frame generation
- Improved memory management during animation creation

## [1.1.0] - 2025-XX-XX

### Added
- **Theme System**: Full dark mode support with system theme detection
  - Light/Dark themes
  - System theme detection (Windows 10/11)
  - Instant theme switching (no restart required)
  - Theme persistence across sessions
- **Custom Path Configuration**: Browse dialogs for file locations
  - Browse dialog for backgrounds folder
  - Browse dialog for quotes JSON file
  - Browse dialog for output wallpaper path
  - Reset to defaults button
  - All paths validated for security
- **Settings UI**: Complete settings panel
  - Theme mode selector (System/Light/Dark)
  - Path display and management
  - Default paths information
- **Secure File Storage**: Default location in AppData
  - Uses `%LOCALAPPDATA%\AnimeQuotes\`
  - Automatic directory creation
  - Multi-user safe

### Changed
- Moved default storage to AppData folder
- Enhanced path validation and security
- Improved settings persistence
- Updated UI with settings tab

### Security
- Implemented secure default paths
- Added path validation to prevent directory traversal
- Enhanced input validation
- Improved error handling

## [1.0.0] - 2025-10-15

### Added
- Initial release of AnimeQuoteWall
- Dynamic quote rendering with anime-style text effects
- Custom background image support
- Animation frame generation (16 frames with pulsing effects)
- JSON-based quote database system
- Random quote and background selection
- Automatic wallpaper setting via Windows API
- Responsive text sizing based on screen resolution
- Multiple font fallback system
- Text wrapping for long quotes
- Rounded panel design with gradient effects
- Glow and outline text effects
- Sample quotes from popular anime series

### Features
- **Quote System**: Load and manage quotes from JSON file
- **Background Support**: Use custom anime wallpapers as backgrounds
- **Frame Generation**: Create 16 animation frames with pulse effects
- **Auto Wallpaper**: Automatically set desktop wallpaper on Windows
- **Responsive Design**: Text scales based on resolution
- **Font Fallback**: Graceful degradation through font options
- **Privacy**: All data stored locally, no telemetry

### Technical
- Built with .NET 8.0 and C#
- Uses System.Drawing for image processing
- Win32 API integration for wallpaper setting
- Cross-resolution support (tested on 1920x1080, 2560x1440)
- Optimized image processing pipeline

### Documentation
- Comprehensive README.md
- MIT License
- Contributing guidelines
- Security policy
- Code documentation and guides

---

## Version History Summary

### Version 1.3.0 (Current) - Major Update
- ✅ Playlist system with scheduling
- ✅ Multi-monitor support
- ✅ Performance optimization
- ✅ Code protection framework
- ✅ Windows compatibility improvements

### Version 1.2.0
- ✅ Animation export (GIF/MP4)
- ✅ Modern sidebar navigation
- ✅ Animation settings

### Version 1.1.0
- ✅ Theme system
- ✅ Custom paths
- ✅ Settings UI

### Version 1.0.0
- ✅ Initial release
- ✅ Core wallpaper generation

---

## Update Instructions

### Updating from Source

```bash
git pull origin main
dotnet build
```

### Migrating Data

If you're upgrading and want to keep your data:

1. Your quotes and backgrounds are safe in `%LOCALAPPDATA%\AnimeQuotes\`
2. Playlists are stored in `%LOCALAPPDATA%\AnimeQuotes\playlists\`
3. No migration needed for v1.x updates
4. Check CHANGELOG for breaking changes in major versions

### Updating from v1.2.0 to v1.3.0

- New `playlists/` folder will be created automatically
- Existing quotes and backgrounds remain unchanged
- Settings file will be updated with new options
- No data loss during upgrade

---

## Planned Features

### Future Versions

**v1.4.0** (Planned)
- Steam Workshop integration
- Cloud save synchronization
- Custom font selection UI
- More animation motion types

**v2.0.0** (Future)
- Advanced quote editor
- Wallpaper templates
- Community features
- Enhanced customization options

---

**Note**: For detailed commit history, see the [GitHub commit log](https://github.com/YOUR_USERNAME/AnimeQuoteWall/commits/main).

**Last Updated**: 2025-01-27  
**Current Version**: 1.3.0  
**Status**: Production Ready | Free & Open Source
