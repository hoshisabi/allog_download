# Adventurers League Log Downloader — Task List

The goal is for the C# WPF app to absorb all capabilities of the Python scripts, becoming the single distributable for non-programmer users. Python scripts remain as a reference/fallback during the transition.

Checked items are completed. Unchecked are pending.

---

## 1) Project setup and foundations (C#)
- [x] Create WPF project targeting `net9.0-windows` and set up namespaces
- [x] Add HtmlAgilityPack for HTML parsing
- [x] Add Windows Credential Manager support via `CredentialManagement`
- [x] Add settings persistence service saving JSON to `%AppData%/AllogDownloader/settings.json`
- [x] Build basic README and inline documentation notes

---

## 2) UI: Main window and options
- [x] Main window with fields: Username (TextBox), Password (PasswordBox), Output folder (with Browse…), Output file name, Run button
- [x] Proper StatusBar docked at the bottom; menu with File/Options/Help
- [x] Options → Delay… dialog to configure/persist network delay
- [x] "Remember credentials" checkbox, enabled when secure storage available; loads/saves via credential store
- [ ] Non-blocking runs (async with UI responsiveness; disable/enable inputs for all long tasks)
- [ ] Progress bar + Cancel button during long-running operations
- [ ] Error surface improvements (step-specific errors; link to logs)
- [ ] Validate all fields with inline hints; preserve focus/selection on errors
- [ ] Keyboard navigation/tab order; access keys on menu and buttons
- [ ] About dialog: version/build info; link to repo/issue tracker

---

## 3) Auth and session
- [x] Implement `IAdventurersLeagueAuth` and `AdventurersLeagueAuth` with cookie/CSRF handling
- [x] Verify login and discover `userId` from returned HTML
- [x] Wire Run to authenticate using supplied credentials
- [ ] "Forget credentials" action in menu; explanatory tooltip/help text
- [ ] Cross-platform credential store abstraction (macOS Keychain, Linux Secret Service) with `IsAvailable` gating

---

## 4) Character list scraping
- [x] Port character scraping to C# `CharacterScraper`
- [x] Robust pagination detection (scan all `page=` links; fallback probing)
- [x] Table parsing resilient to missing/extra columns; only ID mandatory
- [x] Save JSON output keyed by character id with camelCase properties
- [ ] Per-page/total row counts shown in UI during run; final summary in status bar
- [ ] Option to open output file/folder after save
- [ ] Unit tests with small HTML fixtures: pagination discovery, row parsing edge cases

---

## 5) Data: downloading and parsing (port from Python)
- [ ] Per-character session log downloads (detailed pages per character)
- [ ] DM session list scraping (`dmsession_list.py` → C#)
- [ ] CSV ingest parity where Python scripts relied on downloaded CSVs as input

---

## 6) Data export formats (port from Python)
- [x] Characters JSON export
- [ ] Characters Markdown export — single file (`json_to_markdown.py` → C#)
- [ ] Characters Markdown export — per-character files, optional zip
- [ ] Characters CSV export with zip (`json_to_csv_zip.py` → C#)
- [ ] DM sessions JSON/CSV export
- [ ] Per-character session JSON/CSV export
- [ ] **Moonsea Codex (MSC) export** — convert characters JSON to MSC import format
  - Reference schema: `docs/examples/msc-export-*.json`
  - MSC item rarity uses snake_case (`very_rare`, `uncommon`, etc.) — normalize on export (the allog import has an inconsistency: `"veryrare"` vs `"very_rare"`)
  - `gold`/`downtime` should be numbers (default `0`), not null
  - `classes` must be array of `{name, value, subclass}`
  - Stats (`ac`, `hp`, `pp`, `dc`) not tracked by allog — export as `0`
  - `sheet` field can carry a D&D Beyond URL if available
- [ ] Option: append timestamp to filenames

---

## 7) Architecture and quality
- [ ] Introduce light MVVM (ViewModels for windows/dialogs; keep services UI-agnostic)
- [ ] Dependency injection for services to ease testing (auth, scraper, settings, credential store)
- [ ] Unit tests for services (auth token extraction, pagination, HTML parsing); mocks for HTTP
- [ ] File-based rolling log; user-openable from Help menu

---

## 8) Packaging and distribution
- [x] Versioning and build metadata
- [x] Single-file distribution profile (win-x64)
- [ ] First-run experience (explain credentials, delay, output defaults)
- [ ] MSIX installer packaging

---

## 9) Documentation
- [ ] Expand README with setup, usage, screenshots, and troubleshooting
- [ ] SECURITY.md: credential storage approach and limitations

---

## 10) Stretch goals / future
- [ ] Headless CLI mode reusing the same C# services (for automation/scripting)
- [ ] Background sync with scheduled fetches
- [ ] Bookmarklet or browser extension to scrape site data and bypass Cloudflare blocking
- [ ] GitHub Actions for periodic automated data updates
- [ ] Migrate to official API if/when available (switchable backend)

---

## 11) PDF export *(lowest priority)*
- [ ] PDF export in C# (port `characters_json_to_pdf.py` or re-implement using a .NET PDF lib)
- [ ] Page layout options and basic theming

---

## Legend
- [x] Completed
- [ ] Pending
