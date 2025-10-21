# Changelog

All notable changes to AnimeQuoteWall will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
- Built with .NET 10.0 and C#
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

## [Unreleased]

### Planned Features
- GIF export from animation frames
- GUI configuration tool
- Quote management interface
- Multiple wallpaper themes
- Multi-monitor support
- Quote of the day feature
- Custom font selection UI
- Color scheme presets

---

## Version History

### Version 1.0.0 (Current)
- First stable release
- Core functionality complete
- Ready for public use

### Future Versions

**v1.1.0** (Planned)
- GIF export functionality
- Enhanced error handling
- Performance improvements

**v2.0.0** (Future)
- GUI application
- Quote editor
- Theme system
- Multi-monitor support

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
2. No migration needed for v1.x updates
3. Check CHANGELOG for breaking changes in major versions

---

**Note**: For detailed commit history, see the [GitHub commit log](https://github.com/YOUR_USERNAME/AnimeQuoteWall/commits/main).
