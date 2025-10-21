@echo off
REM Anime Quote Wallpaper Manager Launcher
REM This script launches the GUI application

echo Starting Anime Quote Wallpaper Manager...
echo.

cd /d "%~dp0AnimeQuoteWall.GUI"
dotnet run

pause