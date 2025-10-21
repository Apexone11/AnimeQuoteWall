# Anime Quote Wallpaper Manager Launcher
# PowerShell script to launch the GUI application

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Anime Quote Wallpaper Manager" -ForegroundColor Magenta
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$guiPath = Join-Path $scriptPath "AnimeQuoteWall.GUI"

if (Test-Path $guiPath) {
    Write-Host "Launching application..." -ForegroundColor Green
    Set-Location $guiPath
    dotnet run
} else {
    Write-Host "Error: GUI project not found at $guiPath" -ForegroundColor Red
    Write-Host "Press any key to exit..."
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
}