# AnimeQuoteWall Installer

This folder contains the Inno Setup project that produces the official
`AnimeQuoteWall-Setup-<version>.exe` distribution.

## Prerequisites

| Tool | Version | Where |
|------|---------|-------|
| Windows | 10 1809 (build 17763) or newer, x64 | host machine |
| .NET SDK | 8.0 or newer | <https://dotnet.microsoft.com/download/dotnet/8.0> |
| Inno Setup | 6.3 or newer | <https://jrsoftware.org/isdl.php> |
| PowerShell | 5.1 or 7+ | already on Windows |
| signtool (optional) | Windows SDK | only required when using `-Sign` |
| Code-signing certificate (optional) | any Authenticode cert installed in the user/machine store | only required when using `-Sign` |

Inno Setup must be installed to one of the default locations so the build
script can find `ISCC.exe` automatically:

- `C:\Program Files (x86)\Inno Setup 6\ISCC.exe`
- `C:\Program Files\Inno Setup 6\ISCC.exe`

If you installed Inno Setup elsewhere, add the folder containing `ISCC.exe`
to your `PATH` and the script will pick it up.

## Building the installer

From the **repository root**:

```powershell
.\installer\build.ps1
```

What the script does:

1. Resolves paths relative to itself so it works from any working directory.
2. Runs `dotnet publish` of `AnimeQuoteWall.GUI` for `win-x64`,
   framework-dependent, with ReadyToRun, into `publish\win-x64\`.
3. Locates `ISCC.exe` (Inno Setup 6 compiler).
4. Compiles `installer\AnimeQuoteWall.iss`.
5. Reports the resulting setup path, file size, and SHA-256 hash.

The produced installer is written to:

```
installer\dist\AnimeQuoteWall-Setup-<version>.exe
```

## Signing the installer

If you have a code-signing certificate provisioned (in the current user or
machine certificate store), pass `-Sign`:

```powershell
.\installer\build.ps1 -Sign
```

This calls:

```
signtool sign /tr http://timestamp.digicert.com /td sha256 /fd sha256 /a <setup.exe>
```

`/a` lets `signtool` auto-select the best certificate from the store. After
signing, the script re-prints the SHA-256 (which will change because the
PE has been modified).

## Output layout

```
installer/
    AnimeQuoteWall.iss            Inno Setup script
    build.ps1                     Build entry point
    README.md                     This document
    .gitignore                    Excludes dist/ and BMP placeholders
    assets/
        LICENSE.rtf               EULA shown on wizard page 2
        README-assets.md          Documentation for missing branding assets
        setup-icon.ico            (you supply)
        wizard-side.bmp           (you supply)
        wizard-small.bmp          (you supply)
    dist/
        AnimeQuoteWall-Setup-<version>.exe   (produced by build.ps1)
```

## Adding new files to the installer

The `[Files]` section in `AnimeQuoteWall.iss` ships the entire
`publish\win-x64\` directory recursively, so anything that lands in the
publish output (via the .csproj, e.g. `Content` items with
`CopyToPublishDirectory=PreserveNewest`) is automatically picked up. You
should rarely need to edit the `[Files]` section directly.

If you need to ship something that is **not** part of the publish output,
add an explicit line:

```
Source: "..\path\to\extra.dll"; DestDir: "{app}"; Flags: ignoreversion
```

Paths in `Source:` are resolved relative to the `.iss` file itself.

## Wizard branding assets - you MUST supply these

The repository does **not** ship the BMP/ICO branding assets. You (or a
designer) must drop the following three files into `installer\assets\`
before building:

| File | Format | Dimensions | Purpose |
|------|--------|------------|---------|
| `wizard-side.bmp` | 24-bit BMP | 164 x 314 px | Left-hand banner on Welcome and Finish pages |
| `wizard-small.bmp` | 24-bit BMP | 55 x 58 px | Small logo in the upper-right of inner wizard pages |
| `setup-icon.ico` | Multi-resolution ICO | include at minimum 16, 32, 48, 256 | Setup.exe shell icon and Apps & features entry |

Reuse the existing app icon by copying
`AnimeQuoteWall.GUI\Resources\appicon.ico` to
`installer\assets\setup-icon.ico` if you do not have a dedicated installer
icon.

See `installer\assets\README-assets.md` for the full design brief.

## Versioning

The installer version is hard-coded in `AnimeQuoteWall.iss`:

```
#define MyAppVersion "1.3.0"
```

When you bump the version in `AnimeQuoteWall.GUI.csproj`, also update this
line so the produced filename and uninstall entry match.

## Per-user vs machine-wide installs

The installer defaults to a per-user install (no UAC prompt). The wizard
offers an in-dialog elevation toggle (via
`PrivilegesRequiredOverridesAllowed=dialog`) so a user can choose to
install for all users. The `AppId` GUID is stable across both modes, so
upgrades flow naturally.

## Uninstall behaviour

Uninstall removes only files placed by the installer plus the runtime
cache at `%LOCALAPPDATA%\AnimeQuoteWall\cache`. Your wallpaper history,
quote library, and user settings are **not** deleted by uninstall - this
matches the Microsoft guidance for desktop apps that store user data
under `%LOCALAPPDATA%`.

## Troubleshooting

- **"Inno Setup 6 (iscc.exe) was not found"** - install Inno Setup 6.3+ or
  add its folder to `PATH`.
- **"dotnet publish failed"** - inspect the dotnet error output; the
  most common cause is a missing .NET 8 SDK.
- **Wizard images look stretched or wrong colour** - verify the BMPs are
  24-bit, not 32-bit with alpha; Inno Setup classic limitation.
- **Installer refuses to run on the target PC** - the user is probably
  missing the .NET 8 Desktop Runtime; the installer will detect this and
  offer to open the download page.
