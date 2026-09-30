# allog_download

A Windows desktop app for downloading and processing character/session data from AdventurersLeagueLog.com.

## Adventure League Log Downloader (C# WPF App)

The primary deliverable. A native Windows GUI app built on .NET 10.

### Features
- Native Windows authentication via Credential Manager
- Scrapes character lists and logs directly from AdventurersLeagueLog.com
- Export to JSON (more formats coming)
- Per-character CSV downloads and a merged session log workbook CSV
- Hand a character off to [Moonsea Codex](https://moonseacodex.com/)

### Moonsea Codex import

Moonsea Codex (MSC) can create a character from an AdventurersLeagueLog.com CSV. To send one over:

1. Download the character's CSV (the **CSV** column in the character list shows which ones are on disk).
2. Double-click the character, then click **Import to Moonsea Codex…**. Explorer opens with the CSV selected, and your browser opens the MSC characters page (sign in if asked).
3. On MSC, click **Import** and drop the CSV into the dialog.

The app never sees your MSC login. Things to know about MSC's importer:

- Each import creates a **new** character. Importing the same CSV twice makes a duplicate.
- The CSV doesn't record everything, so MSC assumes every level-up was taken and no gold or downtime was spent, and it guesses magic item details. Check the result against your records.
- MSC checks the CSV header lines exactly, so upload the file as downloaded, without opening and re-saving it in Excel.

### Installation

Official builds are published on **[Releases](https://github.com/hoshisabi/allog_download/releases)**. On the release page, under *Assets*:

- **`AdventurersLeagueLogDownloader-vX.X.X-win-x64-selfcontained.zip`** — larger file, includes the .NET runtime. Use this if unsure.
- **`AdventurersLeagueLogDownloader-vX.X.X-win-x64-framework.zip`** — smaller file. Requires [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) for Windows x64 installed first.

**Requirements:** Windows 64-bit only.

Extract the ZIP to a folder you keep (e.g. `Documents\AllogDownloader`) and double-click `Adventure League Log Downloader.exe`.

**Credentials:** how stored passwords are handled (including limitations) is documented in [`SECURITY.md`](SECURITY.md).

### Building

```powershell
dotnet publish "Adventure League Log Downloader" -p:PublishProfile=FolderProfile
```

Output: `Adventure League Log Downloader\bin\Release\net10.0-windows\publish\win-x64\`

### Development

Open `Adventure League Log Downloader.sln` in Visual Studio 2022 or JetBrains Rider. Target framework: `.NET 10.0-windows`.

From the command line, `dotnet test "Adventure League Log Downloader.Tests"` builds the app and runs the tests. The Debug build it produces is at `Adventure League Log Downloader\bin\Debug\net10.0-windows\win-x64\Adventure League Log Downloader.exe`.

---

## Archived Python Scripts

The original Python implementation lives in `archive/python/`. It is **not maintained** — preserved as a reference for the logic that was ported to the C# app. See [`archive/python/README.md`](archive/python/README.md) for details.
