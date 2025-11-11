$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$solutionRoot = Split-Path -Parent $scriptDir
$guiProj = Join-Path $solutionRoot "AnimeQuoteWall.GUI\AnimeQuoteWall.GUI.csproj"
$outDir = Join-Path $solutionRoot "publish\\trimmed"

Write-Host "Publishing trimmed Release (framework-dependent, single-file)..." -ForegroundColor Cyan

# Note: Trimming WPF apps can be risky due to reflection.
# Using conservative options to reduce size while keeping stability.
dotnet publish $guiProj `
    -c Release `
    -r win-x64 `
    -p:PublishSingleFile=true `
    -p:SelfContained=false `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:PublishTrimmed=true `
    -p:TrimMode=partial `
    -p:DebugType=none `
    -o $outDir

Write-Host "Output: $outDir" -ForegroundColor Green
Get-ChildItem -Path $outDir | Select-Object Name,Length,LastWriteTime | Format-Table -AutoSize


