# Security Policy

## 🔒 Overview
AnimeQuoteWall is privacy-first and offline by design. This document explains our security model and how to report vulnerabilities.

## ✅ Security & Privacy

- No telemetry or tracking
- No network access or external API calls
- No credentials or secrets stored
- All data stored locally on the user's machine

### Local Storage (Example)
Files are stored in a per-user application data directory, for example:
```
%LOCALAPPDATA%\AnimeQuotes\
├── backgrounds\
├── frames\
├── quotes.json
├── current.png
└── settings.json
```

## 🧭 Best Practices We Follow

- Use of Windows’ per-user AppData for local files
- Path validation and normalization (blocks unsafe/system paths)
- Defensive file I/O with error handling
- Minimal permissions (no admin rights needed)
- Clear separation of concerns across projects

## 🐛 Reporting Vulnerabilities

Please report security issues responsibly:

### Private Reporting (Preferred)
1. Do not open a public issue
2. Create a private GitHub Security Advisory, or email the maintainers
3. Include:
   - Description and impact
   - Steps to reproduce
   - Affected versions/branches
   - Suggested remediation (if any)

### Public Reporting
For low-risk issues, open an issue with the `[SECURITY]` prefix and a clear description.

## 🔐 Release Checklist
Before each release we verify:

- [ ] No hardcoded sensitive information
- [ ] No unnecessary file system access
- [ ] No network calls or telemetry
- [ ] Proper input/path validation
- [ ] Safe file I/O with error handling
- [ ] `.gitignore` excludes user data

## 📦 Dependencies

- `System.Drawing.Common` (image processing)
- `System.Text.Json` (settings/JSON handling)

All dependencies are from known, reputable sources.

## ❗ Disclaimer
This software is provided “as is” without warranty of any kind. We take security seriously and welcome responsible disclosures.

---

Last Updated: 2025-11-10


