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
- [x] **UI Cleanup & Standardization** — *BLOCKING PREREQUISITE (do before features in sections 4–6)*
  - [x] Audit and standardize all buttons (sizing, padding, font, colors) per Standard WPF best practices
  - [x] Standardize form layout (consistent spacing, alignment, input field widths)
  - [x] Ensure proper tab order and keyboard navigation across all controls
  - [x] Apply consistent styling (brushes, fonts, themes) to match WPF conventions
  - [x] Add visual feedback for focused/disabled states
- [ ] **Verify UI cleanup** — run the built app on Windows; confirm layout, spacing, Alt+mnemonics, tab order, focus and disabled visuals, and the Options dialog; note any theme issues (e.g. light vs dark).
- [ ] Non-blocking runs (async with UI responsiveness; disable/enable inputs for all long tasks) — *ESSENTIAL FOR MVP; implement alongside Section 4 features*
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
- [ ] Cross-platform credential store abstraction — *WINDOWS ONLY FOR MVP*; macOS Keychain and Linux Secret Service can be added in future iterations

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
- [ ] **Per-character session log downloads** (detailed pages per character) — *needed for full MSC/Markdown export (gold, downtime, magic items)*
- [ ] DM session list scraping (`dmsession_list.py` → C#) — *lower priority; can follow after session logs*
- [ ] CSV ingest parity where Python scripts relied on downloaded CSVs as input

---

## 6) Data export formats (port from Python)
- [x] Characters JSON export
- [ ] **Moonsea Codex (MSC) export** — *FIRST EXPORT TARGET*
  - Reference schema: `docs/examples/msc-export-*.json`
  - **v1 (character list data only):** name, race, classes array (parsed from class string), level, season, faction, campaign — gold/downtime as `0`, items as `[]`
  - **v2 (after section 5 CSV parsing):** populate gold, downtime, magic items array from session log data
  - `classes` must be array of `{name, value, subclass}` — parse from class string (e.g. "Warlock 20" or "Fighter 10/Rogue 10")
  - Stats (`ac`, `hp`, `pp`, `dc`) not tracked by allog — always export as `0`
  - `sheet` field: carry D&D Beyond URL if available (may not be scrapeable)
  - Rarity normalization: normalize to consistent format (reference files inconsistent: `very_rare` vs `veryrare`)
  - `uuid` fields: generate new GUIDs on each export
- [ ] Characters Markdown export — single file (`json_to_markdown.py` → C#)
  - Reference template: `docs/examples/markdown-export-notion-template.md` (richer format, preferred over Python version)
  - Consider Scriban for templating
- [ ] Characters Markdown export — per-character files, optional zip
- [ ] Characters CSV export with zip (`json_to_csv_zip.py` → C#)
- [ ] DM sessions JSON/CSV export
- [ ] Per-character session JSON/CSV export
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

## 12) Archive cleanup
When all Python functionality has been ported to C# (sections 5, 6, and 11 fully complete), delete `archive/python/` and this section.
- [ ] Verify all Python script capabilities are covered in C# (per-character session logs, DM sessions, Markdown export, CSV export, PDF export, MSC export)
- [ ] Delete `archive/python/`
- [ ] Remove archive references from README.md and CLAUDE.md

---

## Legend
- [x] Completed
- [ ] Pending
