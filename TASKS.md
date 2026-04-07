# Adventurers League Log Downloader — Task List

The goal is for the C# WPF app to absorb all capabilities of the Python scripts, becoming the single distributable for non-programmer users. Python scripts remain as a reference/fallback during the transition.

Checked items are completed. Unchecked are pending.

---

## 1) Project setup and foundations (C#)
- [x] Create WPF project targeting `net10.0-windows` and set up namespaces
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
- [x] **Session log workbook CSV** — **File → Export session log workbook (CSV)…** → `session_log_workbook.csv` (merged view of downloaded `character_*.csv`). Reference layout: [Ken DDAL Log (Google Sheet)](https://docs.google.com/spreadsheets/d/1bqbClFX-MMgIWDKbnEEmmxBvwzO6wYSXm_ig690kojA/edit?usp=sharing); mapping and future ideas: `docs/examples/spreadsheet-ken-ddal-log.md`
- [ ] **Moonsea Codex (MSC) integration** — *FIRST EXPORT TARGET*
  - The allog CSV format is already what MSC's importer expects — no transformation needed
  - Per-character: button/menu item opens the character's CSV folder in Explorer + opens the MSC import page in the default browser
  - User drags the CSV onto the MSC page themselves — no MSC credentials stored in the app
  - Depends on per-character CSVs being downloaded (section 5)
- [ ] Characters Markdown export — single file (`json_to_markdown.py` → C#)
  - Reference template: `docs/examples/markdown-export-notion-template.md` (richer format, preferred over Python version)
  - Use **Scriban** for templating (`Scriban` NuGet package)
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

## 7b) Shared Core, console host (CLI + TUI), and WPF *(cross-platform path)*

**Intent:** Keep the Windows **WPF** app as the primary “full GUI” experience. Add a **`net10.0` console** program that ships on **Windows, macOS, and Linux** and shares all site logic with WPF via a **`net10.0` class library**. The console host combines:

- **CLI** — subcommands and flags for scripting, automation, and advanced users; suitable for CI and `cron`; exits non-zero on failure; no prompts when required arguments are supplied.
- **TUI** — an interactive, full-terminal guided flow (menus, forms, progress) for users who are fine in a terminal but want *discovery* and feedback without memorizing flags. Useful on **Windows** (Windows Terminal, etc.) as well as Unix.

**Target solution layout**

| Project | TFM | Role |
|--------|-----|------|
| `Allog.Core` *(name TBD)* | `net10.0` | Auth, scrapers, settings models/paths, file I/O contracts; `IProgress<T>` + `CancellationToken`; **no** WPF/WinForms/Console UI packages |
| `Adventure League Log Downloader` | `net10.0-windows` | WPF + Windows credential store + folder picker; references Core only for site logic |
| `Allog.Console` *(name TBD)* | `net10.0` | References Core; hosts **System.CommandLine** (or equivalent) root command with subcommands, e.g. `download`, `auth-test`, … and **`tui`** (or make `tui` the default when no subcommand is given — decide at implementation time) |

**TUI implementation note:** Start with **[Spectre.Console](https://spectreconsole.net/)** — prompts, tables, live progress, and layouts are quick to align with existing WPF flows and behave well across Windows Terminal and Unix terminals. If we later need classic multi-pane terminal UI (separate windows, mouse regions), evaluate **[Terminal.Gui](https://github.com/gui-cs/Terminal.Gui)** as a second phase or for specific screens only.

**Roadmap order (hosts)**  
1) **Headless CLI** first — cross-platform, scripting, minimal UX surface.  
2) **TUI** second — same `Allog.Console` binary, interactive mode when Core + CLI patterns are stable.  
3) **New product features** (including exports, session logs, MSC, Markdown, etc.) can land **mostly in Core** as they are implemented; **WPF can gain those features between TUI and any full GUI rework** so Windows users are not blocked.  
4) **Full GUI rework** (cross-platform replacement for WPF) **last** — only after Core stabilizes and hosts are proven.

**Credential / config on console (CLI + later TUI)**  
Priority: **environment variables** and **explicit CLI arguments** for non-interactive use. **Optional persistence:** settings under app data / home (same family as `settings.json`); password stored only as **obfuscation** for convenience — **not** encryption; user copy must say **“obfuscated password,”** never **“encrypted.”** Users who want no local secret leave **“store credentials”** unchecked. See **`SECURITY.md`** for the full decision record, threat model, and WPF vs portable notes. WPF may keep **Windows Credential Manager** for now; **no** Keychain / Secret Service in the first cross-platform wave.

**Exports vs CLI/TUI timing**  
Do **not** block the first CLI on “all exports complete.” Ship CLI verbs for whatever Core already does well (e.g. auth + character list → JSON). Add **new CLI/TUI commands as export pipelines move into Core** (Markdown, zip, MSC helpers, etc.). Several heavier exports depend on **per-character session data** (sections 5–6 above); sequencing those in Core naturally extends the same façade the CLI and TUI call, with little throwaway work.

**Distribution:** Publish **`Allog.Console`** self-contained per RID (`win-x64`, `osx-arm64`, `osx-x64`, `linux-x64`). WPF publish profile stays **Windows-only**.

### Checklist — phased

**Phase A — Core extraction (no user-visible change on Windows WPF)**
- [ ] Add `Allog.Core` project; move `Services/*` and non-UI models; ensure zero references to `System.Windows`, WinForms, or Console-specific types in Core
- [ ] Define small **host-facing façade** (e.g. use-case methods or `IAllogApp` / orchestrator) that WPF, CLI, and TUI call so behavior stays single-sourced
- [ ] Wire WPF project to reference Core; trim duplicated logic from code-behind incrementally
- [ ] Windows-only implementations (`ICredentialStore`, etc.) remain in WPF assembly or a `net10.0-windows` companion project referenced only by WPF

**Phase B — Headless CLI**
- [ ] Add `Allog.Console`; root parser with `--help` and subcommands mirroring **initial** Core capabilities (authenticate, scrape characters, output paths, delay); extend verbs as new Core features ship
- [ ] Credentials: **env vars** + **flags**; optional persisted settings including **obfuscated** password per **`SECURITY.md`** (documented; restrictive permissions on Unix where applicable)
- [ ] Document environment variables, config file location, and exit codes for scripting
- [ ] CI job: build and test Core + Console on Linux (and optionally macOS) so regressions surface early

**Phase C — TUI**
- [ ] Implement `… tui` (or default interactive mode): main menu mapping to WPF concepts — account, output location, options/delay, run download, open output folder (`Process.Start` / `xdg-open` / `open` by OS)
- [ ] Shared progress reporting via `IProgress<string>` or structured status DTOs so CLI (optional `--verbose`) and TUI stay aligned
- [ ] Keyboard-first navigation; respect terminal size; clear error panels

**Phase D — Polish (console)**
- [ ] Single downloadable per OS for console + version string parity with WPF
- [ ] README section: when to use WPF vs TUI vs headless CLI; security notes for env and config file

**Phase E — Full GUI rework** *(last)*  
- [ ] Replace or supplement WPF with a cross-platform desktop UI **after** Core and console hosts are stable; new features should already live in Core where possible

---

## 8) Packaging and distribution
- [x] Versioning and build metadata
- [x] Single-file distribution profile (win-x64)
- [ ] First-run experience (explain credentials, delay, output defaults)
- [ ] MSIX installer packaging

---

## 9) Documentation
- [ ] Expand README with setup, usage, screenshots, and troubleshooting
- [x] SECURITY.md: credential storage approach and limitations *(initial decisions recorded; update when implementation lands)*

---

## 10) Stretch goals / future
- [ ] **Headless CLI + interactive TUI** — detailed checklist under **7b)** (same `Allog.Console` host)
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
