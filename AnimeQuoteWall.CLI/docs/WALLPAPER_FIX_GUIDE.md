# Wallpaper Display Fix - Troubleshooting Guide

## ✅ Fixes Applied

### 1. **Changed Storage Location**
- **Old Path:** Example: `%USERPROFILE%\OneDrive\Pictures\AnimeQuotes\`
- **New Path:** `%LOCALAPPDATA%\AnimeQuotes\`
- **Reason:** OneDrive-synced folders can interfere with Windows wallpaper API

### 2. **Improved API Call**
- Added `CharSet = CharSet.Auto` to the P/Invoke declaration
- Added better error handling with error codes
- Added file existence checks

### 3. **Created Backup PowerShell Script**
- `SetWallpaper.ps1` can manually set the wallpaper if needed
- Alternative method that works more reliably on some systems

## 🔍 How to Verify Wallpaper is Working

### Method 1: Check Windows Settings
1. Right-click on Desktop → **Personalize**
2. Go to **Background** settings
3. You should see your anime quote wallpaper in the preview

### Method 2: Manual Test with PowerShell
Run this command:
```powershell
powershell -ExecutionPolicy Bypass -File "SetWallpaper.ps1" -ImagePath "%LOCALAPPDATA%\AnimeQuotes\current.png"
```

### Method 3: Run the Application
```bash
dotnet run
```
The wallpaper should change immediately after running.

## 🛠️ If Wallpaper Still Doesn't Show

### Issue 1: Windows Slideshow Mode
If Windows is set to slideshow mode, it might override your wallpaper.

**Fix:**
1. Right-click Desktop → **Personalize**
2. Go to **Background**
3. Change from "Slideshow" to "Picture"
4. Run `dotnet run` again

### Issue 2: Group Policy Restrictions
Some systems (especially work computers) have group policies that prevent wallpaper changes.

**Check:**
1. Press `Win + R`
2. Type `gpedit.msc` and press Enter
3. Navigate to: User Configuration → Administrative Templates → Control Panel → Personalization
4. Ensure "Prevent changing desktop background" is **Not Configured** or **Disabled**

### Issue 3: Transparency Effects or Tablet Mode
Some Windows display modes can affect wallpaper display.

**Fix:**
1. Disable Tablet Mode: Settings → System → Tablet
2. Check display settings: Settings → System → Display
3. Restart Windows Explorer:
   - Press `Ctrl + Shift + Esc` (Task Manager)
   - Find "Windows Explorer"
   - Right-click → Restart

### Issue 4: Desktop Background Service Not Running
**Fix:**
```bash
# Run in PowerShell as Administrator
Restart-Service -Name "Desktop Window Manager Session Manager" -Force
```

## 📂 File Locations (Examples)

- **Application (source):** `AnimeQuoteWall\` (repository root)
- **Wallpaper Images:** `%LOCALAPPDATA%\AnimeQuotes\` 
- **Current Wallpaper:** `%LOCALAPPDATA%\AnimeQuotes\current.png`
- **Background Images:** `%LOCALAPPDATA%\AnimeQuotes\backgrounds\`
- **Quotes Database:** `%LOCALAPPDATA%\AnimeQuotes\quotes.json`

## 🎯 Quick Test Commands

### View Current Wallpaper Path
```bash
dir "%LOCALAPPDATA%\AnimeQuotes\current.png"
```

### Open Wallpaper Image Manually
```bash
start "" "%LOCALAPPDATA%\AnimeQuotes\current.png"
```

### Set Wallpaper Manually (PowerShell)
```powershell
powershell -ExecutionPolicy Bypass -File "SetWallpaper.ps1" -ImagePath "%LOCALAPPDATA%\AnimeQuotes\current.png"
```

### Generate New Wallpaper
```bash
cd AnimeQuoteWall
dotnet run
```

## 🎨 Adding More Background Images

1. Copy your anime images to: `%LOCALAPPDATA%\AnimeQuotes\backgrounds\`
2. Supported formats: `.jpg`, `.jpeg`, `.png`, `.bmp`
3. Recommended resolution: 1920x1080 or higher
4. Run `dotnet run` to use a random background

## 💡 Tips

- **Instant Refresh:** Right-click desktop and press F5 after running the app
- **Verify the Image:** Open `current.png` manually to see if the quote is rendered correctly
- **Check Console Output:** The app shows which quote and background it's using
- **Test Different Times:** Run multiple times to see different quotes

## 🐛 Still Having Issues?

If none of the above works, try this comprehensive reset:

```bash
# 1. Delete cache
del "%LOCALAPPDATA%\AnimeQuotes\current.png"

# 2. Run application
cd AnimeQuoteWall
dotnet run

# 3. Force wallpaper refresh with PowerShell
powershell -ExecutionPolicy Bypass -File "SetWallpaper.ps1" -ImagePath "%LOCALAPPDATA%\AnimeQuotes\current.png"

# 4. Restart Explorer
taskkill /F /IM explorer.exe && start explorer.exe
```

---

**Note:** After any fix, you may need to wait a few seconds or refresh the desktop (F5) to see the changes.