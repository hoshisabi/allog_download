# Archive: Python Scripts

This directory contains the original Python implementation of the allog_download toolset.

**These scripts are archived and no longer maintained.** They are preserved here as a reference for the logic and approach used before the C# WPF app (`Adventure League Log Downloader/`) was built.

Do not treat this code as current or active. The C# app is the active development target.

---

## What's Here

| Script | Purpose |
|---|---|
| `src/auth.py` | Login to AdventurersLeagueLog.com, manage cookies/CSRF |
| `src/character_list.py` | Scrape character list for a user |
| `src/csv_download.py` | Download a single character CSV |
| `src/download_all_csv.py` | Batch-download all character CSVs |
| `src/parse_all_csv.py` | Parse downloaded CSVs into JSON |
| `src/character_csv_parser.py` | Parse a single character CSV to JSON |
| `src/dmsession_list.py` | Scrape DM session/credit list |
| `src/characters_json_to_pdf.py` | Generate PDF from characters JSON (partially working) |
| `src/json_to_csv_zip.py` | Export characters JSON to CSV (zipped) |
| `src/json_to_markdown.py` | Export characters JSON to Markdown |

Dependencies were managed with `uv` (`pyproject.toml` + `uv.lock`). `Pipfile`/`requirements.txt` are from the earlier `pipenv` era.
