# 🎌 AnimeQuoteWall

A C# desktop application that automatically generates and sets beautiful anime quote wallpapers on Windows. Features dynamic text rendering with animation frame generation for future GIF support.

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)
![Platform](https://img.shields.io/badge/Platform-Windows-blue?logo=windows)
![License](https://img.shields.io/badge/License-MIT-green)
![Language](https://img.shields.io/badge/Language-C%23-239120?logo=csharp)

## ✨ Features

- 🎨 **Dynamic Quote Rendering** - Beautiful anime-style text with gradients, outlines, and glow effects
- 🖼️ **Custom Backgrounds** - Use your own anime images as wallpaper backgrounds
- 🎞️ **Animation Frame Generation** - Creates 16 frames with pulsing animation effects
- 📚 **Extensive Quote Database** - Easily expandable JSON-based quote system
- 🔄 **Random Selection** - Different quote and background every time you run it
- 💾 **Auto Wallpaper Setting** - Automatically sets your desktop wallpaper via Windows API
- 🎯 **Responsive Design** - Automatically scales text based on screen resolution
- 🌈 **Anime-Style Fonts** - Falls back gracefully through multiple font options

## 📸 Screenshots



## 🚀 Quick Start

### Prerequisites

- Windows 10/11
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download) or later
- (Optional) Anime-style fonts for better aesthetics

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/YOUR_USERNAME/AnimeQuoteWall.git
   cd AnimeQuoteWall
   ```

2. **Build the project**
   ```bash
   dotnet build
   ```

3. **Run the application**
   ```bash
   dotnet run
   ```

## 📖 Usage

### First Run

On first run, the application will:
1. Create necessary directories at `%LOCALAPPDATA%\AnimeQuotes\`
2. Generate a sample `quotes.json` with 3 starter quotes
3. Create a wallpaper with a solid color background (no background images yet)

### Adding Your Own Content

#### Adding Background Images

1. Navigate to: `C:\Users\YOUR_USERNAME\AppData\Local\AnimeQuotes\backgrounds\`
2. Copy your anime wallpapers (`.jpg`, `.jpeg`, `.png`, `.bmp`, `.gif`)
3. Run the application again

#### Adding Quotes

Edit `%LOCALAPPDATA%\AnimeQuotes\quotes.json`:

```json
[
  {
    "Text": "Your quote here",
    "Character": "Character Name",
    "Anime": "Anime Title"
  }
]
```

### Running Automatically

**Schedule with Task Scheduler (Windows):**

1. Open Task Scheduler
2. Create Basic Task
3. Set trigger (e.g., "Daily" or "At log on")
4. Action: Start a program
5. Program: `dotnet`
6. Arguments: `run --project "PATH_TO_PROJECT\AnimeQuoteWall.csproj"`

## 🎨 Customization

### Text Styling

Modify `DrawQuote()` method to adjust:
- Font size: `float fs = Math.Max(bmp.Height / 30f, 24f);`
- Panel width: `int pw = (int)(bmp.Width * 0.65f);`
- Colors and gradients
- Outline thickness
- Animation effects

### Animation Frames

Frames are saved to: `%LOCALAPPDATA%\AnimeQuotes\frames\TIMESTAMP\`

Adjust frame count in `GenerateQuoteFrames()`:
```csharp
GenerateQuoteFrames(backgroundPath, selectedQuote, 32); // Generate 32 frames
```

## 📁 Project Structure

```
AnimeQuoteWall/
├── Program.cs              # Main application logic
├── AnimeQuoteWall.csproj   # Project configuration
├── quotes.json             # Sample quotes (created on first run)
├── README.md               # This file
├── LICENSE                 # MIT License
├── .gitignore             # Git ignore rules
└── docs/                   # Additional documentation
    ├── ANIME_FONT_GUIDE.md
    ├── FONT_IMPROVEMENTS.md
    ├── LEARNING_GUIDE.md
    ├── TODO.md
    └── WALLPAPER_FIX_GUIDE.md
```

## 🛠️ Technical Details

### Technologies

- **Language**: C# 10.0
- **Framework**: .NET 10.0
- **Graphics**: System.Drawing (GDI+)
- **Platform**: Windows Desktop
- **APIs**: Win32 User32.dll for wallpaper setting

### Key Components

- **Quote Management**: JSON-based quote storage and retrieval
- **Image Processing**: Dynamic bitmap creation and manipulation
- **Text Rendering**: Anti-aliased text with custom styling
- **Animation**: Frame-by-frame generation with sine wave animation
- **Windows Integration**: Direct wallpaper API calls

## 🔒 Privacy & Security

- **No Telemetry**: This application does NOT collect or send any data
- **Local Storage Only**: All data is stored locally on your machine
- **No Network Access**: Application works completely offline
- **No Personal Information**: Your quotes and images stay on your computer
- **Open Source**: Review the code to verify security

### Data Locations

All data is stored locally in:
```
C:\Users\YOUR_USERNAME\AppData\Local\AnimeQuotes\
├── current.png          # Current wallpaper
├── quotes.json          # Your quote collection
├── backgrounds/         # Your background images
└── frames/             # Generated animation frames
```

**Note**: These files are excluded from git via `.gitignore` to protect your privacy.

## 🤝 Contributing

Contributions are welcome! Here's how you can help:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

### Feature Ideas

- [ ] GIF animation export from generated frames
- [ ] GUI configuration tool
- [ ] Quote management interface
- [ ] Multiple wallpaper styles/themes
- [ ] Multi-monitor support
- [ ] Quote of the day feature
- [ ] Web API integration for online quote databases
- [ ] Custom font selection
- [ ] Color scheme presets

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

This means you can:
- ✅ Use it commercially
- ✅ Modify it
- ✅ Distribute it
- ✅ Use it privately
- ✅ Sublicense it

## 🙏 Acknowledgments

- Inspired by the anime community
- Thanks to all quote contributors
- Built with love for anime fans

## 📞 Support

- **Issues**: [GitHub Issues](https://github.com/YOUR_USERNAME/AnimeQuoteWall/issues)
- **Discussions**: [GitHub Discussions](https://github.com/YOUR_USERNAME/AnimeQuoteWall/discussions)

## 🗺️ Roadmap

### Version 1.0 (Current)
- ✅ Basic wallpaper generation
- ✅ Quote system
- ✅ Animation frame generation
- ✅ Background image support

### Version 2.0 (Planned)
- [ ] GIF export functionality
- [ ] GUI application
- [ ] Quote editor
- [ ] Multiple themes

### Version 3.0 (Future)
- [ ] Multi-monitor support
- [ ] Web dashboard
- [ ] Community quote sharing
- [ ] Advanced animations

## 💡 Tips

1. **Best Image Resolution**: Use 1920x1080 or higher resolution backgrounds
2. **Font Installation**: Install anime fonts like "Anime Ace" for authentic styling
3. **Performance**: Large images may take a few seconds to process
4. **Quotes**: Keep quotes under 200 characters for best display
5. **Testing**: Run `dotnet run` multiple times to see different quotes

## 🐛 Known Issues

- Requires Windows (Win32 API dependency)
- Some anime fonts may not render correctly if not installed
- Very long quotes may overflow panel on small screens

## 📚 Additional Documentation

See the `docs/` folder for:
- Font installation guide
- Troubleshooting wallpaper issues
- Learning resources
- Development TODO list

---

**Made with ❤️ for the anime community**

*If you enjoy this project, give it a ⭐ on GitHub!*
