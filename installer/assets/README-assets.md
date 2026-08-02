# Installer Branding Assets

The Inno Setup script references three branding files that are **not**
checked into the repository. You must supply them before running
`build.ps1`, otherwise the compiler will abort with a "file not found"
error.

Drop the finished files directly into this folder (`installer/assets/`)
using the exact names listed below.

## Required files

### 1. `wizard-side.bmp`

The large vertical banner that appears on the left of the Welcome and
Finish pages.

| Property | Value |
|----------|-------|
| Format | Windows Bitmap (.bmp) |
| Bit depth | 24-bit (no alpha channel) |
| Dimensions | 164 x 314 pixels (exact) |
| Aspect | portrait |
| Colour space | sRGB |

Design notes:

- Bleed art to all four edges; Inno Setup does not add padding.
- Place the wordmark in the upper third so it stays clear of the wizard
  text overlay.
- Keep the bottom third visually quiet - the Finish page renders the
  "completion" text near the bottom of the banner.

### 2. `wizard-small.bmp`

The small badge shown in the upper-right of every inner wizard page
(License, Install Dir, Ready, Progress).

| Property | Value |
|----------|-------|
| Format | Windows Bitmap (.bmp) |
| Bit depth | 24-bit (no alpha channel) |
| Dimensions | 55 x 58 pixels (exact) |
| Colour space | sRGB |

Design notes:

- Treat this like a favicon - a single, recognisable mark.
- Avoid fine type; at 55 pixels wide most text will alias unreadably.

### 3. `setup-icon.ico`

The shell icon embedded into `setup.exe` itself; also reused by Windows
for the Apps and Features uninstall entry via
`UninstallDisplayIcon={app}\AnimeQuoteWall.exe` at runtime.

| Property | Value |
|----------|-------|
| Format | Windows Icon (.ico) |
| Required sizes (must include all) | 16 x 16, 32 x 32, 48 x 48, 256 x 256 |
| Recommended additional sizes | 24 x 24, 64 x 64, 128 x 128 |
| Bit depth | 32-bit (with alpha) |

Quick option: copy the existing app icon.

```powershell
Copy-Item ..\..\AnimeQuoteWall.GUI\Resources\appicon.ico .\setup-icon.ico
```

That gives you a working installer immediately and keeps the visual
identity consistent between the setup.exe and the installed app.

## Tools that produce conformant assets

- **wizard-side.bmp / wizard-small.bmp** - export from Figma / Affinity /
  Photoshop as PNG, then convert to 24-bit BMP with ImageMagick:
  ```
  magick wizard-side.png -alpha off -type TrueColor BMP3:wizard-side.bmp
  ```
  The `BMP3:` prefix forces the legacy BMP variant Inno Setup expects.
- **setup-icon.ico** - any modern icon editor (IcoFX, Greenfish, online
  converters). Make sure the 256 x 256 layer is stored as PNG-compressed
  inside the ICO so the file does not balloon.

## Verifying before build

After dropping the files in, list them to confirm names and sizes:

```powershell
Get-ChildItem .\installer\assets\ | Select-Object Name, Length
```

Expected (file sizes will vary with content):

```
LICENSE.rtf
README-assets.md
setup-icon.ico
wizard-side.bmp
wizard-small.bmp
```

Then run `installer\build.ps1` from the repo root.
