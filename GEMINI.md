## Project Context

This project, `allog_download`, is a toolset for downloading and processing data from AdventurersLeagueLog.com. The primary deliverable is a C# WPF app targeting non-programmer users. Python scripts in `src/` are legacy utilities that remain useful for batch operations and serve as a reference during the C# port.

## User Preferences

- **Operating System**: Windows (win32). All commands should work on Windows.

## Development Environment

- **Python Dependency Management**: `uv` (not pipenv). Use `uv sync` to install, `uv run python ...` to run scripts.
- **Python Version**: 3.15+
- **C# IDE**: Visual Studio 2022 or JetBrains Rider; solution file is `Adventure League Log Downloader.sln`
- **Project Root**: `f:\Users\decha\Documents\Projects\allog_download`

## Architecture Notes

- The C# WPF app (`Adventure League Log Downloader/`) is the active development target and will eventually absorb all Python script capabilities.
- `main.py` in the project root is a `uv init` stub — not a real entry point.
- Python scripts in `src/` are functional for: batch CSV download, CSV parsing, JSON→Markdown export, JSON→CSV/zip export. PDF generation is partially working and is the lowest priority item.
- See `TASKS.md` for the full migration checklist and priorities.
