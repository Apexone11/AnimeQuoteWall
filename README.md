# 🎨 Anime Quote Wallpaper Manager

A powerful desktop application that generates beautiful custom wallpapers featuring anime quotes. Built with .NET 8 and WPF, featuring Wallpaper Engine-inspired functionality including playlists, multi-monitor support, performance optimization, and code protection for Steam release.

![Version](https://img.shields.io/badge/version-1.3.1-blue)
![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![Platform](https://img.shields.io/badge/platform-Windows-lightgrey)
![License](https://img.shields.io/badge/license-MIT-green)
![Free](https://img.shields.io/badge/Free-Open%20Source-green)

## ✨ Features

### Core Features
- **🖼️ Wallpaper Generation**: Create beautiful anime quote wallpapers with custom backgrounds
- **💬 Quote Library**: Manage 200+ anime quotes from popular series
- **🌄 Custom Backgrounds**: Add and manage your own background images
- **🎬 Animated Wallpapers**: Support for MP4, WebM, and MOV video wallpapers with Wallpaper Engine integration
- **⚡ One-Click Apply**: Set generated wallpapers as your desktop background instantly
- **🎯 Modern UI**: Clean, intuitive interface with theme support
- **🔄 Live Preview**: See your wallpaper before applying it
- **🌙 Dark Mode**: Follow system theme or choose Light/Dark mode (applies instantly)
- **📁 Custom Paths**: Configure backgrounds folder, quotes file, and output location
- **🎬 Animation Export**: Create animated GIFs or MP4 videos from your wallpapers
- **✂️ Media Editing**: Crop, resize, apply filters, and add text overlays to images and videos
- **🎨 Interactive Effects**: Mouse parallax tracking and time-based color effects
- **✨ Particle Effects**: Add snow, stars, sparkles, and rain effects to wallpapers

### 🆕 Wallpaper Engine-Inspired Features (v1.3.0)

#### 🎵 Playlist System
- **Automatic Wallpaper Rotation**: Create playlists that automatically change wallpapers
- **Multiple Schedule Types**: Interval-based, hourly, daily, or custom schedules
- **Shuffle Mode**: Randomize wallpaper order for variety
- **Playlist Management**: Create, edit, and manage multiple playlists
- **One Active Playlist**: Only one playlist active at a time for simplicity

#### 🖥️ Multi-Monitor Support
- **Primary Monitor**: Set wallpaper on primary monitor only
- **All Monitors**: Extend wallpaper across all monitors
- **Per-Monitor**: Different wallpapers for each monitor
- **Monitor Detection**: Automatic detection of connected monitors
- **Dynamic Support**: Handles monitor connection/disconnection

#### ⚡ Performance Optimization
- **Fullscreen Detection**: Automatically pauses wallpaper changes when fullscreen apps are running
- **Smart Scheduling**: Efficient background processing
- **Resource Management**: Optimized memory usage
- **Background Services**: Non-intrusive wallpaper rotation

#### 🔒 Code Protection
- **String Encryption**: Critical strings encrypted at compile time
- **Code Integrity Checks**: Validates code hasn't been tampered
- **Anti-Tampering**: Protection against modification
- **Method Obfuscation**: Critical algorithms protected

#### 🌍 Windows Compatibility
- **Multi-Version Support**: Windows 7, 8, 8.1, 10, and 11
- **Version Detection**: Automatic Windows version detection
- **Fallback Mechanisms**: Graceful degradation on older systems
- **Hardware Compatibility**: Works on various hardware configurations

## 🆕 What's New in v1.3.1

### Bug Fixes & Stability
- **🔧 Settings Page Fixed**: Fixed critical crash when opening Settings page
- **🛡️ Improved Error Handling**: Better exception handling with user-friendly messages and automatic logging
- **📚 Library Pages Fixed**: Fixed Image Library and Animated Library pages not loading properly
- **⚡ Async Loading**: Implemented async loading for better performance and responsiveness
- **🎨 UI Fixes**: Fixed XAML styling errors that caused application crashes

### Improvements
- **📊 Loading Indicators**: Added loading indicators and empty state messages for better UX
- **🔍 Error Logging**: Automatic error logging to help diagnose issues
- **⚙️ Experimental Features**: Added Settings toggles for experimental features
- **📦 Dependency Updates**: Updated all NuGet packages to latest compatible versions

## 🆕 What's New in v1.3.0

### Major Features
- **🎵 Playlist System**: Automatic wallpaper rotation with scheduling
- **🖥️ Multi-Monitor Support**: Full support for multiple displays
- **🎬 Animated Wallpapers**: Full support for video wallpapers (MP4/WebM/MOV) with Wallpaper Engine integration
- **📹 Video Thumbnails**: Automatic thumbnail generation for video files (no more gray placeholders!)
- **⚡ Performance Optimization**: Smart fullscreen detection, async loading, and faster startup
- **✂️ Media Editing**: Crop, resize, filters (blur, glow, sepia, grayscale, vintage), and text overlays
- **🎨 Interactive Effects**: Mouse parallax tracking and time-based color/opacity effects
- **✨ Particle Effects**: Snow, stars, sparkles, and rain effects for enhanced wallpapers
- **🔒 Code Protection**: Encryption and obfuscation for Steam release
- **🌍 Windows Compatibility**: Support for Windows 7 through 11

### Previous Updates

#### v1.2.0
- **🎬 Animation Export**: Create animated GIFs and MP4 videos
- **🎨 Modern Sidebar Navigation**: Clean sidebar navigation with page-based UI
- **⚙️ Animation Settings**: Configure FPS, duration, motion type, and easing
- **📤 Multiple Export Formats**: Export as GIF (animated) or MP4 (video)

#### v1.1.0
- **Theme System**: Full dark mode support with system theme detection
- **Custom Paths**: Browse dialogs for backgrounds, quotes, and output paths
- **Settings UI**: Complete settings panel with theme selector and path management
- **Live Updates**: Theme changes apply immediately without restart

## 🚀 Quick Start

### Prerequisites

- **Windows**: 7, 8, 8.1, 10, or 11
- **.NET Runtime**: .NET 8.0 Runtime or SDK ([Download here](https://dotnet.microsoft.com/download/dotnet/8.0))

### Installation

1. **Download the latest release** from GitHub Releases
2. **Run the application**:
   - **Easiest**: Double-click `AnimeQuoteWall.exe`
   - **Or**: Double-click `Launch-AnimeQuoteWall.bat`
   - **Or using terminal**:
     ```bash
     cd AnimeQuoteWall.GUI
     dotnet run
     ```

## 🎮 How to Use

### Basic Usage

1. **Generate Wallpaper**
   - Click "⚡ Generate New Wallpaper" to create a random wallpaper
   - The preview will show your new wallpaper

2. **Apply Wallpaper**
   - Click "✨ Apply as Wallpaper" to set it as your desktop background

3. **Manage Quotes**
   - Go to the "💬 Quotes" tab
   - Add new quotes with "➕ Add Quote"
   - Select and delete unwanted quotes

4. **Manage Backgrounds**
   - Go to the "🌄 Backgrounds" tab
   - Add images with "➕ Add Images"
   - Delete backgrounds you don't want

### 🎵 Playlist Features

1. **Create a Playlist**
   - Go to the "🎵 Playlists" tab
   - Click "➕ Create Playlist"
   - Add wallpaper entries (quote + background combinations)
   - Configure schedule (interval, hourly, daily, or custom)

2. **Enable Playlist**
   - Select a playlist from the list
   - Click "▶️ Enable" to start automatic rotation
   - The playlist will change wallpapers according to schedule

3. **Schedule Types**
   - **Interval**: Change every X seconds (default: 5 minutes)
   - **Hourly**: Change at the top of every hour
   - **Daily**: Change at a specific time each day
   - **Custom**: Advanced scheduling options

### 🖥️ Multi-Monitor Setup

1. **Configure Monitor Mode**
   - Go to Settings → Multi-Monitor
   - Choose: Primary, All Monitors, or Per-Monitor
   - Select which monitors to use

2. **Monitor Modes**
   - **Primary**: Wallpaper on primary monitor only
   - **All Monitors**: Same wallpaper extended across all monitors
   - **Per-Monitor**: Different wallpaper for each monitor

### ⚡ Performance Features

- **Auto-Pause on Fullscreen**: Automatically pauses wallpaper changes when fullscreen apps are detected
- **Enable in Settings**: Toggle "Auto-pause on fullscreen" in Settings
- **Smart Detection**: Works with games, videos, and other fullscreen applications

### 🎬 Animation Export

1. **Configure Animation Settings**
   - Go to the "🎬 Animation" tab
   - Set FPS (frames per second)
   - Set duration in seconds
   - Choose motion type (Fade/Slide)
   - Select easing type

2. **Generate Animation**
   - Click "🎬 Generate Animation"
   - Wait for frames to be generated
   - Click "💾 Export" to save as GIF or MP4

### 🎬 Animated Wallpapers

1. **Add Animated Wallpapers**
   - Go to the "🎬 Animated Wallpapers" tab
   - Click "➕ Add Videos" to add MP4, WebM, or MOV files
   - Thumbnails are automatically generated for video files

2. **Apply Animated Wallpaper**
   - Select a video wallpaper from the list
   - Click "✨ Apply" to set it as your desktop background
   - Works with Wallpaper Engine (if installed) or falls back to static frame extraction

3. **Wallpaper Engine Integration**
   - Automatically detects Wallpaper Engine installation
   - Uses Web API (port 7070) or CLI for applying wallpapers
   - Provides detailed feedback about Wallpaper Engine status

### ✂️ Media Editing

1. **Load Media**
   - Go to the "🎬 Animation" tab
   - Click "📁 Load Media" to select an image or video file

2. **Edit Tools**
   - **Crop**: Select area to crop (X, Y, Width, Height)
   - **Resize**: Set new dimensions (Width, Height)
   - **Filters**: Apply blur, glow, sepia, grayscale, vintage, brightness, or contrast
   - **Text Overlay**: Add anime quotes as text overlay on images/videos

3. **Apply Changes**
   - Click the respective "Apply" button for each tool
   - Changes are applied to the loaded media

### 🎨 Interactive Effects

1. **Mouse Tracking**
   - Enable mouse parallax effects
   - Text and elements follow mouse movement
   - Adjustable intensity and smoothing

2. **Time Effects**
   - Time-based color shifts (day/night transitions)
   - Opacity changes based on time of day
   - Smooth transitions throughout the day

### ✨ Particle Effects

1. **Enable Particles**
   - Go to the "🎬 Animation" tab
   - Scroll to "Particle Effects" section

2. **Configure Particles**
   - **Type**: Choose from Snow, Stars, Sparkles, or Rain
   - **Count**: Set number of particles
   - **Speed**: Adjust particle movement speed
   - **Color**: Click color picker to choose particle color

3. **Apply to Animation**
   - Particles are automatically included when generating animations

## 📁 Project Structure

```
AnimeQuoteWall/
├── AnimeQuoteWall.exe          # Main launcher
├── Launch-AnimeQuoteWall.bat   # Alternative launcher
├── AnimeQuoteWall.CLI/         # Command-line interface
├── AnimeQuoteWall.Core/        # Core business logic
│   ├── Configuration/          # AppConfiguration & settings
│   ├── Models/                 # Data models (Playlist, Quote, etc.)
│   ├── Services/               # Services (Playlist, Schedule, Monitor, etc.)
│   ├── Protection/             # Code protection & encryption
│   └── Interfaces/             # Service interfaces
├── AnimeQuoteWall.GUI/         # WPF Desktop application
│   ├── Resources/
│   │   └── Themes/             # Light & Dark theme resources
│   ├── Pages/                  # Page-based UI (Playlists, etc.)
│   ├── Controls/               # Custom controls
│   └── Services/               # GUI services
├── docs/                        # Documentation
│   ├── PROTECTION_GUIDE.md     # Code protection guide
│   ├── STEAM_RELEASE_CHECKLIST.md
│   ├── WINDOWS_COMPATIBILITY.md
│   └── ...
└── scripts/                     # Utility scripts
```

## 🛠️ Development

### Building from Source

```bash
# Build the entire solution
dotnet build

# Build specific project
cd AnimeQuoteWall.GUI
dotnet build

# Create release build (with protection)
dotnet build -c Release

# Prepare for Steam release
.\scripts\ProtectForSteam.ps1
```

### Code Protection

The project includes comprehensive code protection for Steam release:

- **String Encryption**: Critical strings encrypted
- **Integrity Checks**: Code validation on startup
- **Obfuscation Ready**: ConfuserEx configuration included
- **See**: `docs/PROTECTION_GUIDE.md` for details

## 🔒 Security & Privacy

### Privacy First
- ✅ **No telemetry or tracking**
- ✅ **No network access or external API calls**
- ✅ **No credentials or secrets stored**
- ✅ **All data stored locally** on the user's machine

### Code Protection
- ✅ **String encryption** for sensitive data
- ✅ **Code integrity validation**
- ✅ **Anti-tampering protection**
- ✅ **Method obfuscation** ready
- ✅ **Steam API integration** framework

### Secure Storage
Files are stored in a per-user application data directory:
```
%LOCALAPPDATA%\AnimeQuotes\
├── backgrounds/
├── playlists/
├── quotes.json
├── current.png
└── settings.json
```

See [SECURITY.md](SECURITY.md) for complete security documentation.

## 🎨 Customization

### Theme Settings
- **System Default**: Follows Windows theme (updates automatically)
- **Light**: Always use light theme
- **Dark**: Always use dark theme

### Custom Paths
- **Backgrounds**: Choose any folder containing background images
- **Quotes**: Point to any JSON file with quotes
- **Output**: Set where `current.png` is saved

All paths are validated for security and stored in settings.

## 🌍 Windows Compatibility

### Supported Versions
- ✅ Windows 7 (with .NET 8.0 Runtime)
- ✅ Windows 8
- ✅ Windows 8.1
- ✅ Windows 10 (all versions)
- ✅ Windows 11 (all versions)

### Hardware Support
- ✅ Single monitor setups
- ✅ Multi-monitor setups (2-4+ monitors)
- ✅ Mixed resolutions
- ✅ Different DPI settings

See [docs/WINDOWS_COMPATIBILITY.md](docs/WINDOWS_COMPATIBILITY.md) for details.

## 📋 Roadmap

### Completed ✅
- [x] Playlist system with scheduling
- [x] Multi-monitor support
- [x] Performance optimization
- [x] Code protection framework
- [x] Windows compatibility improvements
- [x] Animation export (GIF/MP4)

### Planned 🔄
- [ ] Steam Workshop integration
- [ ] Cloud save synchronization
- [ ] Custom font selection UI
- [ ] More animation motion types
- [ ] Wallpaper history browser
- [ ] Quote editor improvements

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

See [CONTRIBUTING.md](AnimeQuoteWall.CLI/CONTRIBUTING.md) for guidelines.

## 🐛 Known Issues

- **Experimental Features**: Animated wallpaper apply and per-monitor wallpaper apply are currently disabled by default. Enable them in Settings → Behavior Settings → Experimental Features if you want to test them.
- **Multi-Monitor Different Images**: The feature to generate different images for each monitor is temporarily disabled due to stability issues. It will be re-enabled in a future update.

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Anime quotes sourced from popular anime series
- Inspired by Wallpaper Engine features
- Built with love for the anime community ❤️

## 🚀 Distribution

This project is prepared for free distribution with:
- Code protection and obfuscation
- Professional build configuration
- Comprehensive documentation
- Open source under MIT License

See [docs/RELEASE_CHECKLIST.md](docs/RELEASE_CHECKLIST.md) for release preparation.

---

⭐ **Star this repo if you love anime quotes!** ⭐

**Version**: 1.3.1  
**Last Updated**: 2025-01-27  
**Status**: Production Ready | Free & Open Source
