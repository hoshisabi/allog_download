# CLAUDE.md

## Project Overview

`allog_download` is a tool for downloading and processing data from AdventurersLeagueLog.com (Adventurers League character/session logs).

**Active development target:** The C# WPF App (`Adventure League Log Downloader/`) — a native Windows GUI app built on .NET 10.

**Archived:** `archive/python/` contains the original Python scripts that preceded the C# app. They are not maintained and should not be treated as current code.

---

## C# WPF App

### Tech Stack
- .NET 10.0-windows, WPF
- NuGet: `HtmlAgilityPack` (HTML parsing), `CredentialManagement` (Windows Credential Manager)
- IDE: Visual Studio 2022 or JetBrains Rider

### Build & Publish
```powershell
# Debug run — open Adventure League Log Downloader.sln in Visual Studio
# Single-file release build for distribution:
dotnet publish "Adventure League Log Downloader" -p:PublishProfile=FolderProfile
# Output: Adventure League Log Downloader\bin\Release\net10.0-windows\publish\win-x64\
```

### What's Implemented
- Auth with CSRF/cookie handling and userId discovery
- Character list scraping with robust pagination
- Windows Credential Manager for saved credentials
- Settings persistence to `%AppData%/AllogDownloader/settings.json`
- Options dialog (configurable network delay)
- Characters JSON export
- Per-character CSV download (`character_{id}.csv`) and **File → Export session log workbook (CSV)…** (`session_log_workbook.csv`) — see `docs/examples/spreadsheet-ken-ddal-log.md`

### Spreadsheet-style reference (Ken DDAL Log example)

- **Google Sheet (bookmark):** [Ken DDAL Log](https://docs.google.com/spreadsheets/d/1bqbClFX-MMgIWDKbnEEmmxBvwzO6wYSXm_ig690kojA/edit?usp=sharing)
- **Doc:** `docs/examples/spreadsheet-ken-ddal-log.md` — column layout, site CSV mapping, workbook export mapping, changelog, and future suggestions

### What's Pending (see TASKS.md for full checklist)
- Progress bar and cancellation during long operations
- DM session list scraping/export
- Per-character session log downloads
- PDF export
- MVVM refactor and DI
- MSIX installer

---

## Development Environment
- **OS**: Windows (win32)
- **Output files**: Written to `out/` (gitignored)
- **Build artifacts**: `build/`, `dist/` (gitignored)

---

## Project Status Summary
- C# WPF app is the primary deliverable for non-programmer users
- DM session download/export is pending in C#
- See TASKS.md for the full checklist

## Security / credentials
- Policy and wording for stored site passwords (obfuscation vs encryption, opt-out, CLI): see **`SECURITY.md`**.

## Archived Python Code
The original Python scripts are in `archive/python/` — reference only, not maintained. See `archive/python/README.md`.
