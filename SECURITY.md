# Security Policy

## 🔒 Security Overview

AnimeQuoteWall is designed with privacy and security in mind. This document outlines our security practices and how to report vulnerabilities.

## ✅ Security Features

### Privacy Protection

1. **No Data Collection**: This application does NOT collect, transmit, or store any user data remotely
2. **Local Storage Only**: All data remains on your local machine
3. **No Network Access**: The application operates completely offline
4. **No Telemetry**: No analytics or tracking of any kind
5. **No External Dependencies**: No third-party services are contacted

### Data Storage

All user data is stored locally at:
```
C:\Users\YOUR_USERNAME\AppData\Local\AnimeQuotes\
```

This location:
- ✅ Is user-specific (not accessible by other users)
- ✅ Is excluded from version control (`.gitignore`)
- ✅ Contains only user-created content (quotes, images, wallpapers)
- ✅ Can be deleted anytime without affecting the application

### Personal Information

The application **DOES NOT**:
- ❌ Access your personal files outside the designated folder
- ❌ Connect to the internet
- ❌ Send data to any servers
- ❌ Track your usage
- ❌ Collect personally identifiable information
- ❌ Access system information beyond what's needed for wallpaper setting

The application **ONLY**:
- ✅ Reads images from the designated backgrounds folder
- ✅ Reads quotes from the local JSON file
- ✅ Writes generated wallpapers to the designated folder
- ✅ Calls Windows API to set desktop wallpaper

## 🛡️ Security Best Practices

### For Users

1. **Source Code Review**: The entire source code is open and available for review
2. **Build from Source**: You can build the application yourself to verify its contents
3. **Antivirus Scanning**: Feel free to scan the built executable with your antivirus
4. **Data Backup**: Back up your quotes.json if you've added custom content

### For Developers

1. **No Secrets in Code**: Never commit sensitive information, API keys, or personal data
2. **Input Validation**: Validate all user inputs (file paths, JSON data)
3. **Safe File Operations**: Use proper error handling for file I/O
4. **Windows API Safety**: Follow Microsoft's best practices for P/Invoke calls

## 🐛 Reporting Vulnerabilities

If you discover a security vulnerability, please report it responsibly:

### Private Reporting (Preferred)

For serious security issues, please:
1. **DO NOT** open a public issue
2. Email the maintainers directly (create a private security advisory on GitHub)
3. Include:
   - Description of the vulnerability
   - Steps to reproduce
   - Potential impact
   - Suggested fix (if applicable)

### Public Reporting

For minor issues that don't pose immediate risk:
1. Open a GitHub issue with `[SECURITY]` prefix
2. Describe the issue clearly
3. Suggest improvements

## 📋 Security Checklist

Before each release, we verify:

- [ ] No hardcoded sensitive information
- [ ] No unnecessary file system access
- [ ] No network calls
- [ ] Proper input validation
- [ ] Safe file operations with error handling
- [ ] No execution of external commands
- [ ] Proper use of Windows APIs
- [ ] `.gitignore` properly excludes user data

## 🔐 Permissions Required

The application requires:

| Permission | Reason | Risk Level |
|------------|--------|------------|
| File System Read/Write | Read backgrounds, write wallpapers | Low |
| Windows API (User32.dll) | Set desktop wallpaper | Low |

The application does **NOT** require:
- ❌ Administrator privileges
- ❌ Network access
- ❌ Registry modifications (beyond wallpaper setting)
- ❌ Access to user documents, downloads, etc.

## 🌐 External Dependencies

### NuGet Packages

- `System.Drawing.Common` - Microsoft official package for image processing
- `System.Text.Json` - Microsoft official package for JSON parsing

All dependencies are from Microsoft and are regularly updated.

### Windows APIs

- `user32.dll` - Standard Windows API for wallpaper setting
  - Function: `SystemParametersInfo`
  - Purpose: Set desktop wallpaper
  - Risk: Low (standard Windows functionality)

## 📱 Safe Usage Guidelines

1. **Run from Trusted Sources**
   - Download from official GitHub repository
   - Build from source if uncertain

2. **Review Code**
   - Source code is fully available
   - Review before running if concerned

3. **Use Standard Locations**
   - Keep data in default `%LOCALAPPDATA%` location
   - Don't modify paths unless necessary

4. **Regular Updates**
   - Keep .NET runtime updated
   - Use latest version of the application

## 🔄 Update Policy

- Security patches will be released as soon as possible
- Users will be notified via GitHub releases
- Critical issues will be marked with `[SECURITY]` tag

## ❓ Questions?

If you have security questions or concerns:
- Open a GitHub Discussion
- Email maintainers
- Review the source code

## 📄 Disclaimer

This software is provided "as is" without warranty of any kind. While we take security seriously and follow best practices, users should:
- Review the code themselves if concerned
- Use at their own discretion
- Report any issues discovered

---

**Last Updated**: October 15, 2025

Thank you for helping keep AnimeQuoteWall secure! 🔒
