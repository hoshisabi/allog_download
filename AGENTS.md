# AGENTS.md

## Cursor Cloud specific instructions

### Platform constraints

This is a **Windows-only WPF desktop app** targeting `net10.0-windows`. The Cloud Agent VM runs Linux, so:

- **Build**: Works on Linux. Requires `EnableWindowsTargeting=true` environment variable (already set in `~/.bashrc` by setup).
- **Run the app**: Not possible on Linux — WPF requires the Windows Desktop runtime (`Microsoft.WindowsDesktop.App`), which has no Linux distribution.
- **Unit tests (`dotnet test`)**: Cannot execute on Linux for the same reason — the test host requires `Microsoft.WindowsDesktop.App`. The CI workflow (`dotnet-desktop.yml`) runs tests on `windows-latest`.
- **Lint**: No dedicated lint tool is configured; rely on compiler warnings from `dotnet build`.

### Common commands

All commands require `EnableWindowsTargeting=true` to be set (already in `~/.bashrc`).

```bash
# Restore NuGet packages
dotnet restore "Adventure League Log Downloader.sln"

# Build (Debug)
dotnet build "Adventure League Log Downloader.sln" -c Debug

# Build (Release, matches CI PR check)
dotnet build "Adventure League Log Downloader.sln" -c Release
```

### Gotchas

- The `CredentialManagement` NuGet package emits `NU1701` warnings because it targets .NET Framework only. These warnings are expected and harmless for build purposes.
- `global.json` pins SDK to `10.0.0` with `"rollForward": "latestMajor"`, so any .NET 10.x SDK will work.
- The CI only does `dotnet build` on PRs (not publish). Publishing happens on pushes to `main`.
