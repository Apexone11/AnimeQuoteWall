# 🚀 How to Launch Anime Quote Wallpaper Manager

## Quick Start - Choose Your Method:

### Method 1: Double-Click Batch File (Easiest)
**Windows Users:**
1. Double-click `Launch-AnimeQuoteWall.bat`
2. The app will start automatically!

### Method 2: PowerShell Launcher
**For PowerShell Users:**
1. Right-click `Launch-AnimeQuoteWall.ps1`
2. Select "Run with PowerShell"
3. Or open PowerShell and run: `.\Launch-AnimeQuoteWall.ps1`

### Method 3: Direct Build & Run (Developers)
**Using Command Line:**
```bash
cd AnimeQuoteWall.GUI
dotnet run
```

### Method 4: Visual Studio / VS Code
1. Open the solution `AnimeQuoteWall.sln`
2. Set `AnimeQuoteWall.GUI` as the startup project
3. Press F5 or click Run

## Building the Application

To build a standalone executable:
```bash
cd AnimeQuoteWall.GUI
dotnet publish -c Release -r win-x64 --self-contained
```

The executable will be in: `bin/Release/net8.0-windows/win-x64/publish/`

## Requirements

- ✅ .NET 8.0 SDK or Runtime
- ✅ Windows 10/11 (for WPF)
- ✅ 100MB free disk space

## Troubleshooting

**"dotnet is not recognized":**
- Install .NET 8.0 SDK from: https://dotnet.microsoft.com/download

**App won't start:**
1. Make sure you're in the correct folder
2. Run `dotnet restore` in the AnimeQuoteWall.GUI folder
3. Run `dotnet build` to check for errors

**Icon not showing:**
- The app icon is located in `Resources/appicon.ico`
- It will show in the taskbar and window title bar

---
Enjoy creating beautiful anime quote wallpapers! 🌸✨
