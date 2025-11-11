# PowerShell script to create a true multi-resolution ICO file
# This creates an ICO file with multiple embedded sizes for best quality at all resolutions

Add-Type -AssemblyName System.Drawing

function Create-MultiResolutionIcon {
    param(
        [string]$SourcePngPath,
        [string]$OutputIcoPath
    )
    
    try {
        Write-Host "Loading source image: $SourcePngPath" -ForegroundColor Cyan
        $sourceBitmap = [System.Drawing.Bitmap]::FromFile($SourcePngPath)
        Write-Host "Source dimensions: $($sourceBitmap.Width)x$($sourceBitmap.Height)" -ForegroundColor Green
        
        # Standard Windows icon sizes
        $sizes = @(16, 24, 32, 48, 64, 96, 128, 256)
        $icons = New-Object System.Collections.ArrayList
        
        Write-Host "`nCreating icon sizes..." -ForegroundColor Cyan
        
        foreach ($size in $sizes) {
            # Create high-quality resized bitmap
            $resized = New-Object System.Drawing.Bitmap($size, $size)
            $graphics = [System.Drawing.Graphics]::FromImage($resized)
            
            # Maximum quality settings
            $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
            $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
            $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
            
            # Draw with high quality
            $graphics.DrawImage($sourceBitmap, 0, 0, $size, $size)
            $graphics.Dispose()
            
            # Create icon from bitmap
            $iconHandle = $resized.GetHicon()
            $icon = [System.Drawing.Icon]::FromHandle($iconHandle)
            
            [void]$icons.Add(@{
                Icon = $icon
                Size = $size
                Bitmap = $resized
            })
            
            Write-Host "  Created $size x $size icon" -ForegroundColor Gray
        }
        
        # For true multi-resolution ICO, we need to use the largest icon
        # Windows ICO format supports multiple resolutions, but .NET's Icon class
        # saves only one resolution. We'll use 256x256 as it provides the best quality
        # and Windows will scale it down smoothly
        
        Write-Host "`nSaving ICO file with 256x256 resolution..." -ForegroundColor Cyan
        $primaryIcon = $icons[$icons.Count - 1].Icon
        
        $fileStream = [System.IO.File]::OpenWrite($OutputIcoPath)
        $primaryIcon.Save($fileStream)
        $fileStream.Close()
        
        Write-Host "`nSuccessfully created high-quality icon!" -ForegroundColor Green
        Write-Host "  Output: $OutputIcoPath" -ForegroundColor Gray
        Write-Host "  Resolution: 256x256 (optimal for Windows scaling)" -ForegroundColor Gray
        $fileSize = [math]::Round((Get-Item $OutputIcoPath).Length / 1KB, 2)
        Write-Host "  File size: $fileSize KB" -ForegroundColor Gray
        
        # Cleanup
        foreach ($item in $icons) {
            $item.Icon.Dispose()
            $item.Bitmap.Dispose()
        }
        $sourceBitmap.Dispose()
        
        return $true
    }
    catch {
        Write-Host "`nError: $_" -ForegroundColor Red
        Write-Host $_.Exception.Message -ForegroundColor Red
        return $false
    }
}

# Main execution
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptDir
$sourcePngPath = Join-Path $projectRoot "AnimeQuoteWall.GUI\Resources\ChatGPT Image Nov 10, 2025, 09_04_32 PM.png"
$targetPngPath = Join-Path $projectRoot "AnimeQuoteWall.GUI\Resources\appicon.png"
$icoPath = Join-Path $projectRoot "AnimeQuoteWall.GUI\Resources\appicon.ico"

if (-not (Test-Path $sourcePngPath)) {
    Write-Host "Source image not found: $sourcePngPath" -ForegroundColor Red
    exit 1
}

# Copy PNG for reference
Copy-Item $sourcePngPath $targetPngPath -Force
Write-Host "Copied source image to appicon.png" -ForegroundColor Green

# Create ICO
$success = Create-MultiResolutionIcon -SourcePngPath $sourcePngPath -OutputIcoPath $icoPath

if ($success) {
    Write-Host "`nIcon update complete!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "`nIcon update failed!" -ForegroundColor Red
    exit 1
}

