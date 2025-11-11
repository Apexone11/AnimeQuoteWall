param(
    [switch]$All
)

Write-Host "Cleaning build artifacts..." -ForegroundColor Cyan

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$solutionRoot = Split-Path -Parent $root

Get-ChildItem -Path $solutionRoot -Recurse -Directory -Force -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -in @("bin","obj") } |
    ForEach-Object {
        try {
            Write-Host "Removing $($_.FullName)" -ForegroundColor Gray
            Remove-Item -Recurse -Force -LiteralPath $_.FullName
        } catch {
            Write-Host "Failed to remove: $($_.FullName)" -ForegroundColor Yellow
        }
    }

if ($All) {
    # Optional: clear thumbnail cache
    $thumbs = Join-Path $solutionRoot "AnimeQuoteWall.CoreData\thumbnails"
    if (Test-Path $thumbs) {
        Write-Host "Clearing thumbnail cache: $thumbs" -ForegroundColor Gray
        Remove-Item -Recurse -Force -LiteralPath $thumbs
    }
}

Write-Host "Cleanup complete." -ForegroundColor Green


