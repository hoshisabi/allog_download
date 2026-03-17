# CLAUDE.md

## Project Overview

`allog_download` is a toolset for downloading and processing data from AdventurersLeagueLog.com (Adventurers League character/session logs). It has two components:

1. **C# WPF App** (`Adventure League Log Downloader/`) — the primary active development target; a native Windows GUI app porting Python functionality to .NET 9.
2. **Python Scripts** (`src/`) — legacy utilities still useful for batch operations and data transformation.

---

## C# WPF App

### Tech Stack
- .NET 9.0-windows, WPF
- NuGet: `HtmlAgilityPack` (HTML parsing), `CredentialManagement` (Windows Credential Manager)
- IDE: Visual Studio 2022 or JetBrains Rider

### Build & Publish
```powershell
# Debug run — open Adventure League Log Downloader.sln in Visual Studio
# Single-file release build for distribution:
dotnet publish "Adventure League Log Downloader" -p:PublishProfile=FolderProfile
# Output: Adventure League Log Downloader\bin\Release\net9.0-windows\publish\win-x64\
```

### What's Implemented
- Auth with CSRF/cookie handling and userId discovery
- Character list scraping with robust pagination
- Windows Credential Manager for saved credentials
- Settings persistence to `%AppData%/AllogDownloader/settings.json`
- Options dialog (configurable network delay)
- Characters JSON export

### What's Pending (see TASKS.md for full checklist)
- Progress bar and cancellation during long operations
- DM session list scraping/export
- Per-character session log downloads
- PDF export
- MVVM refactor and DI
- MSIX installer

---

## Python Scripts (`src/`)

### Dependency Management
The project has **migrated from `pipenv` to `uv`**. Use `uv` for all Python dependency management.

```bash
# Install dependencies
uv sync

# Run a script
uv run python src/download_all_csv.py
```

- `pyproject.toml` defines dependencies
- `uv.lock` is the lock file
- Requires **Python 3.15+**
- `.venv/` is gitignored; `uv sync` recreates it

### Scripts
| Script | Purpose |
|---|---|
| `auth.py` | Login to AdventurersLeagueLog.com, manage cookies/CSRF |
| `character_list.py` | Scrape character list for a user |
| `csv_download.py` | Download a single character CSV |
| `download_all_csv.py` | Batch-download all character CSVs (with rate-limit delay) |
| `parse_all_csv.py` | Parse all downloaded CSVs into JSON |
| `character_csv_parser.py` | Parse a single character CSV to JSON |
| `dmsession_list.py` | Scrape DM session/credit list |
| `characters_json_to_pdf.py` | Generate PDF from characters JSON (partially working) |
| `json_to_csv_zip.py` | Export characters JSON to CSV (zipped) |
| `json_to_markdown.py` | Export characters JSON to Markdown (single file or per-character, optional zip) |

### Notes
- `main.py` in the project root is a `uv init` stub — ignore it, it's not a real entry point.

### Common Usage
```bash
# Download all character CSVs (requires .env with credentials)
uv run python src/download_all_csv.py

# Parse downloaded CSVs to JSON
uv run python src/parse_all_csv.py -o out/characters.json

# Export to Markdown
uv run python src/json_to_markdown.py -j out/characters.json -o out/characters.md

# Export to Markdown per character, zipped
uv run python src/json_to_markdown.py -j out/characters.json -o out/characters.zip -t per-character -z

# Generate PDF
uv run python src/characters_json_to_pdf.py -j out/characters.json -o out/characters.pdf
```

### Environment / Credentials
Credentials are stored in `.env` (gitignored). See `.env` for the expected variables. For PDF generation, `DejaVuSans.ttf` must be placed in `src/` (gitignored).

---

## Development Environment
- **OS**: Windows (win32)
- **Python**: 3.15+, managed with `uv`
- **Output files**: Written to `out/` (gitignored)
- **Build artifacts**: `build/`, `dist/` (gitignored)

---

## Project Status Summary
- C# WPF app is the primary deliverable for non-programmer users
- Python scripts are functional for data export (CSV, Markdown, JSON) and batch download
- PDF generation (Python) is partially working, lower priority
- DM session download/export is pending in both Python and C#
