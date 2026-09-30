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

# Build + run tests; also produces the Debug exe used for manual testing:
dotnet test "Adventure League Log Downloader.Tests"
# Debug exe: Adventure League Log Downloader\bin\Debug\net10.0-windows\win-x64\Adventure League Log Downloader.exe
# (older bin\Release and net9.0 folders may be stale — check timestamps)
```

### What's Implemented
- Auth with CSRF/cookie handling and userId discovery
- Character list scraping with robust pagination
- Windows Credential Manager for saved credentials
- Settings persistence to `%AppData%/AllogDownloader/settings.json`
- Options dialog (configurable network delay)
- Characters JSON export
- Per-character CSV download (`character_{id}.csv`) and **File → Export session log workbook (CSV)…** (`session_log_workbook.csv`) — see `docs/examples/spreadsheet-ken-ddal-log.md`
- DM session list scraping (**DM Sessions…** window → `dm_sessions.json`)
- Moonsea Codex handoff: **Import to Moonsea Codex…** in the character detail window writes an MSC-ready `moonseacodeximport.csv` to Downloads (browser-style ` (N)` suffix if taken), selects it in Explorer, and opens `https://moonseacodex.com/characters` (`Services/MoonseaCodexHandoff.cs`, `MoonseaCodexCsvWriter.cs`)

### Moonsea Codex (MSC) notes
- MSC source: [API](https://github.com/MoonseaCodex/API) (Django backend), [WebUI](https://github.com/MoonseaCodex/WebUI) (Next.js frontend)
- MSC's importer (`codex/imports/csv.py`) requires the site's CSV header lines verbatim, then splits each line on `,` with no quote handling and reads session fields by index up to 15 — a site row with multi-line notes and no comma on the first line is silently dropped. `MoonseaCodexCsvWriter` keeps the headers exact and flattens fields (line breaks → space, `,` → `;`)
- MSC does `bool(publicly_visible)`, so any non-empty value (including `false`) means public. The writer emits empty for not public, `true` for public; default is not public (`MoonseaCodexHandoff.DefaultMakePublic`). MSC's `public` flag only filters its Discord bot listings — character pages and the API serve any character by UUID, and the API never returns the flag, so it can't be verified after import
- MSC's `Character.save()` recalculates level as the sum of class levels parsed from `class_and_levels` (`codex/imports/parse_classes.py`), overwriting the importer's session count. Site class text is often stale or has no level (`Wizard-1` on a level-15 character), so imports show level 1. `parse_classes` also has `(barbarian)(bard)` with no `|`, so Bard/Barbarian don't match
- A real import (2026-09-29) matched the emulator: 15 of 15 sessions with flattened notes
- `Adventure League Log Downloader.Tests/MoonseaCodexImportEmulator.cs` reimplements MSC's import rules (written from their behavior, not copied — their API is GPL-3.0) so tests assert what MSC would create from our output. Checked against MSC's actual Python on 65 real CSVs (original + converted): identical results. If MSC changes its importer, update the emulator first
- `/characters` redirects logged-out users to `/auth/login`, and both login paths (password and Discord) come back to `/characters`
- MSC's API accepts only its own browser session cookie (no API tokens) and its Discord OAuth runs on MSC's server, so the app can't upload on the user's behalf. One-click upload would need token access from the MSC maintainers

### Spreadsheet-style reference (Ken DDAL Log example)

- **Google Sheet (bookmark):** [Ken DDAL Log](https://docs.google.com/spreadsheets/d/1bqbClFX-MMgIWDKbnEEmmxBvwzO6wYSXm_ig690kojA/edit?usp=sharing)
- **Doc:** `docs/examples/spreadsheet-ken-ddal-log.md` — column layout, site CSV mapping, workbook export mapping, changelog, and future suggestions

### What's Pending (see TASKS.md for full checklist)
- DM session CSV export
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
- DM session CSV export is pending in C# (scraping and JSON are done)
- See TASKS.md for the full checklist

## Security / credentials
- Policy and wording for stored site passwords (obfuscation vs encryption, opt-out, CLI): see **`SECURITY.md`**.

## Archived Python Code
The original Python scripts are in `archive/python/` — reference only, not maintained. See `archive/python/README.md`.
