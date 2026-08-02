# Contributing to AnimeQuoteWall

Thank you for considering a contribution. Before opening an issue or pull request, please read this document and `CLAUDE.md`. The rules in `CLAUDE.md` are mandatory for both human contributors and AI coding assistants.

---

## Quick start

1. Fork the repo and clone your fork.
2. Install prerequisites:
   - Windows 10 1809+ x64
   - .NET 8 SDK: <https://dotnet.microsoft.com/en-us/download/dotnet/8.0>
   - Inno Setup 6.3+ (only if you intend to build the installer): <https://jrsoftware.org/isdl.php>
3. Restore and build:

```powershell
dotnet restore
dotnet build AnimeQuoteWall.GUI/AnimeQuoteWall.GUI.csproj -c Debug
dotnet run --project AnimeQuoteWall.GUI/AnimeQuoteWall.GUI.csproj
```

---

## Workflow

1. Open an issue describing the bug or feature before writing significant code, unless the change is trivial.
2. Create a feature branch off `main`: `git checkout -b feat/<short-slug>` or `fix/<short-slug>`.
3. Make focused commits using Conventional Commits.
4. Run the pre-commit checklist in `CLAUDE.md` Section 15.
5. Open a PR against `main` with a clear description and a manual smoke-test plan.
6. Address review feedback in additional commits (no force-pushes to shared branches).

---

## Conventional commits

| Type | Use for |
|---|---|
| `feat` | New user-visible functionality |
| `fix` | Bug fixes |
| `refactor` | Code restructure with no behavior change |
| `perf` | Performance improvement |
| `security` | Security-impacting change |
| `docs` | Documentation only |
| `test` | Tests only |
| `build` | Build system, dependencies, installer |
| `ci` | CI workflow changes |
| `chore` | Tooling, formatting, or other non-functional changes |

Examples:

```
feat(quotes): add bulk-import from CSV
fix(history): respect SafePath when restoring entries
security(http): pin shared HttpClient timeout for Wallpaper Engine probe
docs: rewrite README to professional style
```

Breaking changes use `!` after the type and a `BREAKING CHANGE:` footer:

```
feat!: rename PlaylistService.GetById to FindById

BREAKING CHANGE: GetById is removed.
```

---

## Coding rules (summary)

The full list lives in `CLAUDE.md`. The hard bans are repeated here so PR authors cannot miss them:

- No emojis in code, XAML strings, comments, log messages, commit messages, PR titles, or documentation.
- No hardcoded hex colors in XAML or code-behind. Use `DynamicResource` against theme tokens.
- No `StaticResource` for theme color/brush keys.
- No empty `catch {}` blocks. Always log via `App.LogException` or `System.Diagnostics.Debug.WriteLine`.
- No `--no-verify`, `--no-gpg-sign`, or force-push to `main`.
- No half-finished features merged. Revert partial work; do not commit stubs or commented-out code.

---

## Pre-commit checklist

Before opening a PR, verify all of these and state the result in the PR body:

1. `dotnet build AnimeQuoteWall.GUI/AnimeQuoteWall.GUI.csproj --no-incremental -v minimal` returns 0 errors and no new warnings.
2. Manual smoke test of the changed surface area (described in PR body).
3. `dotnet list package --vulnerable --include-transitive` returns no High or Critical.
4. No emojis added (grep the diff).
5. No hardcoded hex colors added.
6. No empty catches added.
7. No `.Result`, `.Wait()`, or `GetAwaiter().GetResult()` added on the UI dispatcher.
8. Any new external process invocations use `ProcessStartInfo.ArgumentList`.
9. Any new file-system operations route through `SafePath` for user- or JSON-supplied paths.

---

## Releasing (maintainers only)

1. Bump `<Version>` in `AnimeQuoteWall.GUI.csproj` and `AnimeQuoteWall.Core/AnimeQuoteWall.Core.csproj`.
2. Update `CHANGELOG.md`.
3. Tag the commit: `git tag -s v<version> -m "v<version>"`.
4. Run `installer/build.ps1 -Sign` on a signing-equipped machine.
5. Verify SHA-256 of the produced setup.exe matches the one printed by `build.ps1`.
6. Upload the signed setup.exe to GitHub Releases.
7. Update Steamworks build (for Steam-bound releases).

---

## Questions

Open a discussion on the GitHub repo or file an issue.
