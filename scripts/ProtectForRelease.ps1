# PowerShell script to prepare build for public release with protection
# Run this script before building the release version

param(
    [switch]$EnableObfuscation = $false,
    [switch]$RemoveDebugSymbols = $true,
    [switch]$SignAssembly = $false
)

Write-Host "=== AnimeQuoteWall Release Protection Setup ===" -ForegroundColor Cyan
Write-Host ""

# Check if ConfuserEx is available (optional obfuscation tool)
$confuserPath = ".\tools\Confuser.CLI.exe"
$hasConfuser = Test-Path $confuserPath

if ($EnableObfuscation -and -not $hasConfuser)
{
    Write-Host "Warning: ConfuserEx not found at $confuserPath" -ForegroundColor Yellow
    Write-Host "Obfuscation will be skipped. Download from: https://github.com/mkaring/ConfuserEx" -ForegroundColor Yellow
    Write-Host ""
}

# Update project files for release build
Write-Host "Configuring projects for protected release build..." -ForegroundColor Green

# Core project
$coreProj = "AnimeQuoteWall.Core\AnimeQuoteWall.Core.csproj"
if (Test-Path $coreProj)
{
    $content = Get-Content $coreProj -Raw
    $content = $content -replace '<DebugType>pdbonly</DebugType>', '<DebugType>none</DebugType>'
    $content = $content -replace '<DebugSymbols>true</DebugSymbols>', '<DebugSymbols>false</DebugSymbols>'
    Set-Content $coreProj $content
    Write-Host "  ✓ Updated $coreProj" -ForegroundColor Gray
}

# GUI project
$guiProj = "AnimeQuoteWall.GUI\AnimeQuoteWall.GUI.csproj"
if (Test-Path $guiProj)
{
    $content = Get-Content $guiProj -Raw
    $content = $content -replace '<DebugType>pdbonly</DebugType>', '<DebugType>none</DebugType>'
    $content = $content -replace '<DebugSymbols>true</DebugSymbols>', '<DebugSymbols>false</DebugSymbols>'
    Set-Content $guiProj $content
    Write-Host "  ✓ Updated $guiProj" -ForegroundColor Gray
}

Write-Host ""
Write-Host "Build Configuration:" -ForegroundColor Cyan
Write-Host "  - Debug Symbols: $($RemoveDebugSymbols ? 'Removed' : 'Kept')" -ForegroundColor Gray
Write-Host "  - Obfuscation: $($EnableObfuscation ? 'Enabled' : 'Disabled')" -ForegroundColor Gray
Write-Host "  - Code Signing: $($SignAssembly ? 'Enabled' : 'Disabled')" -ForegroundColor Gray
Write-Host ""

# Build release version
Write-Host "Building Release configuration..." -ForegroundColor Green
dotnet build -c Release --no-incremental

if ($LASTEXITCODE -eq 0)
{
    Write-Host ""
    Write-Host "✓ Build successful!" -ForegroundColor Green
    
    if ($EnableObfuscation -and $hasConfuser)
    {
        Write-Host ""
        Write-Host "Applying obfuscation..." -ForegroundColor Green
        # Add ConfuserEx obfuscation commands here
        Write-Host "  Note: Configure ConfuserEx project file for full obfuscation" -ForegroundColor Yellow
    }
    
    Write-Host ""
    Write-Host "=== Protection Setup Complete ===" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Next steps for public release:" -ForegroundColor Yellow
    Write-Host "  1. Test the protected build thoroughly" -ForegroundColor White
    Write-Host "  2. Verify all features work correctly" -ForegroundColor White
    Write-Host "  3. Code sign assemblies (if certificate available)" -ForegroundColor White
    Write-Host "  4. Package for distribution" -ForegroundColor White
}
else
{
    Write-Host ""
    Write-Host "✗ Build failed! Check errors above." -ForegroundColor Red
    exit 1
}

