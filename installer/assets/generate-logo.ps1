# generate-logo.ps1
# Generates the original AnimeQuoteWall application logo and installer branding from a single
# GDI+ vector-style drawing. The mark is intentionally generic - an indigo rounded tile with a
# white quotation mark - so it contains NO third-party or character IP (replaces the prior
# Naruto-derived icon, which was a trademark/copyright risk for Store/Steam distribution).
#
# Outputs (overwrites in place):
#   AnimeQuoteWall.GUI/Resources/appicon.ico   (multi-resolution app + EXE icon)
#   AnimeQuoteWall.GUI/Resources/appicon.png   (256px, used in-app)
#   installer/assets/setup-icon.ico            (installer icon)
#   installer/assets/wizard-small.bmp          (55x58, 24-bit, Inno small wizard image)
#   installer/assets/wizard-side.bmp           (164x314, 24-bit, Inno large wizard image)
#
# Usage:  powershell -ExecutionPolicy Bypass -File installer/assets/generate-logo.ps1
#         (or pwsh -File ... on systems where System.Drawing loads under .NET)

$ErrorActionPreference = 'Stop'
try { Add-Type -AssemblyName System.Drawing } catch { Add-Type -AssemblyName System.Drawing.Common }

$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$guiRes = Join-Path $repo 'AnimeQuoteWall.GUI\Resources'
$assets = Join-Path $repo 'installer\assets'

# Brand colours (match Theme PrimaryColor gradient family).
$indigoLight = [System.Drawing.Color]::FromArgb(255, 0x81, 0x8C, 0xF8) # #818CF8
$indigoDark  = [System.Drawing.Color]::FromArgb(255, 0x4F, 0x46, 0xE5) # #4F46E5

# Superellipse ("squircle") path - a smoother, more premium tile shape than a plain rounded
# rectangle (the iOS-style continuous curve). n controls the corner fullness.
function New-SquirclePath([single]$x, [single]$y, [single]$w, [single]$h) {
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $cx = [double]($x + $w / 2)
    $cy = [double]($y + $h / 2)
    $ax = [double]($w / 2)
    $ay = [double]($h / 2)
    $n = 5.0
    $steps = 120
    $pts = New-Object 'System.Collections.Generic.List[System.Drawing.PointF]'
    for ($i = 0; $i -lt $steps; $i++) {
        $t = 2.0 * [Math]::PI * $i / $steps
        $ct = [Math]::Cos($t)
        $st = [Math]::Sin($t)
        $sx = [Math]::Sign($ct) * [Math]::Pow([Math]::Abs($ct), 2.0 / $n)
        $sy = [Math]::Sign($st) * [Math]::Pow([Math]::Abs($st), 2.0 / $n)
        $px = [single]($cx + $ax * $sx)
        $py = [single]($cy + $ay * $sy)
        $pts.Add((New-Object System.Drawing.PointF($px, $py)))
    }
    $path.AddPolygon($pts.ToArray())
    $path.CloseFigure()
    return $path
}

# Draws the logo (gradient squircle + soft sheen + elegant serif quotation mark with a drop
# shadow) onto a Graphics surface spanning the given square size, starting at (ox, oy).
function Draw-Logo([System.Drawing.Graphics]$g, [single]$ox, [single]$oy, [single]$size) {
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAlias

    $pad = [single]([Math]::Max(1, $size * 0.055))
    $tx = [single]($ox + $pad)
    $ty0 = [single]($oy + $pad)
    $tw = [single]($size - 2 * $pad)
    $th0 = [single]($size - 2 * $pad)
    $rect = New-Object System.Drawing.RectangleF($tx, $ty0, $tw, $th0)
    $tile = New-SquirclePath $tx $ty0 $tw $th0

    # Diagonal three-stop indigo -> violet gradient for depth.
    $cTop = [System.Drawing.Color]::FromArgb(255, 0x8E, 0x97, 0xFF)
    $cMid = [System.Drawing.Color]::FromArgb(255, 0x63, 0x66, 0xF1)
    $cBot = [System.Drawing.Color]::FromArgb(255, 0x43, 0x38, 0xCA)
    $grad = New-Object System.Drawing.Drawing2D.LinearGradientBrush($rect, $cTop, $cBot, 55)
    $blend = New-Object System.Drawing.Drawing2D.ColorBlend(3)
    $blend.Colors = @($cTop, $cMid, $cBot)
    $blend.Positions = @([single]0.0, [single]0.55, [single]1.0)
    $grad.InterpolationColors = $blend
    $g.FillPath($grad, $tile)
    $grad.Dispose()

    # Soft sheen highlight in the top-left, clipped to the tile.
    $g.SetClip($tile)
    $glow = New-Object System.Drawing.Drawing2D.GraphicsPath
    $gx = [single]($tx - $tw * 0.25)
    $gy = [single]($ty0 - $th0 * 0.35)
    $gw = [single]($tw * 1.15)
    $gh = [single]($th0 * 1.0)
    $glow.AddEllipse($gx, $gy, $gw, $gh)
    $pgb = New-Object System.Drawing.Drawing2D.PathGradientBrush($glow)
    $pgb.CenterColor = [System.Drawing.Color]::FromArgb(75, 255, 255, 255)
    $pgb.SurroundColors = @([System.Drawing.Color]::FromArgb(0, 255, 255, 255))
    $g.FillPath($pgb, $glow)
    $pgb.Dispose(); $glow.Dispose()
    $g.ResetClip()

    # Elegant curly opening quotation mark (serif glyph) with a soft drop shadow for depth.
    $fontSize = [single]($size * 0.74)
    $font = New-Object System.Drawing.Font('Georgia', $fontSize, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
    $sf = New-Object System.Drawing.StringFormat
    $sf.Alignment = [System.Drawing.StringAlignment]::Center
    $sf.LineAlignment = [System.Drawing.StringAlignment]::Center
    $glyph = [string][char]0x201C
    $qy = [single]($oy + $size * 0.12)
    $qh = [single]($size * 0.84)
    $shadowRect = New-Object System.Drawing.RectangleF([single]($ox + $size * 0.018), [single]($qy + $size * 0.03), [single]$size, $qh)
    $shadow = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(65, 17, 17, 46))
    $g.DrawString($glyph, $font, $shadow, $shadowRect, $sf)
    $mainRect = New-Object System.Drawing.RectangleF([single]$ox, $qy, [single]$size, $qh)
    $white = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 255, 255, 255))
    $g.DrawString($glyph, $font, $white, $mainRect, $sf)
    $shadow.Dispose(); $white.Dispose(); $font.Dispose(); $sf.Dispose()
    $tile.Dispose()
}

function New-LogoBitmap([int]$size) {
    $bmp = New-Object System.Drawing.Bitmap($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.Clear([System.Drawing.Color]::Transparent)
    Draw-Logo $g 0 0 $size
    $g.Dispose()
    return $bmp
}

function Save-Ico([int[]]$sizes, [string]$path) {
    $datas = @()
    foreach ($s in $sizes) {
        $b = New-LogoBitmap $s
        $ms = New-Object System.IO.MemoryStream
        $b.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
        $datas += , $ms.ToArray()
        $ms.Dispose(); $b.Dispose()
    }
    $fs = [System.IO.File]::Open($path, [System.IO.FileMode]::Create)
    $bw = New-Object System.IO.BinaryWriter($fs)
    $bw.Write([UInt16]0); $bw.Write([UInt16]1); $bw.Write([UInt16]$datas.Count)  # ICONDIR
    $offset = 6 + 16 * $datas.Count
    for ($i = 0; $i -lt $datas.Count; $i++) {
        $s = $sizes[$i]; $dim = if ($s -ge 256) { 0 } else { $s }
        $bw.Write([Byte]$dim); $bw.Write([Byte]$dim); $bw.Write([Byte]0); $bw.Write([Byte]0)
        $bw.Write([UInt16]1); $bw.Write([UInt16]32)
        $bw.Write([UInt32]$datas[$i].Length); $bw.Write([UInt32]$offset)
        $offset += $datas[$i].Length
    }
    foreach ($d in $datas) { $bw.Write($d) }
    $bw.Flush(); $bw.Close(); $fs.Close()
    Write-Host "Wrote $path"
}

function Save-Png([int]$size, [string]$path) {
    $b = New-LogoBitmap $size
    $b.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $b.Dispose()
    Write-Host "Wrote $path"
}

# 24-bit BMP for Inno Setup wizard images (Inno requires 24-bit BMP).
function Save-WizardBmp([int]$w, [int]$h, [bool]$gradientBg, [string]$path, [string]$title = '') {
    $bmp = New-Object System.Drawing.Bitmap($w, $h, [System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAlias
    if ($gradientBg) {
        $r = New-Object System.Drawing.Rectangle(0, 0, $w, $h)
        $br = New-Object System.Drawing.Drawing2D.LinearGradientBrush($r, $indigoLight, $indigoDark, 90)
        $g.FillRectangle($br, $r); $br.Dispose()
    }
    else {
        $g.Clear([System.Drawing.Color]::White)
    }
    $logoFactor = if ($title -ne '') { 0.58 } else { 0.78 }
    $logo = [single]([Math]::Min($w, $h) * $logoFactor)
    $ox = [single](($w - $logo) / 2)
    $oy = if ($gradientBg) { [single]($h * 0.10) } else { [single](($h - $logo) / 2) }
    Draw-Logo $g $ox $oy $logo

    # Optional wordmark beneath the logo (used on the tall installer side banner).
    if ($title -ne '') {
        $tfont = New-Object System.Drawing.Font('Segoe UI', [single]($w * 0.105), [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
        $tsf = New-Object System.Drawing.StringFormat
        $tsf.Alignment = [System.Drawing.StringAlignment]::Center
        $tbrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 255, 255, 255))
        $trow = [single]($oy + $logo + $h * 0.03)
        $trect = New-Object System.Drawing.RectangleF([single]0, $trow, [single]$w, [single]($h * 0.2))
        $g.DrawString($title, $tfont, $tbrush, $trect, $tsf)
        $tfont.Dispose(); $tsf.Dispose(); $tbrush.Dispose()
    }
    $g.Dispose()
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Bmp)
    $bmp.Dispose()
    Write-Host "Wrote $path"
}

$icoSizes = @(16, 24, 32, 48, 64, 128, 256)
Save-Ico $icoSizes (Join-Path $guiRes 'appicon.ico')
Save-Png 256 (Join-Path $guiRes 'appicon.png')
Save-Ico $icoSizes (Join-Path $assets 'setup-icon.ico')
Save-WizardBmp 55 58 $false (Join-Path $assets 'wizard-small.bmp')
Save-WizardBmp 164 314 $true (Join-Path $assets 'wizard-side.bmp') 'AnimeQuoteWall'

# --- Microsoft Store / MSIX tile assets (kept consistent with the app icon) ---
$storeAssets = Join-Path $repo 'installer\store-assets\Assets'
New-Item -ItemType Directory -Force -Path $storeAssets | Out-Null

function Save-SquareTile([int]$size, [string]$path) {
    $b = New-LogoBitmap $size
    $b.Save($path, [System.Drawing.Imaging.ImageFormat]::Png); $b.Dispose()
    Write-Host "Wrote $path"
}

# Non-square tile: logo centred on the indigo gradient.
function Save-WideTile([int]$w, [int]$h, [string]$path) {
    $bmp = New-Object System.Drawing.Bitmap($w, $h, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.Clear([System.Drawing.Color]::Transparent)
    $r = New-Object System.Drawing.Rectangle(0, 0, $w, $h)
    $br = New-Object System.Drawing.Drawing2D.LinearGradientBrush($r, $indigoLight, $indigoDark, 90)
    $g.FillRectangle($br, $r); $br.Dispose()
    $logo = [single]([Math]::Min($w, $h) * 0.82)
    $lx = [single](($w - $logo) / 2)
    $ly = [single](($h - $logo) / 2)
    Draw-Logo $g $lx $ly $logo
    $g.Dispose()
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png); $bmp.Dispose()
    Write-Host "Wrote $path"
}

Save-SquareTile 44 (Join-Path $storeAssets 'Square44x44Logo.png')
Save-SquareTile 88 (Join-Path $storeAssets 'Square44x44Logo.scale-200.png')
Save-SquareTile 256 (Join-Path $storeAssets 'Square44x44Logo.targetsize-256.png')
Save-SquareTile 48 (Join-Path $storeAssets 'Square44x44Logo.targetsize-48.png')
Save-SquareTile 32 (Join-Path $storeAssets 'Square44x44Logo.targetsize-32.png')
Save-SquareTile 24 (Join-Path $storeAssets 'Square44x44Logo.targetsize-24.png')
Save-SquareTile 16 (Join-Path $storeAssets 'Square44x44Logo.targetsize-16.png')
Save-SquareTile 150 (Join-Path $storeAssets 'Square150x150Logo.png')
Save-SquareTile 300 (Join-Path $storeAssets 'Square150x150Logo.scale-200.png')
Save-SquareTile 71 (Join-Path $storeAssets 'SmallTile.png')
Save-SquareTile 310 (Join-Path $storeAssets 'LargeTile.png')
Save-SquareTile 50 (Join-Path $storeAssets 'StoreLogo.png')
Save-WideTile 310 150 (Join-Path $storeAssets 'Wide310x150Logo.png')
Save-WideTile 620 300 (Join-Path $storeAssets 'SplashScreen.png')

Write-Host 'Logo generation complete.'
