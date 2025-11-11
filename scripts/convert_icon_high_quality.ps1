# PowerShell script to convert PNG to high-quality multi-resolution ICO
Add-Type -AssemblyName System.Drawing

# Get paths relative to script location
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptDir
$sourcePngPath = Join-Path $projectRoot "AnimeQuoteWall.GUI\Resources\ChatGPT Image Nov 10, 2025, 09_04_32 PM.png"
$targetPngPath = Join-Path $projectRoot "AnimeQuoteWall.GUI\Resources\appicon.png"
$icoPath = Join-Path $projectRoot "AnimeQuoteWall.GUI\Resources\appicon.ico"

try {
    Write-Host "Loading source image..." -ForegroundColor Cyan
    
    # Load the source PNG image
    $sourceBitmap = [System.Drawing.Bitmap]::FromFile($sourcePngPath)
    Write-Host "Source image: $($sourceBitmap.Width)x$($sourceBitmap.Height)" -ForegroundColor Green
    
    # Copy to appicon.png (for reference)
    $sourceBitmap.Save($targetPngPath, [System.Drawing.Imaging.ImageFormat]::Png)
    Write-Host "Copied to appicon.png" -ForegroundColor Green
    
    # Create a list of icon sizes for high quality
    $iconSizes = @(16, 32, 48, 64, 128, 256)
    $iconBitmaps = New-Object System.Collections.ArrayList
    
    Write-Host "Creating multi-resolution icon..." -ForegroundColor Cyan
    
    foreach ($size in $iconSizes) {
        # Create high-quality resized bitmap
        $resizedBitmap = New-Object System.Drawing.Bitmap($size, $size)
        $graphics = [System.Drawing.Graphics]::FromImage($resizedBitmap)
        
        # Use high-quality rendering settings
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
        $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
        
        # Draw the resized image
        $graphics.DrawImage($sourceBitmap, 0, 0, $size, $size)
        $graphics.Dispose()
        
        # Convert to icon handle
        $iconHandle = $resizedBitmap.GetHicon()
        $icon = [System.Drawing.Icon]::FromHandle($iconHandle)
        
        [void]$iconBitmaps.Add($icon)
        Write-Host "  Created $size x $size icon" -ForegroundColor Gray
    }
    
    # Create multi-resolution ICO file
    # Note: .NET doesn't have built-in multi-resolution ICO support,
    # so we'll use the largest size (256x256) which Windows will scale down
    # For true multi-resolution ICO, we'd need a third-party library
    
    # Use the 256x256 icon as the primary icon (best quality)
    $primaryIcon = $iconBitmaps[$iconBitmaps.Count - 1]
    
    # Save ICO file
    $fileStream = [System.IO.File]::OpenWrite($icoPath)
    $primaryIcon.Save($fileStream)
    $fileStream.Close()
    
    Write-Host "`nSuccessfully created high-quality icon!" -ForegroundColor Green
    Write-Host "  Source: $sourcePngPath" -ForegroundColor Gray
    Write-Host "  Output: $icoPath" -ForegroundColor Gray
    Write-Host "  Size: 256x256 (Windows will scale as needed)" -ForegroundColor Gray
    
    # Cleanup
    foreach ($icon in $iconBitmaps) {
        $icon.Dispose()
    }
    $sourceBitmap.Dispose()
    
    Write-Host "`nIcon conversion complete!" -ForegroundColor Green
}
catch {
    Write-Host "Error converting icon: $_" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

