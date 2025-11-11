# Changelog

All notable changes to Anime Quote Wallpaper Manager will be documented in this file.

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

