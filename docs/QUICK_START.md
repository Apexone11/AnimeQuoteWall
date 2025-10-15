# 🚀 Quick Start Guide

This guide will help you get AnimeQuoteWall up and running in minutes!

## Prerequisites

✅ Windows 10 or Windows 11  
✅ .NET 10.0 SDK or later ([Download here](https://dotnet.microsoft.com/download))

## Installation Steps

### 1. Download the Project

**Option A: Clone with Git**
```bash
git clone https://github.com/YOUR_USERNAME/AnimeQuoteWall.git
cd AnimeQuoteWall
```

**Option B: Download ZIP**
1. Click the green "Code" button on GitHub
2. Select "Download ZIP"
3. Extract to your desired location

### 2. Build the Application

Open a terminal in the project folder and run:

```bash
dotnet build
```

You should see: `Build succeeded`

### 3. Run for the First Time

```bash
dotnet run
```

**What happens:**
- Creates folder: `C:\Users\YOUR_USERNAME\AppData\Local\AnimeQuotes\`
- Generates sample `quotes.json` with 3 starter quotes
- Creates your first wallpaper with a solid color background
- Sets it as your desktop wallpaper

### 4. Add Your Own Content

#### Add Background Images

1. Open File Explorer
2. Navigate to: `%LOCALAPPDATA%\AnimeQuotes\backgrounds\`
3. Copy your favorite anime wallpapers (PNG, JPG, etc.)
4. Run the application again: `dotnet run`

#### Add More Quotes

1. Navigate to: `%LOCALAPPDATA%\AnimeQuotes\`
2. Open `quotes.json` in a text editor
3. Add your favorite anime quotes:

```json
[
  {
    "Text": "The world isn't perfect, but it's there for us, trying the best it can. That's what makes it so damn beautiful.",
    "Character": "Roy Mustang",
    "Anime": "Fullmetal Alchemist"
  },
  {
    "Text": "Your quote here",
    "Character": "Character Name",
    "Anime": "Anime Title"
  }
]
```

4. Save and run again!

## Usage Examples

### Generate a New Wallpaper

Just run:
```bash
dotnet run
```

Each time you run it, you get:
- A new random quote
- A new random background
- 16 animation frames saved for future use

### View Generated Files

All files are in: `%LOCALAPPDATA%\AnimeQuotes\`

- `current.png` - Your current wallpaper
- `backgrounds\` - Your background images
- `frames\` - Animation frames (organized by timestamp)
- `quotes.json` - Your quote database

### Publishing a Standalone Executable

Want a single `.exe` file you can run without the .NET command?

```bash
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

Find your executable at:
```
bin\Release\net10.0\win-x64\publish\AnimeQuoteWall.exe
```

Double-click to run!

## Automation

### Run on Startup

1. Press `Win + R`
2. Type: `shell:startup`
3. Create a shortcut to `AnimeQuoteWall.exe` in this folder

### Run Daily with Task Scheduler

1. Open "Task Scheduler" (search in Start menu)
2. Click "Create Basic Task"
3. Name: "Anime Quote Wallpaper"
4. Trigger: "Daily" (or "At log on")
5. Action: "Start a program"
6. Program: `C:\Path\To\AnimeQuoteWall.exe`
7. Finish!

## Troubleshooting

### "dotnet: command not found"
- Install .NET SDK from https://dotnet.microsoft.com/download
- Restart your terminal

### Wallpaper doesn't change
- See `docs/WALLPAPER_FIX_GUIDE.md`
- Try running as administrator

### Build errors
```bash
dotnet clean
dotnet restore
dotnet build
```

### No background images
- Add images to `%LOCALAPPDATA%\AnimeQuotes\backgrounds\`
- Supported formats: PNG, JPG, JPEG, BMP, GIF

## Next Steps

✨ Customize text styles in `Program.cs`  
📚 Read the full [README.md](README.md)  
🎨 Check out [ANIME_FONT_GUIDE.md](docs/ANIME_FONT_GUIDE.md) for font tips  
🤝 Contribute: [CONTRIBUTING.md](CONTRIBUTING.md)

## Support

Need help?
- 📖 Check [docs/](docs/) folder for detailed guides
- 🐛 Report issues on [GitHub Issues](https://github.com/YOUR_USERNAME/AnimeQuoteWall/issues)
- 💬 Ask questions in [Discussions](https://github.com/YOUR_USERNAME/AnimeQuoteWall/discussions)

---

**Enjoy your anime quote wallpapers! 🎌**
