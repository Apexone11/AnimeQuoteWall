# PowerShell script to set wallpaper as a backup method
param(
    [string]$ImagePath
)

Add-Type @"
using System;
using System.Runtime.InteropServices;
public class Wallpaper {
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);
}
"@

$SPI_SETDESKWALLPAPER = 0x0014
$SPIF_UPDATEINIFILE = 0x01
$SPIF_SENDWININICHANGE = 0x02

if (Test-Path $ImagePath) {
    Write-Host "Setting wallpaper: $ImagePath" -ForegroundColor Green
    [Wallpaper]::SystemParametersInfo($SPI_SETDESKWALLPAPER, 0, $ImagePath, $SPIF_UPDATEINIFILE -bor $SPIF_SENDWININICHANGE)
    Write-Host "Wallpaper set successfully!" -ForegroundColor Green
} else {
    Write-Host "Error: Image file not found at $ImagePath" -ForegroundColor Red
}
