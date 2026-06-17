# Running and Installing AnimeQuoteWall

This guide explains how to run the app and build the installer **without Visual Studio Code** -
just Windows, the .NET 8 SDK (already installed), and PowerShell. All commands are run from the
repository root: `C:\Users\Abdul PC\OneDrive\Desktop\AnimeQuoteWall`.

> Prerequisite: the .NET 8 SDK. Check with `dotnet --version` in a terminal (PowerShell or
> Command Prompt). If it prints a version, you are set.

---

## 1. Quickest: run the app directly (no install)

Open **PowerShell**, change to the repo, and run:

```powershell
cd "C:\Users\Abdul PC\OneDrive\Desktop\AnimeQuoteWall"
dotnet run --project AnimeQuoteWall.GUI\AnimeQuoteWall.GUI.csproj -c Release
```

The window opens. This is the fastest way to test changes.

## 2. Run the built EXE by double-clicking

Build once, then launch the produced executable like any normal program:

```powershell
cd "C:\Users\Abdul PC\OneDrive\Desktop\AnimeQuoteWall"
dotnet build AnimeQuoteWall.GUI\AnimeQuoteWall.GUI.csproj -c Release
```

Then open this folder in File Explorer and **double-click `AnimeQuoteWall.exe`**:

```
AnimeQuoteWall.GUI\bin\Release\net8.0-windows\AnimeQuoteWall.exe
```

Note: this build needs the .NET 8 runtime on the machine. For a copy that runs on a clean PC with
no .NET installed, use the self-contained publish below.

## 3. Self-contained EXE (runs on any Windows PC, no .NET needed)

```powershell
cd "C:\Users\Abdul PC\OneDrive\Desktop\AnimeQuoteWall"
dotnet publish AnimeQuoteWall.GUI\AnimeQuoteWall.GUI.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish
```

The single-file EXE is then at `publish\AnimeQuoteWall.exe`. Copy that one file anywhere and
double-click it.

---

## 4. Build and run the installer wizard

The installer is built with **Inno Setup 6.3+** (free, from https://jrsoftware.org/isdl.php).
Install Inno Setup first, then:

```powershell
cd "C:\Users\Abdul PC\OneDrive\Desktop\AnimeQuoteWall"
# Generate the icon + wizard branding (only needed once, or after changing the logo):
powershell -ExecutionPolicy Bypass -File installer\assets\generate-logo.ps1
# Build the installer:
powershell -ExecutionPolicy Bypass -File installer\build.ps1
```

This produces:

```
installer\dist\AnimeQuoteWall-Setup-<version>.exe
```

**Double-click that Setup EXE** to run the install wizard (Welcome -> License -> install folder ->
progress -> Finish with "Launch AnimeQuoteWall"). To build a code-signed installer (requires your
signing certificate), use `installer\build.ps1 -Sign`.

> SmartScreen note: a brand-new or unsigned installer may show a "Windows protected your PC"
> screen. Click "More info" -> "Run anyway". A code-signing certificate reduces this over time.

---

## 5. What to try once it is running

- **System tray**: minimize the window - it hides to the notification area (a tray icon appears).
  Double-click the tray icon to restore, or right-click it for Open / Hide / Exit. Toggle this in
  Settings -> Behavior -> "Minimize to the system tray".
- **Global hotkey**: press **Ctrl + Alt + Q** anywhere to show/hide the window.
- **Single instance**: launching the app again just focuses the existing window.
- **Settings -> Performance and Power**: Low-Power mode, and pause rules (on battery, maximized
  window, Remote Desktop, per-app).
- **Settings -> Behavior**: Start with Windows, "Background fit" (Fill/Fit/Stretch/Center), and
  "Background effect" (Blur/Sepia/Grayscale/Vintage). Generate a wallpaper to see the effect.
- **History page**: star a wallpaper to favorite it, filter by the "Favorites" toggle, and search
  by quote / character / anime / date.
- **Window position** is remembered between launches.

---

## 6. Status of the one-click "installs or updates" experience

You asked for the behavior where clicking the EXE installs the app if it is missing, or offers to
update/keep it if it is already installed. That is the **Velopack bootstrapper** model and is
tracked as a remaining task (auto-update). Until it is wired in, the **Inno Setup installer**
(section 4) is the install path, and updates are done by running a newer installer. The Microsoft
Store and Steam channels (planned) handle updates automatically.
