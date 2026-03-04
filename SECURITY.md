# Security Policy

## 🔒 Privacy & Security

AnimeQuoteWall is designed to be completely offline and privacy-first.

### What Makes It Safe

- ✅ **100% Offline** - No network access or internet connection
- ✅ **No Tracking** - Zero telemetry, analytics, or data collection
- ✅ **No Credentials** - No passwords, API keys, or tokens stored
- ✅ **Local Storage Only** - All files stay on your computer
- ✅ **Multi-User Safe** - Each Windows user gets their own folder

### Where Your Files Are Stored

```
%LOCALAPPDATA%\AnimeQuotes\
├── backgrounds/      # Your images
├── playlists/       # Playlist configs
├── quotes.json      # Your quotes
├── current.png      # Generated wallpaper
└── settings.json    # App settings
```

### Security Features

- **Path Validation** - Prevents malicious file paths
- **System Protection** - Can't write to Windows system folders
- **Safe Defaults** - Uses standard Windows user folders
- **Input Validation** - All user inputs are checked
- **Error Handling** - Graceful error messages

## 🐛 Reporting Security Issues

Found a security problem? Please report it responsibly:

1. **Don't open a public issue**
2. Email the maintainers or create a private GitHub Security Advisory
3. Include:
   - What the issue is
   - How to reproduce it
   - What versions are affected

For low-risk issues, you can open a regular issue with `[SECURITY]` in the title.

## 📦 Dependencies

We use these trusted libraries:
- `System.Drawing.Common` (image processing)
- `System.Text.Json` (settings)
- `Magick.NET-Q8-AnyCPU` (GIF export)
- `SixLabors.ImageSharp` (image editing)

All from official NuGet sources and regularly updated.

## ✅ For Users

- ✅ Safe to download and run
- ✅ No admin rights needed
- ✅ Files only in your user folder
- ✅ No system files touched
- ✅ Easy to uninstall (just delete the folder)

## 🛡️ Best Practices

1. Download from official GitHub releases only
2. Keep the app updated
3. Review custom paths before setting them
4. Backup your quotes and backgrounds regularly

---

**Version**: 1.3.1  
**Last Updated**: 2025-01-27  
**Status**: Safe & Secure | Open Source
