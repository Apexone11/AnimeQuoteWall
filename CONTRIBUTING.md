# Contributing to AnimeQuoteWall

First off, thank you for considering contributing to AnimeQuoteWall! 🎌

## How Can I Contribute?

### 🐛 Reporting Bugs

Before creating bug reports, please check existing issues to avoid duplicates. When creating a bug report, include:

- **Description**: Clear description of the issue
- **Steps to Reproduce**: Detailed steps to reproduce the behavior
- **Expected Behavior**: What you expected to happen
- **Actual Behavior**: What actually happened
- **Screenshots**: If applicable
- **Environment**:
  - OS Version (Windows 10/11)
  - .NET Version
  - Application Version

### 💡 Suggesting Enhancements

Enhancement suggestions are welcome! Please include:

- **Use Case**: Why this enhancement would be useful
- **Description**: Clear description of the feature
- **Examples**: Mock-ups or examples if applicable
- **Alternatives**: Alternative solutions you've considered

### 📝 Pull Requests

1. **Fork the Repository**
2. **Create a Branch**: `git checkout -b feature/YourFeature`
3. **Make Changes**: Follow coding standards below
4. **Test**: Ensure your changes work correctly
5. **Commit**: Use clear commit messages
6. **Push**: `git push origin feature/YourFeature`
7. **Submit PR**: Open a pull request with description

## Coding Standards

### C# Style Guide

- Use **PascalCase** for class names and public members
- Use **camelCase** for local variables and parameters
- Use **meaningful names** for variables and methods
- Add **XML documentation** for public methods
- Keep methods **small and focused**
- Use **LINQ** where appropriate for readability

### Example

```csharp
/// <summary>
/// Loads quotes from the specified JSON file
/// </summary>
/// <param name="path">Path to the quotes JSON file</param>
/// <returns>List of valid quotes</returns>
static List<Quote> LoadQuotes(string path)
{
    var json = File.ReadAllText(path);
    var list = JsonSerializer.Deserialize<List<Quote>>(json) ?? new List<Quote>();
    return list.Where(q => !string.IsNullOrWhiteSpace(q.Text)).ToList();
}
```

### Commit Message Format

```
type: brief description

Detailed description if needed

Fixes #issue_number
```

**Types:**
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `style`: Code style changes (formatting, etc.)
- `refactor`: Code refactoring
- `test`: Adding tests
- `chore`: Maintenance tasks

**Examples:**
```
feat: add GIF export functionality

Implemented GIF export using animation frames.
Users can now export their wallpapers as animated GIFs.

Fixes #42
```

```
fix: resolve wallpaper setting issue on Windows 11

Updated Win32 API call to include proper flags for Windows 11 compatibility.

Fixes #15
```

## Development Setup

1. **Install Prerequisites**
   - [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
   - [Git](https://git-scm.com/)
   - [Visual Studio Code](https://code.visualstudio.com/) or [Visual Studio](https://visualstudio.microsoft.com/)

2. **Clone Repository**
   ```bash
   git clone https://github.com/YOUR_USERNAME/AnimeQuoteWall.git
   cd AnimeQuoteWall
   ```

3. **Build Project**
   ```bash
   dotnet build
   ```

4. **Run Project**
   ```bash
   dotnet run
   ```

## Testing

Before submitting a PR:

1. ✅ Build the project successfully
2. ✅ Test basic wallpaper generation
3. ✅ Test with and without background images
4. ✅ Test quote loading and validation
5. ✅ Test frame generation
6. ✅ Verify no crashes or errors

## Documentation

When adding features:

- Update `README.md` if user-facing
- Add XML comments to public methods
- Update relevant documentation in `docs/`
- Include usage examples

## Questions?

Feel free to:
- Open an issue for discussion
- Start a GitHub Discussion
- Contact maintainers

Thank you for contributing! 🎉
