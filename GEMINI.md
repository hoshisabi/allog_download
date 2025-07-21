## Project Context

This project, `allog_download`, is a Python-based toolset for downloading and processing data from AdventurersLeagueLog.com. It includes scripts for downloading character and session data, parsing CSV files, and generating PDF reports from structured JSON data.

## User Preferences

- **Operating System**: Windows (win32). All shell commands should use `cmd.exe` syntax (e.g., `dir`, `copy`, `move`, `del`, `type`, `echo %VAR%`, and backslashes for paths).
- **Commit Messages**: User prefers commit messages to be provided in a file, specifically `maintaindb/commit_message.txt`.

## Outstanding Tasks

- **Data Export Formats**: Functionality to convert the processed JSON data into CSV (for Excel compatibility) and Markdown formats. The JSON to CSV with zipping and JSON to Markdown (with options for single/per-character files and zipping) have been implemented.
- **PDF Generation**: The PDF generation functionality is not fully working and is currently a lower priority.
- **Distributables for Non-Programmers**: Investigate and implement methods to create easy-to-use distributables for users without programming knowledge. This will be re-evaluated after data export formats are addressed.
- **User-Friendly GUI**: Develop a simple graphical user interface to allow non-developers to easily input information and interact with the tool.
- **Secure Credential Storage**: Implement a secure method for storing user credentials (e.g., for AdventurersLeagueLog.com) in a non-plaintext format.

## Future Tasks / Interests

- **Scraping Bypass**: User is exploring creating a bookmarklet or browser extension to scrape DMsGuild page data (or full HTML) and save it to `maintaindb/_dc/` to bypass Cloudflare blocking automated scraping.
- **GitHub Actions**: User is interested in setting up periodic execution of `dmsguild_rss_parser.py` (from the `al_adventure_catalog` project, but likely relevant here for automated data updates) using GitHub Actions. They will ask for help on how to do this later.

## Development Environment

- **Dependency Management**: `pipenv` is the preferred tool for managing Python dependencies.
- **Project Root**: `f:\Users\decha\Documents\Projects\allog_download`