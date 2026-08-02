# Generates wizard-side.bmp and wizard-small.bmp for the Inno Setup installer.
# Uses System.Drawing.Common from .NET 8 (windows-only) so it has no external
# dependency on ImageMagick. Run once before installer/build.ps1.
#
# The banners are intentionally restrained: indigo gradient background, app
# icon centered on the side banner, brand wordmark below it, and just the icon
# on the small banner. No emojis, no fancy filters - production-clean.

$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName 'System.Drawing'

$scriptDir   = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot    = Resolve-Path (Join-Path $scriptDir '..\..')
$appIconPath = Join-Path $repoRoot 'AnimeQuoteWall.GUI\Resources\appicon.png'
if (-not (Test-Path $appIconPath)) {
    throw "App icon not found at $appIconPath"
}

# Brand palette - matches PrimaryColor (#6366F1) and PrimaryDark (#4F46E5).
$brandLight = [System.Drawing.Color]::FromArgb(0x81, 0x8C, 0xF8)
$brandMid   = [System.Drawing.Color]::FromArgb(0x63, 0x66, 0xF1)
$brandDark  = [System.Drawing.Color]::FromArgb(0x4F, 0x46, 0xE5)
$ink        = [System.Drawing.Color]::FromArgb(0xFF, 0xFF, 0xFF)
$inkSubtle  = [System.Drawing.Color]::FromArgb(0xE0, 0xE7, 0xFF)

# Save as classic 24-bit BMP (Inno Setup expects the legacy BMP3 variant).
function Save-Bmp24 {
    param(
        [Parameter(Mandatory)] [System.Drawing.Bitmap] $Source,
        [Parameter(Mandatory)] [string] $Path
    )
    $rect = [System.Drawing.Rectangle]::new(0, 0, $Source.Width, $Source.Height)
    $bmp24 = New-Object System.Drawing.Bitmap $Source.Width, $Source.Height, ([System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
    try {
        $g = [System.Drawing.Graphics]::FromImage($bmp24)
        try {
            $g.Clear([System.Drawing.Color]::Black)
            $g.DrawImage($Source, $rect)
        } finally { $g.Dispose() }
        if (Test-Path $Path) { Remove-Item -LiteralPath $Path -Force }
        $bmp24.Save($Path, [System.Drawing.Imaging.ImageFormat]::Bmp)
    } finally { $bmp24.Dispose() }
}

# ---- wizard-side.bmp (164 x 314, portrait) ----
$sideW = 164
$sideH = 314
$side = New-Object System.Drawing.Bitmap $sideW, $sideH, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
try {
    $g = [System.Drawing.Graphics]::FromImage($side)
    try {
        $g.SmoothingMode     = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
        $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::ClearTypeGridFit

        # Diagonal gradient background.
        $rect = [System.Drawing.Rectangle]::new(0, 0, $sideW, $sideH)
        $brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
            $rect, $brandLight, $brandDark,
            [System.Drawing.Drawing2D.LinearGradientMode]::ForwardDiagonal)
        $g.FillRectangle($brush, $rect)
        $brush.Dispose()

        # Subtle vignette circle behind the icon.
        $vignette = New-Object System.Drawing.Drawing2D.GraphicsPath
        $vignette.AddEllipse(8, 30, 148, 148)
        $vignetteBrush = New-Object System.Drawing.Drawing2D.PathGradientBrush($vignette)
        $vignetteBrush.CenterColor = [System.Drawing.Color]::FromArgb(80, 255, 255, 255)
        $vignetteBrush.SurroundColors = @([System.Drawing.Color]::FromArgb(0, 255, 255, 255))
        $g.FillEllipse($vignetteBrush, 8, 30, 148, 148)
        $vignetteBrush.Dispose(); $vignette.Dispose()

        # App icon centered in upper third.
        $icon = [System.Drawing.Image]::FromFile($appIconPath)
        try {
            $iconSize = 96
            $iconX = [int](($sideW - $iconSize) / 2)
            $iconY = 56
            $g.DrawImage($icon, $iconX, $iconY, $iconSize, $iconSize)
        } finally { $icon.Dispose() }

        # Wordmark.
        $titleFont    = New-Object System.Drawing.Font('Segoe UI', 13, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
        $subtitleFont = New-Object System.Drawing.Font('Segoe UI', 10, [System.Drawing.FontStyle]::Regular, [System.Drawing.GraphicsUnit]::Pixel)
        $titleBrush    = New-Object System.Drawing.SolidBrush $ink
        $subtitleBrush = New-Object System.Drawing.SolidBrush $inkSubtle

        $titleFormat = New-Object System.Drawing.StringFormat
        $titleFormat.Alignment     = [System.Drawing.StringAlignment]::Center
        $titleFormat.LineAlignment = [System.Drawing.StringAlignment]::Center

        $g.DrawString('Anime Quote',         $titleFont,    $titleBrush,    [System.Drawing.RectangleF]::new(0, 170, $sideW, 22), $titleFormat)
        $g.DrawString('Wallpaper Manager',   $titleFont,    $titleBrush,    [System.Drawing.RectangleF]::new(0, 192, $sideW, 22), $titleFormat)
        $g.DrawString('Version 1.3.0',       $subtitleFont, $subtitleBrush, [System.Drawing.RectangleF]::new(0, 224, $sideW, 18), $titleFormat)

        # Decorative footer rule near the bottom (keep bottom third visually quiet).
        $rulePen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(64, 255, 255, 255)), 1
        $g.DrawLine($rulePen, 24, 288, $sideW - 24, 288)
        $rulePen.Dispose()
        $titleBrush.Dispose(); $subtitleBrush.Dispose()
        $titleFont.Dispose(); $subtitleFont.Dispose()
    } finally { $g.Dispose() }

    Save-Bmp24 -Source $side -Path (Join-Path $scriptDir 'wizard-side.bmp')
    Write-Host ("Wrote " + (Join-Path $scriptDir 'wizard-side.bmp'))
} finally { $side.Dispose() }

# ---- wizard-small.bmp (55 x 58) ----
$smallW = 55
$smallH = 58
$small = New-Object System.Drawing.Bitmap $smallW, $smallH, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
try {
    $g = [System.Drawing.Graphics]::FromImage($small)
    try {
        $g.SmoothingMode     = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
        $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic

        # Solid brand fill.
        $bg = New-Object System.Drawing.SolidBrush $brandMid
        $g.FillRectangle($bg, 0, 0, $smallW, $smallH)
        $bg.Dispose()

        # Centered app icon.
        $icon = [System.Drawing.Image]::FromFile($appIconPath)
        try {
            $iconSize = 40
            $iconX = [int](($smallW - $iconSize) / 2)
            $iconY = [int](($smallH - $iconSize) / 2)
            $g.DrawImage($icon, $iconX, $iconY, $iconSize, $iconSize)
        } finally { $icon.Dispose() }
    } finally { $g.Dispose() }

    Save-Bmp24 -Source $small -Path (Join-Path $scriptDir 'wizard-small.bmp')
    Write-Host ("Wrote " + (Join-Path $scriptDir 'wizard-small.bmp'))
} finally { $small.Dispose() }

Write-Host ''
Write-Host 'Banner assets generated successfully.'
