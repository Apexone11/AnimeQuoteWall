# Security Policy

## Reporting a vulnerability

If you discover a security vulnerability in AnimeQuoteWall, please report it privately via one of:

- Open a GitHub Security Advisory: <https://github.com/Apexone11/AnimeQuoteWall/security/advisories/new>
- Email the maintainer at the address listed on the GitHub profile

Please do NOT open a public issue with vulnerability details. We will acknowledge the report within 7 days and provide a remediation timeline within 14 days for confirmed issues.

When reporting, include where possible:

- Affected version (see About dialog or `AnimeQuoteWall.GUI.csproj` `<Version>`).
- Reproduction steps or proof-of-concept input.
- Observed and expected behavior.
- Impact assessment (data exposure, code execution, denial of service, etc.).

## Privacy

AnimeQuoteWall is designed to be offline-first and telemetry-free.

- The app has no analytics, telemetry, error reporting, or remote update endpoints other than the optional Wallpaper Engine localhost API (`http://127.0.0.1:7070`) and an offered redirect to the official .NET 8 runtime download page during installation.
- All user data is stored under `%LOCALAPPDATA%\AnimeQuoteWall\`. The app never writes outside this directory and the user-chosen wallpaper directories.
- Logs at `%LOCALAPPDATA%\AnimeQuoteWall\logs\error_<yyyymmdd>.txt` contain exception text and stack traces. They never contain quote text, credentials, or remote URLs.
- The Windows registry is written only at the documented wallpaper key (`HKCU\Control Panel\Desktop\Wallpaper`).

## Hardening posture (current)

The following protections are implemented and tested:

- **Path-traversal protection.** All filesystem reads, writes, copies, and deletes of user- or JSON-supplied paths route through `AnimeQuoteWall.Core.Services.SafePath`, which canonicalizes the path with `Path.GetFullPath`, asserts containment under an allowed root, and rejects reparse points.
- **Argument-injection protection.** Every external process invocation (FFmpeg, Wallpaper Engine) uses `ProcessStartInfo.ArgumentList` instead of `Arguments` string concatenation, so paths containing quotes or shell metacharacters cannot escape an argument.
- **FFmpeg PATH refusal.** The animated wallpaper service refuses to fall back to FFmpeg on `PATH`; only the bundled binary at `Resources/ffmpeg/ffmpeg.exe` is invoked.
- **ImageMagick resource limits.** `ImageMagickHardening.Apply()` runs at startup and caps image width/height, memory, disk, and worker threads to bound the impact of a malicious image.
- **Image magic-byte validation.** Imported images are sniffed for JPEG, PNG, GIF, BMP, or WebP signatures via `SafePath.LooksLikeSupportedImage` before being passed to the imaging pipeline.
- **HttpClient lifecycle.** Wallpaper Engine HTTP probes use a single shared `HttpClient` with a 5-second timeout and a 3-second connect timeout; auto-redirect is disabled.
- **Fail-closed integrity checks.** `CodeProtection` fails closed (returns false) in Release builds when integrity verification throws; only Debug builds fail open for developer convenience.
- **Settings backup on parse failure.** If `settings.json` cannot be parsed, the corrupt file is backed up to `settings.json.corrupt-<timestamp>` instead of being silently overwritten.

## Known limitations

- The `Protection` namespace (`CodeProtection`, `StringEncryption`) provides surface-level obfuscation only. It is NOT a substitute for code signing. Steam release builds rely on Authenticode signing of the installer and the main executable.
- Magick.NET inherits ImageMagick CVEs. We track the upstream releases (currently pinned to Magick.NET-Q8-AnyCPU 14.13.1) via Dependabot. Risky coders are out of scope for our usage (MVG, MSL, JP2, JXL, etc.) and are not exercised by any code path.
- Wallpaper Engine integration assumes the local Wallpaper Engine binary and its localhost API are trustworthy. We validate that responses come from `127.0.0.1:7070` and apply a strict timeout but cannot vouch for Wallpaper Engine itself.

## Supported versions

Only the latest released version receives security updates. The current version is recorded in `AnimeQuoteWall.GUI.csproj` and shown in the About dialog.
