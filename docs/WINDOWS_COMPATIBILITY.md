# Windows Compatibility Guide

This document outlines the Windows version compatibility and cross-platform considerations for AnimeQuoteWall.

## Supported Windows Versions

- **Windows 7** (with .NET 8.0 Runtime)
- **Windows 8**
- **Windows 8.1**
- **Windows 10** (all versions)
- **Windows 11** (all versions)

## Compatibility Features

### 1. Windows Version Detection

The application automatically detects the Windows version and adjusts behavior accordingly:

- **Windows 7/8/8.1**: Basic features with fallbacks for newer APIs
- **Windows 10**: Full feature support including dark mode detection
- **Windows 11**: Optimized taskbar detection and full feature support

### 2. Monitor Detection

**Multi-Monitor Support:**
- Automatically detects all connected monitors
- Handles dynamic monitor connections/disconnections
- Falls back to primary monitor if detection fails
- Supports various resolutions and DPI settings

**Fallbacks:**
- If monitor detection fails, defaults to 1920x1080 resolution
- Single monitor systems work seamlessly
- Multi-monitor setups (2-4+ monitors) are supported

### 3. Screen Resolution Handling

The application uses multiple methods to detect screen resolution:

1. **Primary Method**: Windows Forms Screen API (most reliable)
2. **Fallback Method**: Windows API GetSystemMetrics
3. **Default Fallback**: 1920x1080 if all methods fail

This ensures compatibility across different hardware configurations.

### 4. Theme Detection

**Windows 10/11:**
- Full dark mode detection via registry
- Automatic theme switching support

**Windows 7/8/8.1:**
- Defaults to light theme (no native dark mode)
- Manual theme selection still works

### 5. Fullscreen Detection

**Compatibility:**
- Works on Windows 7 and later
- Adjusts tolerance based on Windows version:
  - Windows 11: 40px taskbar tolerance
  - Windows 10/8/7: 50px taskbar tolerance
- Handles edge cases gracefully (no crashes on detection failure)

### 6. Wallpaper Setting

**Supported Formats:**
- BMP, JPG, JPEG, PNG, GIF

**Error Handling:**
- Validates file existence before setting
- Checks file size limits (256MB max)
- Handles permission errors gracefully
- Provides helpful error messages for:
  - Group policy restrictions
  - Windows slideshow mode conflicts
  - File access issues

**Compatibility:**
- Works on Windows 7, 8, 8.1, 10, and 11
- Uses standard Windows API (SystemParametersInfo)
- Handles path length limitations

## Hardware Compatibility

### Minimum Requirements

- **RAM**: 512MB available
- **Storage**: 50MB for application + user data
- **Display**: Any resolution (minimum 800x600 recommended)
- **Graphics**: Basic graphics support (no GPU required)

### Recommended Requirements

- **RAM**: 2GB+ available
- **Storage**: 500MB+ for backgrounds and wallpapers
- **Display**: 1920x1080 or higher
- **Multi-Monitor**: Supported but not required

### Supported Configurations

- **Single Monitor**: Fully supported
- **Dual Monitor**: Fully supported
- **Triple+ Monitor**: Supported (tested up to 4 monitors)
- **Mixed Resolutions**: Supported (each monitor uses its own resolution)
- **Different DPI Settings**: Supported (handled automatically)

## Error Handling

### Graceful Degradation

The application is designed to continue working even when some features fail:

1. **Monitor Detection Fails**: Falls back to default resolution
2. **Fullscreen Detection Fails**: Assumes not fullscreen (playlists continue)
3. **Theme Detection Fails**: Defaults to light theme
4. **Wallpaper Setting Fails**: Shows helpful error message, doesn't crash

### User-Friendly Error Messages

All errors provide clear, actionable messages:
- Explains what went wrong
- Suggests possible solutions
- Doesn't expose technical details unnecessarily

## Performance Considerations

### Resource Usage

- **CPU**: Minimal (only during wallpaper generation)
- **Memory**: Efficient caching system prevents memory leaks
- **Disk I/O**: Optimized with image caching
- **Network**: None (fully offline)

### Background Operations

- Playlist rotation runs in background thread
- Fullscreen detection polls every 2 seconds (configurable)
- No impact on system performance during idle

## Testing Recommendations

When testing on different systems:

1. **Test on different Windows versions** (if possible)
2. **Test with single and multiple monitors**
3. **Test with different screen resolutions**
4. **Test with group policy restrictions** (if applicable)
5. **Test with Windows slideshow mode enabled/disabled**

## Known Limitations

1. **Windows 7/8/8.1**: No automatic dark mode detection (manual selection works)
2. **Group Policy**: Wallpaper changes may be blocked by IT policies
3. **Windows Slideshow**: May override manual wallpaper changes
4. **Very Old Hardware**: May experience slower wallpaper generation

## Troubleshooting

### Wallpaper Not Changing

1. Check Windows slideshow mode (disable in Settings)
2. Check group policy restrictions
3. Verify file permissions
4. Ensure file format is supported (BMP, JPG, PNG, GIF)

### Monitor Detection Issues

1. Application will fall back to default resolution
2. Check Windows display settings
3. Restart application if monitors were recently connected/disconnected

### Performance Issues

1. Reduce playlist rotation frequency
2. Use smaller background images
3. Disable fullscreen detection if not needed
4. Check available disk space

## Future Compatibility

The application is designed with forward compatibility in mind:

- Uses standard Windows APIs (not deprecated)
- Follows .NET 8.0 best practices
- Ready for future Windows updates
- Modular design allows easy updates

