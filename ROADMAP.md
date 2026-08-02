# Roadmap

This roadmap tracks planned fixes and improvements. The focus is refining existing
features and shipping a stable release rather than adding major new ones.

## Shipped in 2.0.0

See [CHANGELOG.md](CHANGELOG.md) for the full list. Highlights that closed out
long-standing roadmap entries:

- Per-monitor wallpaper now works (the OS version gate was unsatisfiable on Windows 10/11)
- System tray icon with quick actions, plus minimize-to-tray
- Start with Windows
- Window size and position remembered across launches
- Global hotkey (Ctrl+Alt+Q) and single-instance behaviour
- Settings backup and restore (export/import ZIP)
- Keyboard shortcuts (Ctrl+1 through Ctrl+7 for navigation)
- Wallpaper history browser: favorites, search, and quick restore
- Quote search plus category and favorites filtering
- Background fit modes (Fill / Fit / Stretch / Center) and filter effects
- Performance and power controls (pause on battery, fullscreen, RDP, per-app; Low-Power mode)
- Memory and responsiveness: list virtualization, bounded image cache, no UI-thread blocking
- Theme switching applies immediately with no restart
- Installer plus Velopack auto-update, and a Setup.exe that installs when missing

## Bug Fixes and Stability

### High Priority

- [ ] Improve video wallpaper stability
- [ ] Better error messages for missing .NET runtime
- [ ] Further memory reduction for very large image libraries

### Medium Priority

- [ ] Improve thumbnail generation speed
- [ ] Better handling of corrupted image files beyond the magic-byte gate

## Feature Improvements

### Existing Features to Enhance

- [ ] **Quote Management**
  - Import/export quotes to CSV or JSON
  - Duplicate quote detection

- [ ] **Playlist System**
  - Preview mode (test without applying)
  - Weekday-specific schedules surfaced in the UI (the Core model already supports `DaysOfWeek`)
  - Playlist templates (pre-configured schedules)

- [ ] **User Interface**
  - Font size options for better readability
  - Drag-and-drop for images and videos

- [ ] **Wallpaper Generation**
  - More text overlay positions
  - Text shadow/outline customization
  - Brightness/contrast adjustment slider
  - Preview different quotes on the same background

- [ ] **Animation Export**
  - Progress indicator for long exports
  - Batch export multiple wallpapers
  - More animation effects (zoom, rotate)
  - Re-expose the framerate cap and render scale settings once the GUI consumes them

- [ ] **Performance**
  - Faster startup time
  - Background image caching across sessions

### Small Additions

- [ ] Random wallpaper button on the main screen

## Testing

- [x] Stand up `AnimeQuoteWall.Core.Tests` (xUnit) per CLAUDE.md Section 9
- [x] Cover `SafePath` containment and id sanitization, magic-byte sniffing,
      ffmpeg and Wallpaper Engine argument construction, and per-monitor dispatch
- [ ] Theme switch round-trip (needs a WPF/STA harness, so it does not fit the
      current WPF-free Core test project)
- [ ] A CI workflow implementing the CLAUDE.md Section 11 stages. Only the CodeQL
      workflow exists today, so `dotnet build`, `dotnet format`, `dotnet test`, and
      the vulnerability audit currently run locally only
- [ ] Integration tests for the file and process I/O services, behind
      `[Trait("Category","Integration")]`

## Documentation

- [ ] Video tutorials for new users
- [ ] FAQ page for common questions
- [ ] Troubleshooting guide
- [ ] Performance tips guide

## Future Considerations

Ideas that may be explored later:

- Steam Workshop integration
- Custom font selection UI
- Quote categories/tags
- Community quote sharing (privacy-respecting)
- Portable mode (run from USB drive)

## Timeline

No fixed dates, but the general order:

1. **2.0.0** - release hardening, security, installer and updater, the feature set above
2. **Next** - quote import/export and the test project
3. **Later** - wallpaper generation controls and animation export improvements

## Suggest Features

Have an idea? Open an issue on GitHub with the `enhancement` label.

Priorities are:

- Improvements to existing features
- Bug fixes and stability
- User experience enhancements
- Performance optimizations

---

**Last Updated**: 2026-08-02
**Focus**: Stability and refinement over new features
