$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$solutionRoot = Split-Path -Parent $scriptDir
$guiProj = Join-Path $solutionRoot "AnimeQuoteWall.GUI\AnimeQuoteWall.GUI.csproj"
$outDir = Join-Path $solutionRoot "publish\compact"

Write-Host "Publishing compact Release (framework-dependent, single-file)..." -ForegroundColor Cyan

dotnet publish $guiProj `
    -c Release `
    -r win-x64 `
    -p:PublishSingleFile=true `
    -p:SelfContained=false `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=none `
    -o $outDir

Write-Host "Output: $outDir" -ForegroundColor Green
Get-ChildItem -Path $outDir | Select-Object Name,Length,LastWriteTime | Format-Table -AutoSize


