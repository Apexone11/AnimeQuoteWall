# PowerShell script to convert PNG to ICO
Add-Type -AssemblyName System.Drawing

# Get paths relative to script location
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptDir
$pngPath = Join-Path $projectRoot "AnimeQuoteWall.GUI\Resources\appicon.png"
$icoPath = Join-Path $projectRoot "AnimeQuoteWall.GUI\Resources\appicon.ico"

try {
    # Load the PNG image
    $bitmap = [System.Drawing.Bitmap]::FromFile($pngPath)
    
    # Create icon from bitmap
    $icon = [System.Drawing.Icon]::FromHandle($bitmap.GetHicon())
    
    # Save as ICO file
    $fileStream = [System.IO.File]::OpenWrite($icoPath)
    $icon.Save($fileStream)
    $fileStream.Close()
    
    Write-Host "Successfully converted $pngPath to $icoPath" -ForegroundColor Green
    
    # Cleanup
    $icon.Dispose()
    $bitmap.Dispose()
}
catch {
    Write-Host "Error converting icon: $_" -ForegroundColor Red
}
