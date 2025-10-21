# 🎨 Anime Quote Wallpaper Manager

A beautiful desktop application that generates custom wallpapers featuring anime quotes. Built with .NET 8 and WPF.

![Version](https://img.shields.io/badge/version-1.0.0-blue)
![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![Platform](https://img.shields.io/badge/platform-Windows-lightgrey)
![License](https://img.shields.io/badge/license-MIT-green)

## ✨ Features

- **🖼️ Wallpaper Generation**: Create beautiful anime quote wallpapers with custom backgrounds
- **💬 Quote Library**: Manage 200+ anime quotes from popular series
- **🌄 Custom Backgrounds**: Add and manage your own background images
- **⚡ One-Click Apply**: Set generated wallpapers as your desktop background instantly
- **🎯 Simple UI**: Clean, intuitive interface that's easy to use
- **🔄 Live Preview**: See your wallpaper before applying it

## 📸 Screenshots

*Coming soon - Add your screenshots here*

## 🚀 Quick Start

### Prerequisites

- Windows 10/11
- .NET 8.0 Runtime or SDK ([Download here](https://dotnet.microsoft.com/download/dotnet/8.0))

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/Apexone11/AnimeQuoteWall.git
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

## 📁 Project Structure

```
AnimeQuoteWall/
├── AnimeQuoteWall.exe          # Main launcher (double-click to run)
├── Launch-AnimeQuoteWall.bat   # Alternative launcher
├── Launch-AnimeQuoteWall.ps1   # PowerShell launcher
├── AnimeQuoteWall.CLI/         # Command-line interface
├── AnimeQuoteWall.Core/        # Core business logic
│   ├── Models/                 # Data models (Quote, WallpaperSettings)
│   ├── Services/               # Services (WallpaperService, QuoteService)
│   └── Interfaces/             # Service interfaces
├── AnimeQuoteWall.GUI/         # WPF Desktop application
│   ├── Resources/              # App icons and resources
│   ├── SimpleMainWindow.xaml   # Main UI
│   └── App.xaml                # Application configuration
├── Launcher/                   # Silent launcher project
├── docs/                       # Documentation
├── scripts/                    # Utility scripts
└── quotes.json                 # Quote database (200+ quotes)
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

### Running Tests

```bash
dotnet test
```

### Adding Quotes

Edit `AnimeQuoteWall.CLI/quotes.json`:

```json
{
  "text": "Your quote here",
  "character": "Character Name",
  "anime": "Anime Title"
}
```

### Adding Backgrounds

Place images in `AnimeQuoteWall.CLI/backgrounds/` or use the GUI to add them.

## 🎨 Customization

### Wallpaper Settings

Modify `AnimeQuoteWall.Core/Models/WallpaperSettings.cs` to customize:
- Background colors
- Text font and size
- Quote positioning
- Image effects

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📋 Roadmap

- [ ] Custom font selection
- [ ] Multiple quote layouts
- [ ] Image filters and effects
- [ ] Wallpaper history
- [ ] Auto-wallpaper rotation

## 🐛 Known Issues

- None currently reported

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Anime quotes sourced from popular anime series
- Icons from various emoji libraries
- Built with love for the anime community ❤️

## 📧 Contact

Abdul Rahman Fornah - [@Apexone11](https://github.com/Apexone11)

Project Link: [https://github.com/Apexone11/AnimeQuoteWall](https://github.com/Apexone11/AnimeQuoteWall)

---

⭐ **Star this repo if you love anime quotes!** ⭐