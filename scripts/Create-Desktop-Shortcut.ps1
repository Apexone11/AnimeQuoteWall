# Create Desktop Shortcut for Anime Quote Wallpaper Manager
$WshShell = New-Object -ComObject WScript.Shell

# Get desktop path
$DesktopPath = [Environment]::GetFolderPath("Desktop")

# Create shortcut
$ShortcutPath = Join-Path $DesktopPath "Anime Quote Wallpaper.lnk"
$Shortcut = $WshShell.CreateShortcut($ShortcutPath)

# Set target to the batch file
$ScriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$BatchPath = Join-Path $ScriptPath "Launch-AnimeQuoteWall.bat"

$Shortcut.TargetPath = $BatchPath
$Shortcut.WorkingDirectory = $ScriptPath
$Shortcut.Description = "Launch Anime Quote Wallpaper Manager"

# Set icon if available
$IconPath = Join-Path $ScriptPath "AnimeQuoteWall.GUI\Resources\appicon.ico"
if (Test-Path $IconPath) {
    $Shortcut.IconLocation = $IconPath
}

$Shortcut.Save()

Write-Host "✅ Desktop shortcut created successfully!" -ForegroundColor Green
Write-Host "   Location: $ShortcutPath" -ForegroundColor Cyan
Write-Host ""
Write-Host "You can now launch the app from your desktop!" -ForegroundColor Yellow
