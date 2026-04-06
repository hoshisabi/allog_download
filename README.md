# allog_download

A Windows desktop app for downloading and processing character/session data from AdventurersLeagueLog.com.

## Adventure League Log Downloader (C# WPF App)

The primary deliverable. A native Windows GUI app built on .NET 9.

### Features
- Native Windows authentication via Credential Manager
- Scrapes character lists and logs directly from AdventurersLeagueLog.com
- Export to JSON (more formats coming)

### Installation

Official builds are published on **[Releases](https://github.com/hoshisabi/allog_download/releases)**. On the release page, under *Assets*:

- **`AdventurersLeagueLogDownloader-vX.X.X-win-x64-selfcontained.zip`** — larger file, includes the .NET runtime. Use this if unsure.
- **`AdventurersLeagueLogDownloader-vX.X.X-win-x64-framework.zip`** — smaller file. Requires [.NET 9 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/9.0) for Windows x64 installed first.

**Requirements:** Windows 64-bit only.

Extract the ZIP to a folder you keep (e.g. `Documents\AllogDownloader`) and double-click `Adventure League Log Downloader.exe`.

### Building

```powershell
dotnet publish "Adventure League Log Downloader" -p:PublishProfile=FolderProfile
```

Output: `Adventure League Log Downloader\bin\Release\net9.0-windows\publish\win-x64\`

### Development

Open `Adventure League Log Downloader.sln` in Visual Studio 2022 or JetBrains Rider. Target framework: `.NET 9.0-windows`.

---

## Archived Python Scripts

The original Python implementation lives in `archive/python/`. It is **not maintained** — preserved as a reference for the logic that was ported to the C# app. See [`archive/python/README.md`](archive/python/README.md) for details.
