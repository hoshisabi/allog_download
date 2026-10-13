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
- [ ] Non-blocking runs (async with UI responsiveness; full-window responsiveness) — *partial: main work area + Options/File export disabled during Download / CSV-only export / session workbook export*
- [x] Progress bar + Cancel button during long-running operations *(main window: roster download, bulk CSV, selected CSV export workbook; cancel via `CancellationToken` where supported)*
- [ ] Error surface improvements (step-specific errors; link to logs)
- [ ] Validate all fields with inline hints; preserve focus/selection on errors — *partial: data location validation hint text under “Last downloaded…” (no modal for missing folder/file name)*
- [ ] Keyboard navigation/tab order; access keys on menu and buttons
- [x] About dialog: version/build info; link to repo/issue tracker

---

## 3) Auth and session
- [x] Implement `IAdventurersLeagueAuth` and `AdventurersLeagueAuth` with cookie/CSRF handling
- [x] Verify login and discover `userId` from returned HTML
- [x] Wire Run to authenticate using supplied credentials
- [x] "Forget credentials" action in menu; explanatory tooltip/help text *(Options → Forget stored credentials)*
- [ ] Cross-platform credential store — see **7b** A7 (Credential Manager / Keychain / Secret Service, with headless fallbacks)

---

## 4) Character list scraping
- [x] Port character scraping to C# `CharacterScraper`
- [x] Robust pagination detection (scan all `page=` links; fallback probing)
- [x] Table parsing resilient to missing/extra columns; only ID mandatory
- [x] Save JSON output keyed by character id with camelCase properties
- [ ] Per-page/total row counts shown in UI during run; final summary in status bar
- [ ] Option to open output file/folder after save — *partial: **Options → Open Files in Explorer** selects the characters JSON on demand; nothing opens automatically after a save*
- [x] Unit tests with small HTML fixtures: pagination discovery, row parsing edge cases (`Adventure League Log Downloader.Tests`)
- [x] **Unit tests for export and persistence logic**
  - [x] `SessionLogWorkbookCsvExporter` — given a set of character CSV fixtures, assert row count, column mapping, skip behavior for missing CSVs
  - [x] `CharacterCsvDetailReader` / `CharacterLogCsvReader` — round-trip parse of known CSV shapes
  - [x] `SettingsService` — load/save round-trip; missing file returns defaults; corrupt file does not throw

---

## 4b) Scraping resilience *(high priority, not immediate)*

The site's HTML structure is the only interface this tool has. When it changes, the app silently produces bad data or crashes — and if scraping breaks entirely, the tool loses its core purpose. This is a chronic maintenance risk worth actively managing.

- [ ] **Sanity checks with user-visible warnings** — after a scrape, if the character count is 0 or implausibly low relative to the previous run, surface an explicit warning ("Expected characters but found none — the site may have changed") rather than silently saving an empty list
- [ ] **Structured parse diagnostics** — when a character row is missing expected fields, log which fields were absent rather than swallowing the gap silently; expose this in a Help → Show log or similar
- [ ] **Fixture-based regression snapshots** — periodically capture a real (anonymized) page of HTML from the site and add it to the test fixtures; if the parser output changes, a test fails before users notice
- [ ] **Long-term: local data independence** — per-user local data copies (downloaded CSVs) already partially decouple from the site; the goal of making the tool useful without live scraping should inform which features get built into Core first (export, workbook, MSC) so users who have already synced are not blocked when the site changes

---

## 5) Data: downloading and parsing (port from Python)
- [ ] **Per-character session log downloads** (detailed pages per character) — *needed for full MSC/Markdown export (gold, downtime, magic items)*
- [x] DM session list scraping (`dmsession_list.py` → C#) — `DmSessionScraper` + **DM Sessions…** window → `dm_sessions.json`
- [ ] CSV ingest parity where Python scripts relied on downloaded CSVs as input

---

## 6) Data export formats (port from Python)
- [x] Characters JSON export
- [x] **Session log workbook CSV** — **File → Export session log workbook (CSV)…** → `session_log_workbook.csv` (merged view of downloaded `character_*.csv`). Reference layout: [Ken DDAL Log (Google Sheet)](https://docs.google.com/spreadsheets/d/1bqbClFX-MMgIWDKbnEEmmxBvwzO6wYSXm_ig690kojA/edit?usp=sharing); mapping and future ideas: `docs/examples/spreadsheet-ken-ddal-log.md`
- [x] **Moonsea Codex (MSC) integration** — *FIRST EXPORT TARGET*
  - Per-character: **Import to Moonsea Codex…** in the character detail window (double-click a row) writes `moonseacodeximport.csv` (or ` (N)`) to Downloads, selects it in Explorer + opens `https://moonseacodex.com/characters` in the default browser (`MoonseaCodexHandoff`)
  - User clicks **Import** on the MSC characters page and drops in the file themselves — no MSC credentials stored in the app
  - MSC's importer (`MoonseaCodex/API` `codex/imports/csv.py`) splits on commas with no quote handling, so the site CSV is converted (`MoonseaCodexCsvWriter`): header lines verbatim, fields flattened. Before this, rows with multi-line notes were dropped (4 of 15 sessions on one real character)
  - Imported character visibility: not public by default (MSC treats any non-empty `publicly_visible` as public; the flag only affects MSC's Discord bot listings)
  - Each MSC import creates a new character — re-importing the same CSV makes a duplicate
- [ ] MSC import visibility as a user choice — setting in Options and/or a checkbox next to the button (currently `MoonseaCodexHandoff.DefaultMakePublic = false`); wording should say it only affects MSC's Discord listings
- [ ] MSC import level: MSC sets level from `class_and_levels` text, which is often stale (`Wizard-1` on a level-15 character; 57 of 65 real characters have no level in it). Rewrite single-class text with the site level from `characters.json` (e.g. `Wizard 15`); leave multiclass text unless its levels already sum to the site level. Extend `MoonseaCodexImportEmulator` with MSC's class parsing so tests check the level
- [ ] Draft an upstream issue for `MoonseaCodex/API` (for Dan to review and post): parse uploads with a CSV parser instead of `splitlines()` + `split(",")`; `bool("false")` is true for `publicly_visible`; `(barbarian)(bard)` missing `|` in `parse_classes`
  - Button is enabled only when the character's CSV has been downloaded
- [ ] Characters Markdown export — single file (`json_to_markdown.py` → C#)
  - Reference template: `docs/examples/markdown-export-notion-template.md` (richer format, preferred over Python version)
  - Use **Scriban** for templating (`Scriban` NuGet package)
- [ ] Characters Markdown export — per-character files, optional zip
- [ ] Characters CSV export with zip (`json_to_csv_zip.py` → C#)
- [ ] DM sessions JSON/CSV export — *partial: JSON (`dm_sessions.json`) written by the DM Sessions window; CSV pending*
- [ ] Per-character session JSON/CSV export
- [ ] Option: append timestamp to filenames

---

## 7) Architecture and quality
- [ ] Introduce light MVVM (ViewModels for windows/dialogs; keep services UI-agnostic)
- [ ] Dependency injection for services to ease testing (auth, scraper, settings, credential store)
- [ ] Unit tests with mocked HTTP for full auth/scraper flows *(HTML parsing / CSRF / user-id helpers covered in test project)*
- [ ] File-based rolling log; user-openable from Help menu

---

## 7b) Shared Core, CLI, and cross-platform GUI *(cross-platform path)*

**Decision (2026-10-13):** Linux and macOS users get a native GUI. At least one current user is on Linux. Wine works in the meantime (on blueglow, 2026-10-13: the test exe passes 57/57 under Wine 10 and the WPF main window draws under Xvfb), so this isn't urgent. The design is a **Core library** that a **CLI** and an **Avalonia GUI** both call. Site logic lives in one place and every host is a thin shell over it.

**Target solution layout**

| Project | TFM | Role |
|--------|-----|------|
| `Allog.Core` *(name TBD)* | `net10.0` | Auth, scrapers, CSV/JSON/MSC/workbook export, settings, use-case façade; `IProgress<T>` + `CancellationToken`; **no** UI packages and **no** Windows APIs |
| `Allog.Cli` *(name TBD)* | `net10.0` | **System.CommandLine** verbs over the Core façade; scripting, `cron`, non-zero exit on failure, no prompts when arguments are supplied |
| `Allog.Desktop` *(name TBD)* | `net10.0` | **Avalonia** GUI over the Core façade; Windows, macOS, Linux |
| `Adventure League Log Downloader` | `net10.0-windows` | Existing WPF app; moves onto Core during Phase A; retirement decided in Phase C |
| `Adventure League Log Downloader.Tests` | `net10.0` | Tests Core only, so `dotnet test` runs natively on Linux and macOS |

**OS-specific code** sits behind two interfaces, each with one implementation per OS:
- `ICredentialStore` *(exists)* — per-OS implementations in Core; approach pending the A7b decision below (`Devlooped.CredentialManager` needs Git installed, so it's out as packaged). Replaces `CredentialManagement` (a .NET Framework package; source of the NU1701 warning; on plain `net10.0` it fails to load without `System.Security.Permissions`).
- `IPlatformShell` *(new)* — `GetDownloadsFolder` (Windows: `SHGetKnownFolderPath`; Linux: `xdg-user-dir DOWNLOAD`, falling back to `~/Downloads`; macOS: `~/Downloads`), `RevealFile` (Windows: `explorer /select,`; macOS: `open -R`; Linux: `xdg-open` on the containing folder), `OpenUrl` (shell execute / `open` / `xdg-open`).

Windows-only code as of 2026-10-13 (after A1): `Allog.Core/Services/WindowsCredentialStore.cs` (behind `ICredentialStore`) and `Allog.Core/Services/WindowsPlatformShell.cs` (behind `IPlatformShell`, A1). No other Windows APIs or `explorer.exe` calls remain in Core or the WPF code-behind.

**Credential posture (2026-10-13):** the site password is low importance. The aim is to give users options: OS secure storage where it exists, plainer fallbacks where it doesn't, and env vars / flags for scripts. If a more secure option comes along later, add it. User-facing text describes each option as it is (a `plaintext` store is not "encrypted").

**Credential lookup for CLI**, in order: (1) `--password` / `--password-file` / environment variable; (2) the configured store; (3) exit non-zero with a message listing the options. `allog login` saves to the configured store ("log in once").

Headless Linux options:
- `gpg` store *(if added; see A7b)* — encrypted with the user's GPG key; `gpg-agent` asks for the passphrase once and caches it per `default-cache-ttl` / `max-cache-ttl`. Log in once per session, over SSH
- `plaintext` store — file readable only by the user; unattended cron
- `systemd-creds` (outside GCM) — encrypted at rest with a machine-bound key; a systemd timer passes it to the job via `$CREDENTIALS_DIRECTORY`, read with `--password-file`

Anything readable by an unattended job is also readable by other processes running as that user; the headless options trade convenience against that.

**Data location on macOS / Linux (decided 2026-10-13):** keep `~/.config/AllogDownloader/` for settings, saved login, and downloaded data, matching Windows (`%AppData%\AllogDownloader`). The XDG spec would put downloaded data in `~/.local/share/AllogDownloader/`; revisit if a user asks. Users can set a different data location (`--output`, or Options → Data location in the GUI).

**Order:** Core (A) → CLI (B) → Avalonia GUI (C) → packaging (D). New features can land in Core as soon as A2 is done; WPF picks them up until Avalonia replaces it. A TUI (Spectre.Console) stays optional (E).

### Checklist — phased *(one task = one commit; each one ends with a passing build and tests)*

**Phase A — Core extraction (no user-visible change in WPF)**
- [x] A1. Add `IPlatformShell` + `WindowsPlatformShell` in the current project. Route `MoonseaCodexHandoff`, `DownloadsFolder`, `MainWindow.xaml.cs:279-308`, and `AboutDialog.xaml.cs:66` through it. Update `MoonseaCodexHandoffTests`. Verify on Windows: Open Files in Explorer, Import to Moonsea Codex, About link *Done 2026-10-13: `IPlatformShell` (`GetDownloadsFolder`, `RevealFile`, `OpenFolder`, `OpenUrl`), `WindowsPlatformShell`, and `PlatformShell.ForCurrentOS()` in Core (not the WPF project, so A6 can add the Unix shell beside it); `ForCurrentOS` throws `PlatformNotSupportedException` off Windows until A6. `DownloadsFolder` folded into `WindowsPlatformShell`. `MoonseaCodexHandoff.Open` takes the shell, and its test uses a recording fake. Behavior unchanged: same `explorer.exe /select,` and shell-execute calls. Linux 84 passed / 9 skipped; win-x64 under Wine 90 passed / 3 skipped; app starts under Wine. Checked on Windows (spectrum, 2026-10-13, Debug build of `linux-cli`): Open Files in Explorer selects `characters.json` when present and opens the data folder when it isn't; Open Settings Folder opens `%AppData%\AllogDownloader`; Import to Moonsea Codex writes `moonseacodeximport.csv` to Downloads, selects it in Explorer, and opens `https://moonseacodex.com/characters`; the About repo link opens in the browser*
- [x] A2. Add `Allog.Core` (`net10.0`, no RuntimeIdentifier). Move `Services/*`. Keep the namespace `Adventure_League_Log_Downloader.Services` for a pure-move diff. WPF references Core; `InternalsVisibleTo` moves to Core. Windows build + tests pass. *Done 2026-10-13: all of `Services/` moved, including the Windows-specific files, because their tests move to Core-only in A3 and they compile on `net10.0`; `WindowsCredentialStore` is `[SupportedOSPlatform("windows")]`, and A1 still routes `DownloadsFolder` / `MoonseaCodexHandoff` through `IPlatformShell`. `CharacterDetailWindow.xaml` maps `services:` with `assembly=Allog.Core`. Verified on blueglow: build, 64/64 tests under Wine, self-contained single-file publish starts under Wine. Checked on Windows (spectrum, 2026-10-13, `linux-cli`): build, tests (161 passed / 9 skipped), and the character detail window (opened for Import to Moonsea Codex)*
- [x] A3. Retarget the test project to `net10.0` with no RuntimeIdentifier, referencing Core only. Verify `dotnet test --project "Adventure League Log Downloader.Tests"` passes natively on Linux (blueglow). *Done 2026-10-13: natively on blueglow 57 pass + 7 `WindowsCredentialStoreTests` skipped (xunit v3 `SkipUnless`); the same tests built for `win-x64` pass 64/64 under Wine. `dotnet test` no longer builds the WPF app — `CLAUDE.md` / `README.md` now say to `dotnet build` the solution first*
- [x] A4. CI: add an `ubuntu-latest` job that builds Core + tests and runs `dotnet test`. Also run tests in the Windows job (as of 2026-10-13 it builds/publishes but doesn't run tests). *Done 2026-10-13: `test-linux` job + a Test step in the Windows `build` job (`.github/workflows/dotnet-desktop.yml`). Manual run on `core-extraction` (run 38354523615): Linux 57 passed / 7 skipped; Windows 64 passed / 0 skipped*
- [ ] A5. Core façade, one use case per commit, each moved out of `MainWindow.xaml.cs` code-behind and covered by tests with mocked HTTP:
  - [x] A5a. Authenticate + character list → `characters.json` *Done 2026-10-13 together with A5b (one pipeline): `CharacterListDownload.RunAsync(auth, options, progress, ct)` in Core — scrape, CSVs per `CharacterCsvMode` (Skip / OnlyMissing / All), last-session fill-in, save. `CharacterListDownloadResult.CsvSummary` holds the summary sentence both hosts show. WPF `OnDownloadClick` keeps its UI work (status text, settings save, message box) and calls it. 9 tests against an in-memory fake site (pages + CSVs over a fake `HttpMessageHandler`); removing the previous-list merge fails a test. Linux 117 passed / 10 skipped. Checked on Windows (spectrum, 2026-10-13, `linux-cli`): a real Download with each CSV option (list only, only missing, all) shows the same status text, progress bar and success message as before*
  - [x] A5b. Per-character CSV download (only-if-missing option, delay, cancel) *Done in A5a*
  - [ ] A5c. Session log workbook export
  - [ ] A5d. DM sessions → `dm_sessions.json` (from `DmSessionWindow.xaml.cs`)
  - [ ] A5e. Additional site data (from `AdditionalDataWindow.xaml.cs`)
  - [x] A5f. MSC handoff (already in Core after A2; confirm it goes through `IPlatformShell`) *Done in A1*
- [x] A6. `UnixPlatformShell` (Linux + macOS) in Core, with tests for path resolution and the command lines it builds. Return it from `PlatformShell.ForCurrentOS()`. `RevealFile`: Linux `org.freedesktop.FileManager1.ShowItems` over D-Bus, falling back to opening the folder; macOS `open -R`. `GetDownloadsFolder`: Linux `XDG_DOWNLOAD_DIR` from `~/.config/user-dirs.dirs`, else `~/Downloads`; macOS `~/Downloads`. `OpenFolder` / `OpenUrl`: shell execute (.NET runs `xdg-open` / `open`) *Done 2026-10-13: `UnixPlatformShell`; command builders and the user-dirs parser are static, process runners injectable, so tests launch nothing. `RevealFile` on Linux calls `dbus-send` with a 3 s reply timeout and opens the containing folder if it exits non-zero or isn't installed. Found on blueglow: `org.freedesktop.FileManager1` is D-Bus-activatable (Nautilus), so the call starts the file manager when needed — and with no display it times out and leaves a background `nautilus --gapplication-service`. Verified on blueglow: `GetDownloadsFolder` matches `xdg-user-dir DOWNLOAD`. Not yet verified: reveal/open on a Linux desktop session; anything on macOS*
- [x] A7a. Spike `Devlooped.CredentialManager` (2026-10-13, blueglow, scratch projects in `/tmp`, nothing committed). Results:
  - **Blocker: requires Git.** `CredentialManager.Create` throws `Failed to locate 'git' executable on the path` on Windows (under Wine) and Linux when Git isn't installed; GCM reads its settings through `git config`, and the only override is `GIT_EXEC_PATH`. Most Windows users won't have Git
  - License: source is MIT; the NuGet binary is under the Open Source Maintenance Fee EULA (fee applies only to revenue-generating users; self-compiling from source is exempt). Bundled analyzer DLL contains only compiler-attribute polyfills
  - Linux has **no default store**: with nothing configured it throws `No credential store has been selected`. The app must choose one (`GCM_CREDENTIAL_STORE`)
  - `secretservice` with no display: fails fast (`requires graphical interface`). Not tested with a display and no running keyring, because D-Bus would start `gnome-keyring-daemon` against the real login keyring; desktop-session test (2) not run either (no desktop login on blueglow)
  - `plaintext`: works; site password + Warhorn token coexist; read/remove work. **File written mode 664** (group/other readable), password on line 1 — the app must set `0600` / `0700` itself
  - `cache`: works (uses `git credential-cache` daemon, so also needs Git)
  - `gpg`: requires `GPG_TTY` set even to encrypt (`GPG_TTY=/dev/null` is accepted). With the passphrase cached in `gpg-agent`, a no-tty (cron-style) read works. With nothing cached it fails in ~2 s with gpg's `Screen or window too small` — no hang. Files mode 664
  - Windows migration (under Wine, plus GCM source): GCM enumerates all credentials and matches `namespace:service` exactly, so `Create("AllogDownloader").Get("AdventurersLeague", user)` matches the existing entry. Blob encoding is UTF-16 in GCM; not confirmed for `CredentialManagement` because the Wine run stopped at the Git check (later confirmed UTF-16 in A7b-1)
- [ ] A7b. Own thin credential stores *(option 1 from A7a, decided 2026-10-13)*. Each store keeps the `ICredentialStore` interface, takes its entry name in the constructor (so the Warhorn token in section 11 can be a second entry), and throws on real failures (callers already catch); deleting a missing entry is not an error
  - [x] A7b-1. Windows: `CredRead` / `CredWrite` / `CredDelete` P/Invoke in `WindowsCredentialStore`; same target name (`AllogDownloader:AdventurersLeague`) and blob format as `CredentialManagement`, so saved logins carry over; remove the `CredentialManagement` package. Round-trip tests (pass on Windows and under Wine); one-off check under Wine that an entry written by the old package reads back. *Done 2026-10-13: 64/64 tests under Wine; old package → new store and new store → old package both read back the same UTF-16 password. Real Windows (spectrum, 2026-10-13, Release build with Core): the existing saved login loads through the new store; in the GUI, remember + restart loads it, Forget + restart loads nothing, and the character detail window shows the session grid. On `linux-cli` (spectrum, 2026-10-13, Debug), with the store coming from the A7b-5 factory, the saved login still loads at startup*
  - [x] A7b-2. Headless file store: one file per entry under the settings folder, `0600` file / `0700` folder on Unix; user-facing text calls it "saved in a file", never "encrypted". Needs A2/A3 so its tests run natively on Linux. *Done 2026-10-13: `FileCredentialStore` + `PasswordObfuscation` in `Allog.Core/Services`. File is `credentials/<entry>.json` beside `settings.json` with `username` + `obfuscatedPassword` (`v1:` + Base64 of the UTF-8 password XORed with a fixed key, per `SECURITY.md`; obfuscation, not encryption). Own `credentials/` subfolder rather than inside `settings.json`, because the settings folder is created with default permissions and only a subfolder can be `0700` without changing it — record in A7c. Written to a temp file created `0600`, then renamed, so the password is never on disk with looser permissions; an existing loose folder is tightened. Unreadable file → `Load` returns null. Not wired into any host yet (A7b-5). Tests: 23 new; Linux 80 passed / 7 skipped (Windows store); win-x64 under Wine 85 passed / 2 skipped (Unix modes). Removing either permission safeguard fails a test*
  - [ ] A7b-3. Linux: libsecret `secret_password_store_sync` / `lookup_sync` / `clear_sync` P/Invoke; falls back to A7b-2 when Secret Service isn't available (no display, no `libsecret-1.so.0`, or the call fails). Needs A2/A3. Verify on a Linux desktop session
  - [ ] A7b-4. *(Deferred — no Mac to test on)* macOS Keychain via `SecKeychain*GenericPassword` P/Invoke. Until then macOS uses A7b-2
  - [x] A7b-5. Store selection by OS in one place (factory in Core); gpg store only if someone asks *Done 2026-10-13: `CredentialStores.ForCurrentOS(settingsFolder | ISettingsService, entryName)` — Windows: `WindowsCredentialStore` with target `AllogDownloader:<entry>` (site login keeps `AllogDownloader:AdventurersLeague`); elsewhere `FileCredentialStore` until A7b-3/A7b-4. `ICredentialStore.Description` gives the SECURITY.md wording for "saved in ..." messages. WPF `MainWindow` uses the factory (same store on Windows). Linux 108 passed / 10 skipped; win-x64 under Wine 109 passed / 9 skipped; app starts under Wine*
- [x] A7c. Update `SECURITY.md` for the posture above (OS stores in the first wave; headless options; honest wording per store) and the user-visible strings that describe storage. *Done 2026-10-13: `SECURITY.md` rewritten — posture, stores by platform, file store details, obfuscation format, unattended-use note, wording table. WPF strings unchanged (they name Windows Credential Manager, which WPF uses); hosts that can fall back to the file store pick wording from the store in use*

**Phase B — CLI**
- [x] B1. Add `Allog.Cli` with System.CommandLine; `characters` verb (A5a); credentials from `--password` / `--password-file` / env var; exit codes. Runs on Linux *Done 2026-10-13: `Allog.Cli` (executable `allog`, System.CommandLine 2.0.13) with `characters`: `--output`, `--csv skip|missing|all`, `--delay`, `--username`, `--password`, `--password-file`, `--quiet`; defaults from settings.json like the desktop app; login order flag → file → env → saved login (`CliCredentials`); progress on stderr, summary on stdout; exit codes 0/1/2/3/130. Also fixed: on Linux, `SpecialFolder.ApplicationData` is empty when `~/.config` doesn't exist, so settings and the file store landed in the current folder; `SettingsService` / `UserSettings.DefaultDataFolder` now pass `SpecialFolderOption.Create`. 23 tests (credential order, and the command end to end against the fake site). Self-contained linux-x64 single file (74 MB) runs with an empty HOME and no .NET installed. Real site, 2026-10-13 on blueglow: 4 pages, 65 characters, 65 CSVs, all 65 with a last-session date, 26 s*
- [ ] B2. `csv` verb (A5b)
- [ ] B3. `workbook` and `dm-sessions` verbs (A5c, A5d)
- [x] B4. `allog login` / `allog logout` over the configured store (A7b), then the lookup order above. README section: env vars, `--password-file`, store choice for headless Linux (`gpg` / `plaintext` / `systemd-creds` timer example), config location, exit codes *Partly done 2026-10-13: `allog login` (checks the login on the site, prompts for anything not given, hidden password input, piped input works) / `allog logout`; `--env-file PATH` on `characters` and `login`, winning over the same environment variables and warning about unused `ALLOG_` keys; progress updates one line in place on a terminal. Real site on blueglow: login → characters with the saved login → logout. README section added the same day. Windows (spectrum, 2026-10-13): self-contained win-x64 single file; `characters --csv skip` and `characters` (only missing) both sign in from the saved login in Windows Credential Manager and save 65 characters from 4 pages; the default run found all 65 CSVs present and downloaded none; in a terminal the progress updates one line in place*

**Phase C — Avalonia GUI**
- [ ] C1. Add `Allog.Desktop` (Avalonia, `net10.0`): an empty main window launches on Linux and Windows. Add it to the Linux CI build
- [ ] C2. Main window: character grid, Download, progress/cancel, status bar (A5a, A5b)
- [ ] C3. Account dialog over `ICredentialStore` (A7b)
- [ ] C4. Options + Data location dialogs (folder picker via Avalonia `StorageProvider`)
- [ ] C5. Character detail window + Import to Moonsea Codex
- [ ] C6. DM Sessions window
- [ ] C7. Additional data window
- [ ] C8. About dialog; File menu exports
- [ ] C9. Parity check against WPF on Windows and Linux; record gaps here
- [ ] C10. Retire WPF; Avalonia becomes the Windows app too *(decided 2026-10-13, conditional on C9: keep WPF for Windows only if C9 finds a blocker, e.g. DataGrid behavior or screen-reader support)*

**Phase D — Packaging (cross-platform)**
- [ ] D1. Self-contained single-file publish per RID: `win-x64`, `linux-x64`, `osx-arm64`, `osx-x64`; attach to GitHub releases
- [ ] D2. Linux: decide tarball vs AppImage; `.desktop` file + icon
- [ ] D3. macOS: `.app` bundle. Unsigned apps are blocked by Gatekeeper (user must right-click → Open, or run `xattr -d com.apple.quarantine`); signing + notarization needs a paid Apple Developer account. Document the workaround until then
- [ ] D4. Version string parity across CLI and GUI; README section on which download to pick

**Phase E — TUI** *(optional)*
- [ ] Spectre.Console interactive mode in `Allog.Cli` over the same façade, if CLI users ask for it

**Phase C notes — Avalonia port estimate (2025-05-05, revisit after Phase A)**

Preferred framework: **Avalonia UI** over MAUI — desktop-first, better macOS stability, no mobile overhead. CORS is not an issue since it runs native.

- **UI rewrite:** 9 windows/dialogs; none are complex. Avalonia data binding is close to WPF's. XAML is rewritten from scratch; layout fidelity is not a goal.
- **Wildcards:** first-time Avalonia project setup; DataGrid differences from WPF; WPF-specific APIs in code-behind (`Dispatcher`, `DependencyProperty`, etc.).
- Doing Phase A first matters: `MainWindow.xaml.cs` (~900 lines) wires auth, scraping, CSV, and JSON directly in code-behind. Porting it before extraction means porting that logic twice.

Migration impact on existing users (all Windows as of 2025-05-05):
- Settings (`%AppData%\AllogDownloader\settings.json`) — survive as-is; `SpecialFolder.ApplicationData` resolves identically. On Linux this is `~/.config/AllogDownloader/`
- Downloaded CSVs/JSON — unaffected (user-chosen paths).
- Saved credentials — stay in Windows Credential Manager under the same target name (A7b option 1 reads it directly).

---

## 8) Packaging and distribution
- [x] Versioning and build metadata
- [x] Single-file distribution profile (win-x64)
- [x] Raw `.exe` attached to GitHub releases (self-contained; no unzipping required)
- [ ] First-run experience (explain credentials, delay, output defaults)
- [ ] MSIX installer packaging
- [ ] Code signing certificate — SmartScreen will warn on unsigned binaries regardless of packaging format; a cert is a prerequisite for a smooth installer experience
- [ ] **Update notification** *(medium priority)* — on startup, check the GitHub releases API for a newer version and surface a non-blocking hint in the status bar or About dialog; no auto-install, just awareness

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

### Deprioritized / icebox
- [ ] Migrate to official API if/when available (switchable backend) — *deprioritized; site scraping remains the plan*

---

## 11) AL Service Rewards tracker (ALSR v2026.1)

Design, rules digest, and decision record: **`docs/service-rewards-tracker.md`**. V1 is redemption tracking (the manual file `hoshisabi.github.io/rpg/al/service_rewards.md` shows that's where tracking died — hours were never the constraint); the Warhorn-fed hours ledger is v2. All services are UI-free C# in `Services/` (Core-ready per §7b). Tasks are ordered one task → one test → one commit.

**v1 — Redemption tracking**
- [ ] Reward-menu data format: per-ALSR-version JSON (id, date range, sections with cost + slots — fixed items with `SourceUrl` vs open "campaign-available" slots — repeatables, unique/steeping double-cost, write-in claim limits) + loader (test: schema round-trip on a small fixture)
- [ ] Encode the ALSR 2026.1 menu (Rewards by Rarity, Repeatable, Adventure Rewards) as embedded data (test: loads, slot counts match the document)
- [ ] `ServiceRedemption` model + persistence in `service_rewards.json`: slot ref, `DisplayName`, `FunctionsAs`, `SourceUrl`, flavor, `ItemAssignedTo`, `LevelAssignedTo`, `SecondaryRewardChoice` (DT|GP + amount), `AdventureCode`, `HoursSpent`, date (test: round-trip; missing file → empty; corrupt file does not throw)
- [ ] Historical-file parser: `- [x] Name + secondary (I:x/L:y)` lines incl. `[X]`, `(I?/L?)`, `(IL:x)`, bare `(Name)`, write-ins with `-Nhr` (test: fixture excerpts from the real file across 11A/12B/50thA formats)
- [ ] Encode legacy version menus (11A, 11B, 12A, 12B, 12C, 50thA, 50th-1.2, 2.1) as data, driven by what the parser finds (test: every parsed redemption maps to a menu slot)
- [ ] One-time import command wiring parser → redemption store; unknown assignments land as unassigned (test: import is idempotent)
- [ ] `ServiceRewardsWindow` v1: version picker, menu view with checkbox slots, slots-used totals, following `DmSessionWindow` patterns
- [ ] Claim/edit dialog capturing both assignments + secondary choice; unassigned-resolution list view (the `I?/L?` backlog)
- [ ] Publish-back exporter: regenerate the GitHub Pages markdown from tracker data (test: golden file — import the real file, export, diff is limited to normalization)
- [ ] README + CLAUDE.md: document the feature

**v2 — Rough hours import + nudge** *(after v1 verified against the real history file; philosophy: VERY rough is fine — the product is the reminder, not the accounting)*
- [ ] `ServiceLogEntry` model + `ServiceType` enum in the same JSON store (test: round-trip)
- [ ] `WarhornClient`: `HttpClient` POST with token auth, `FetchEventSessions(slug)` with pagination, DTOs (test: canned JSON fixture → DTOs; HTTP error surfaces cleanly)
- [ ] Warhorn token storage via `ICredentialStore` + `WARHORN_APPLICATION_TOKEN` env override; `UserSettings.WarhornEventSlugs` + `WarhornDisplayName` + `DefaultSessionHours` (test: settings round-trip)
- [ ] Session → ledger mapping: filter to own GM signups, hours = `DefaultSessionHours` (use `endsAt` duration if the schema has it — check opportunistically, don't block), safety-tools bonus default, idempotent merge by `WarhornUuid` preserving user edits (test: re-import produces no dupes and keeps edits)
- [ ] **The nudge**: on app or window open, non-blocking status hint "N sessions since last import, roughly M hours unlogged" (+ optional "~K unspent hours this document"), same spirit as the §8 update-notification hint (test: hint math from fixture ledger + session list)
- [ ] Hours grid + rough totals + async cancellable Import button
- [ ] Manual hours entry dialog (Posting, Dungeoncraft, Organizing, Streaming, prep) with `EvidenceUrl`
- [ ] *(nice-to-have)* New-player candidate detection: first appearance across event history flags candidate; confirm/deny UI feeds +1/player bonus
- [ ] *(nice-to-have)* Balance math earned − spent per version; version rollover (40-hour carryover cap, 1–4 leftover rule)

**v3 — Achievements**
- [ ] Encode ALSR achievement list as embedded JSON (3-checkbox progression, retroactive flags, reward options)
- [ ] `AchievementClaim` model with evidence links (`WarhornUuid` or URL)
- [ ] Auto-suggestions from import: Online DMs, Gifts All Around (birthday week), Last Minute DMs if signup timestamps are queryable

---

## 12) PDF export *(lowest priority)*
- [ ] PDF export in C# (port `characters_json_to_pdf.py` or re-implement using a .NET PDF lib)
- [ ] Page layout options and basic theming

---

## 13) Archive cleanup
When all Python functionality has been ported to C# (sections 5, 6, and 12 fully complete), delete `archive/python/` and this section.
- [ ] Verify all Python script capabilities are covered in C# (per-character session logs, DM sessions, Markdown export, CSV export, PDF export, MSC export)
- [ ] Delete `archive/python/`
- [ ] Remove archive references from README.md and CLAUDE.md

---

## Legend
- [x] Completed
- [ ] Pending
