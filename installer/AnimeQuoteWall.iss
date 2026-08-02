; ===========================================================================
;  AnimeQuoteWall - Inno Setup Script
; ---------------------------------------------------------------------------
;  Builds a single setup.exe that walks the user through:
;     Welcome -> License -> Install Dir -> Progress -> Finish (Launch checkbox)
;  Produced installer targets Windows 10 1809 (build 17763) and newer, x64.
;  Requires Inno Setup 6.3 or later (for WizardStyle=modern + per-user mode).
; ===========================================================================

#define MyAppName            "AnimeQuoteWall"
#define MyAppVersion         "1.3.0"
#define MyAppPublisher       "Apexone11"
#define MyAppURL             "https://github.com/Apexone11/AnimeQuoteWall"
#define MyAppSupportURL      "https://github.com/Apexone11/AnimeQuoteWall/issues"
#define MyAppExeName         "AnimeQuoteWall.exe"
#define MyAppCopyright       "Copyright (C) 2026 Apexone11"

; SourcePath is the directory that contains this .iss file (provided by ISCC).
; The published payload is expected at <repo-root>\publish\win-x64 which is
; one level above this script (installer\..\publish\win-x64).
#define PublishDir           SourcePath + "..\publish\win-x64"

[Setup]
; Stable AppId. Changing this value will cause new installs to register as a
; separate product instead of upgrading the previous version - do not edit.
AppId={{B7A1F4E2-3C9D-4E8B-9F2A-1D6E5C8B7A91}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppSupportURL}
AppUpdatesURL={#MyAppURL}/releases
AppCopyright={#MyAppCopyright}
VersionInfoVersion={#MyAppVersion}
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion={#MyAppVersion}

; Default to per-user install under %LOCALAPPDATA%\Programs so non-admin users
; can install without UAC. The user may elevate via the in-wizard dialog
; (PrivilegesRequiredOverridesAllowed) to install machine-wide.
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
UsePreviousAppDir=yes
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog commandline

; Visual configuration - modern wizard style (Inno 6.3+).
; WizardResizable was removed: it is obsolete in Inno 6.3+ and the modern
; wizard handles resize behaviour internally.
WizardStyle=modern
ShowLanguageDialog=no

; Branding image slots. The repo ships placeholders documentation only -
; supply the real BMPs at installer\assets\ before building. See README.
WizardImageFile=assets\wizard-side.bmp
WizardSmallImageFile=assets\wizard-small.bmp
SetupIconFile=assets\setup-icon.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName} {#MyAppVersion}

; License presented on the second wizard page.
LicenseFile=assets\LICENSE.rtf

; Output configuration.
OutputDir=dist
OutputBaseFilename=AnimeQuoteWall-Setup-{#MyAppVersion}

; Compression - lzma2/ultra64 gives the smallest setup at the cost of a slower
; build. Solid compression bundles all files into one stream for better ratio.
Compression=lzma2/ultra64
SolidCompression=yes
LZMAUseSeparateProcess=yes

; Platform constraints. We ship x64-only WPF binaries.
; x64compatible matches native x64 AND ARM64-on-x64 emulation (Inno 6.3+).
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763

; Misc UX polish.
CloseApplications=force
RestartApplications=no
AllowNoIcons=yes
DisableWelcomePage=no
DisableReadyPage=no
DisableFinishedPage=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
; desktopicon and startmenuicon are checked by default; startupicon (launch
; on Windows sign-in) is unchecked so we do not change autostart behaviour
; without explicit user consent.
Name: "desktopicon";   Description: "Create a &desktop shortcut";                                   GroupDescription: "Additional shortcuts:";
Name: "startmenuicon"; Description: "Create a Start &Menu shortcut";                                GroupDescription: "Additional shortcuts:"
Name: "startupicon";   Description: "Launch {#MyAppName} when I sign in to Windows (auto-start)"; GroupDescription: "Startup options:";                                                                Flags: unchecked

[Files]
; Copy the entire published output tree into {app}. ignoreversion lets a
; reinstall always overwrite; recursesubdirs + createallsubdirs preserves the
; folder structure (Resources\, Services\, runtime subfolders, etc.).
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
; Always-present Start Menu entry under the publisher group.
Name: "{autoprograms}\{#MyAppName}";              Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\{#MyAppExeName}"; Tasks: startmenuicon
Name: "{autoprograms}\Uninstall {#MyAppName}";    Filename: "{uninstallexe}";        WorkingDir: "{app}"; Tasks: startmenuicon

; Optional Desktop shortcut (task-gated).
Name: "{autodesktop}\{#MyAppName}";               Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

; Optional Startup folder entry so the app launches at Windows sign-in.
Name: "{autostartup}\{#MyAppName}";               Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\{#MyAppExeName}"; Tasks: startupicon

[Run]
; Offer to launch the app once installation finishes. skipifsilent keeps /SILENT
; deployments quiet; nowait returns control to the installer immediately.
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName} now"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Wipe only the runtime cache. User-authored content (wallpaper history,
; settings, custom quotes) under %LOCALAPPDATA%\AnimeQuoteWall\ is preserved
; as required by Windows app data retention best practice.
Type: filesandordirs; Name: "{localappdata}\AnimeQuoteWall\cache"

; ===========================================================================
;  [Code] - Pascal pre-install checks
; ---------------------------------------------------------------------------
;  Detects whether the .NET 8 Desktop Runtime is installed by parsing the
;  output of `dotnet --list-runtimes`. If the user is missing it, we offer
;  to open the official download page and abort the install so they can
;  return after installing the prerequisite.
; ===========================================================================

[Code]
const
  DOTNET_DOWNLOAD_URL = 'https://dotnet.microsoft.com/en-us/download/dotnet/8.0/runtime?cid=getdotnetcore';

function IsDotNet8DesktopInstalled(): Boolean;
var
  TempFile: string;
  ResultCode: Integer;
  RuntimesText: AnsiString;
begin
  Result := False;
  TempFile := ExpandConstant('{tmp}\dotnet-runtimes.txt');

  // Invoke `dotnet --list-runtimes` via cmd.exe so we can redirect stdout.
  // If the dotnet CLI is not on PATH at all, the process will fail and we
  // treat that the same as "runtime missing".
  if not Exec(ExpandConstant('{cmd}'), '/C dotnet --list-runtimes > "' + TempFile + '" 2>&1',
              '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
    Exit;

  if not LoadStringFromFile(TempFile, RuntimesText) then
    Exit;

  // Any line beginning with "Microsoft.WindowsDesktop.App 8." satisfies the
  // requirement (8.0.x patch level does not matter for WPF apps).
  Result := Pos('Microsoft.WindowsDesktop.App 8.', String(RuntimesText)) > 0;
end;

function InitializeSetup(): Boolean;
var
  Response: Integer;
begin
  Result := True;
  if IsDotNet8DesktopInstalled() then
    Exit;

  // Runtime missing. Ask the user whether to open the download page.
  Response := MsgBox(
    'AnimeQuoteWall requires the .NET 8 Desktop Runtime, which was not detected on this PC.' + #13#10 + #13#10 +
    'Click Yes to open the official Microsoft download page in your browser, then re-run this installer once the runtime is installed.' + #13#10 + #13#10 +
    'Click No to abort installation.',
    mbConfirmation, MB_YESNO);

  if Response = IDYES then
  begin
    ShellExec('open', DOTNET_DOWNLOAD_URL, '', '', SW_SHOWNORMAL, ewNoWait, Response);
  end;

  // Either way, we cannot safely install without the runtime.
  Result := False;
end;
