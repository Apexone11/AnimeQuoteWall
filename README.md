# 🎨 Anime Quote Wallpaper Manager

A beautiful desktop application that generates custom wallpapers featuring anime quotes. Built with .NET 8 and WPF.

![Version](https://img.shields.io/badge/version-1.2.0-blue)
![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![Platform](https://img.shields.io/badge/platform-Windows-lightgrey)
![License](https://img.shields.io/badge/license-MIT-green)

## ✨ Features

- **🖼️ Wallpaper Generation**: Create beautiful anime quote wallpapers with custom backgrounds
- **💬 Quote Library**: Manage 200+ anime quotes from popular series
- **🌄 Custom Backgrounds**: Add and manage your own background images
- **⚡ One-Click Apply**: Set generated wallpapers as your desktop background instantly
- **🎯 Modern UI**: Clean, intuitive interface with theme support
- **🔄 Live Preview**: See your wallpaper before applying it
- **🌙 Dark Mode**: Follow system theme or choose Light/Dark mode (applies instantly)
- **📁 Custom Paths**: Configure backgrounds folder, quotes file, and output location
- **🎬 Animation Export**: Create animated GIFs or MP4 videos from your wallpapers

## 🆕 What's New in v1.2.0

- **🎬 Animation Export**: Create animated GIFs and MP4 videos from your wallpapers
- **🎨 Modern Sidebar Navigation**: Clean sidebar navigation with page-based UI
- **⚙️ Animation Settings**: Configure FPS, duration, motion type, and easing
- **📤 Multiple Export Formats**: Export as GIF (animated) or MP4 (video)
- **🔄 Improved Navigation**: Smooth page transitions and navigation between sections

### Previous Updates (v1.1.0)

- **Theme System**: Full dark mode support with system theme detection
- **Custom Paths**: Browse dialogs for backgrounds, quotes, and output paths
- **Settings UI**: Complete settings panel with theme selector and path management
- **Live Updates**: Theme changes apply immediately without restart

## 🚀 Quick Start

### Prerequisites

- Windows 10/11
- .NET 8.0 Runtime or SDK ([Download here](https://dotnet.microsoft.com/download/dotnet/8.0))

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/YOUR_USERNAME/AnimeQuoteWall.git
   cd AnimeQuoteWall
   ```

2. **Run the application**
   - **Easiest**: Double-click `AnimeQuoteWall.exe`
   - **Or**: Double-click `Launch-AnimeQuoteWall.bat`
   - **Or using terminal**:
     ```bash
     cd AnimeQuoteWall.GUI
     dotnet run
     ```

## 🎮 How to Use

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

5. **Create Animations** 🎬
   - Go to the "🎬 Animation" tab
   - **Configure Settings**:
     - **FPS**: Set frames per second (default: 24)
     - **Duration**: Set animation length in seconds (default: 6)
     - **Motion Type**: Choose "Fade" or "Slide" animation
     - **Easing Type**: Select "Linear", "Ease In", or "Ease Out"
     - **Loop Animation**: Check to create looping animations
   - **Choose Export Format**:
     - **GIF (Animated)**: Creates an animated GIF file
     - **MP4 (Video)**: Creates an MP4 video file (requires FFmpeg)
   - Click "🎬 Generate Animation" to create frames
   - Once frames are generated, click "💾 Export" to save your animation
   - **Note**: For MP4 export, place `ffmpeg.exe` in `AnimeQuoteWall.GUI/Resources/ffmpeg/`

6. **Customize Settings**
   - Go to "⚙️ Settings" tab
   - **Theme**: Choose "System Default", "Light", or "Dark" (applies immediately)
   - **Paths**: Use Browse buttons to customize:
     - Backgrounds folder location
     - Quotes JSON file location
     - Output wallpaper path
   - Click "🔄 Reset to Defaults" to restore default paths

## 📁 Project Structure

```
AnimeQuoteWall/
├── AnimeQuoteWall.exe          # Main launcher
├── Launch-AnimeQuoteWall.bat   # Alternative launcher
├── AnimeQuoteWall.CLI/         # Command-line interface
├── AnimeQuoteWall.Core/        # Core business logic
│   ├── Configuration/          # AppConfiguration & settings
│   ├── Models/                 # Data models
│   ├── Services/               # Services
│   └── Interfaces/             # Service interfaces
├── AnimeQuoteWall.GUI/         # WPF Desktop application
│   ├── Resources/
│   │   └── Themes/             # Light & Dark theme resources
│   ├── SimpleMainWindow.xaml   # Main UI
│   ├── ThemeManager.cs         # Theme switching logic
│   └── App.xaml                # Application configuration
└── docs/                       # Documentation
```

## 🛠️ Development

### Building from Source

```bash
# Build the entire solution
dotnet build

# Build specific project
cd AnimeQuoteWall.GUI
dotnet build

# Create release build
dotnet publish -c Release
```

### Adding Quotes

Edit your quotes file (default: `%LOCALAPPDATA%\AnimeQuotes\quotes.json`) or use the GUI:

```json
{
  "text": "Your quote here",
  "character": "Character Name",
  "anime": "Anime Title"
}
```

## 🎨 Customization

### Theme Settings

The app supports three theme modes:
- **System Default**: Follows Windows theme (updates automatically)
- **Light**: Always use light theme
- **Dark**: Always use dark theme

Settings are saved in `%LOCALAPPDATA%\AnimeQuotes\settings.json`

### Custom Paths

You can customize where the app stores files:
- **Backgrounds**: Choose any folder containing background images
- **Quotes**: Point to any JSON file with quotes
- **Output**: Set where `current.png` is saved

All paths are validated for security and stored in settings.

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 🎬 Animation Export Guide

### Creating Animated Wallpapers

1. **Prepare Your Content**
   - Ensure you have quotes added (Quotes tab)
   - Ensure you have background images added (Backgrounds tab)

2. **Configure Animation Settings**
   - **Frames Per Second (FPS)**: Controls animation smoothness
     - Higher FPS = smoother but larger file size
     - Recommended: 24-30 FPS for GIFs, 30-60 FPS for MP4
   - **Duration**: How long the animation plays
     - Shorter = smaller file size
     - Recommended: 3-10 seconds
   - **Motion Type**:
     - **Fade**: Text fades in and out
     - **Slide**: Text slides across the screen
   - **Easing Type**:
     - **Linear**: Constant speed
     - **Ease In**: Starts slow, speeds up
     - **Ease Out**: Starts fast, slows down
   - **Loop Animation**: Enable for continuous playback

3. **Generate Frames**
   - Click "🎬 Generate Animation"
   - Wait for frames to be generated (progress bar will show)
   - The Export button will be enabled when ready

4. **Export Your Animation**
   - Choose your format:
     - **GIF**: Works everywhere, smaller file size, good for web
     - **MP4**: Better quality, smaller file size, requires FFmpeg
   - Click "💾 Export" and choose save location
   - Your animation will be saved!

### FFmpeg Setup (for MP4 Export)

1. Download FFmpeg from [ffmpeg.org](https://ffmpeg.org/download.html)
2. Extract `ffmpeg.exe` from the download
3. Place it in: `AnimeQuoteWall.GUI/Resources/ffmpeg/ffmpeg.exe`
4. Restart the application
5. MP4 export will now work!

## 📋 Roadmap

- [ ] Custom font selection
- [ ] Multiple quote layouts
- [ ] Image filters and effects
- [ ] Wallpaper history
- [ ] Auto-wallpaper rotation
- [ ] More animation motion types
- [ ] Animation preview before export

## 🐛 Known Issues

- None currently reported

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Anime quotes sourced from popular anime series
- Built with love for the anime community ❤️

---

⭐ **Star this repo if you love anime quotes!** ⭐
