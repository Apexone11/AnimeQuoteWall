<#
.SYNOPSIS
    Builds the AnimeQuoteWall Inno Setup installer end-to-end.

.DESCRIPTION
    Publishes the WPF project for win-x64 (framework-dependent, ReadyToRun),
    locates Inno Setup 6 (iscc.exe), compiles installer\AnimeQuoteWall.iss,
    and prints the resulting setup.exe path, size, and SHA-256.

    Pass -Sign to authenticode-sign the produced setup.exe using whatever
    code-signing certificate is already provisioned on this machine.

.PARAMETER Sign
    When supplied, runs signtool against the produced setup.exe.

.PARAMETER Configuration
    Build configuration passed to `dotnet publish`. Defaults to Release.

.EXAMPLE
    .\installer\build.ps1

.EXAMPLE
    .\installer\build.ps1 -Sign
#>

[CmdletBinding()]
param(
    [switch]$Sign,
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Write-Step {
    param([string]$Message)
    Write-Host ("[build] {0}" -f $Message)
}

function Fail {
    param([string]$Message, [int]$Code = 1)
    Write-Host ("[build][error] {0}" -f $Message) -ErrorAction SilentlyContinue
    [Console]::Error.WriteLine("[build][error] $Message")
    exit $Code
}

# ---------------------------------------------------------------------------
# 0. Resolve repo root relative to this script so the script works regardless
#    of the current working directory when invoked.
# ---------------------------------------------------------------------------
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot  = Resolve-Path (Join-Path $ScriptDir '..')
Write-Step "Repository root: $RepoRoot"

$Project   = Join-Path $RepoRoot 'AnimeQuoteWall.GUI\AnimeQuoteWall.GUI.csproj'
$PublishOut = Join-Path $RepoRoot 'publish\win-x64'
$IssScript = Join-Path $ScriptDir 'AnimeQuoteWall.iss'
$DistDir   = Join-Path $ScriptDir 'dist'

if (-not (Test-Path -LiteralPath $Project)) {
    Fail "Project not found: $Project"
}
if (-not (Test-Path -LiteralPath $IssScript)) {
    Fail "Inno Setup script not found: $IssScript"
}

# ---------------------------------------------------------------------------
# 1. dotnet publish - framework-dependent, ReadyToRun, no single-file.
#    We rely on the user installing the .NET 8 Desktop Runtime separately
#    so the installer payload stays small (the .iss [Code] section enforces
#    that prerequisite at install time).
# ---------------------------------------------------------------------------
Write-Step "Verifying dotnet SDK..."
$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnet) {
    Fail "dotnet CLI not found on PATH. Install the .NET 8 SDK from https://dotnet.microsoft.com/download/dotnet/8.0"
}

Write-Step "Publishing $Project ($Configuration, win-x64)..."
& dotnet publish $Project `
    -c $Configuration `
    -r win-x64 `
    --self-contained false `
    -p:PublishSingleFile=false `
    -p:PublishReadyToRun=true `
    -o $PublishOut
if ($LASTEXITCODE -ne 0) {
    Fail "dotnet publish failed with exit code $LASTEXITCODE" $LASTEXITCODE
}

# Sanity check the published exe is actually there.
$PublishedExe = Join-Path $PublishOut 'AnimeQuoteWall.exe'
if (-not (Test-Path -LiteralPath $PublishedExe)) {
    Fail "Expected published binary not found: $PublishedExe"
}
Write-Step "Publish output: $PublishOut"

# ---------------------------------------------------------------------------
# 2. Locate iscc.exe (Inno Setup 6 compiler). Check standard install paths
#    first, then fall back to PATH.
# ---------------------------------------------------------------------------
$IsccCandidates = @(
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles}\Inno Setup 6\ISCC.exe",
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
)
$Iscc = $null
foreach ($cand in $IsccCandidates) {
    if ($cand -and (Test-Path -LiteralPath $cand)) {
        $Iscc = $cand
        break
    }
}
if (-not $Iscc) {
    $cmd = Get-Command iscc.exe -ErrorAction SilentlyContinue
    if ($cmd) { $Iscc = $cmd.Source }
}
if (-not $Iscc) {
    Fail "Inno Setup 6 (iscc.exe) was not found. Download and install Inno Setup 6.3 or later from https://jrsoftware.org/isdl.php and re-run this script."
}
Write-Step "Inno Setup compiler: $Iscc"

# Ensure the dist directory exists before compilation so the SHA report
# step does not race the compiler creating it.
if (-not (Test-Path -LiteralPath $DistDir)) {
    New-Item -ItemType Directory -Path $DistDir -Force | Out-Null
}

# ---------------------------------------------------------------------------
# 3. Compile the installer.
# ---------------------------------------------------------------------------
Write-Step "Compiling $IssScript ..."
& $Iscc $IssScript
if ($LASTEXITCODE -ne 0) {
    Fail "Inno Setup compilation failed with exit code $LASTEXITCODE" $LASTEXITCODE
}

# ---------------------------------------------------------------------------
# 4. Locate the produced setup.exe and report its metadata.
# ---------------------------------------------------------------------------
$Produced = Get-ChildItem -LiteralPath $DistDir -Filter 'AnimeQuoteWall-Setup-*.exe' |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1
if (-not $Produced) {
    Fail "Inno Setup completed but no setup.exe was found in $DistDir"
}

$SizeMB = [math]::Round($Produced.Length / 1MB, 2)
$Hash   = (Get-FileHash -LiteralPath $Produced.FullName -Algorithm SHA256).Hash

Write-Step "----------------------------------------------"
Write-Step "Installer built successfully."
Write-Step ("Path   : {0}" -f $Produced.FullName)
Write-Step ("Size   : {0} MB ({1} bytes)" -f $SizeMB, $Produced.Length)
Write-Step ("SHA256 : {0}" -f $Hash)
Write-Step "----------------------------------------------"

# ---------------------------------------------------------------------------
# 5. Optional Authenticode signing.
# ---------------------------------------------------------------------------
if ($Sign) {
    $SignTool = Get-Command signtool.exe -ErrorAction SilentlyContinue
    if (-not $SignTool) {
        Fail "Sign requested but signtool.exe is not on PATH. Install the Windows SDK or add signtool to PATH."
    }
    Write-Step "Signing $($Produced.Name) ..."
    & $SignTool.Source sign /tr 'http://timestamp.digicert.com' /td sha256 /fd sha256 /a $Produced.FullName
    if ($LASTEXITCODE -ne 0) {
        Fail "signtool failed with exit code $LASTEXITCODE" $LASTEXITCODE
    }
    $Hash = (Get-FileHash -LiteralPath $Produced.FullName -Algorithm SHA256).Hash
    Write-Step ("Signed. New SHA256: {0}" -f $Hash)
}

exit 0
