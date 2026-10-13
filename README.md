# allog_download

A Windows desktop app, and a command-line tool for Windows, macOS, and Linux, for downloading and processing character/session data from AdventurersLeagueLog.com.

## Adventure League Log Downloader (C# WPF App)

The primary deliverable. A native Windows GUI app built on .NET 10.

### Features
- Native Windows authentication via Credential Manager
- Scrapes character lists and logs directly from AdventurersLeagueLog.com
- Export to JSON (more formats coming)
- Per-character CSV downloads and a merged session log workbook CSV
- Hand a character off to [Moonsea Codex](https://moonseacodex.com/)

### Moonsea Codex import

Moonsea Codex (MSC) can create a character from an AdventurersLeagueLog.com CSV. To send one over:

1. Download the character's CSV (the **CSV** column in the character list shows which ones are on disk).
2. Double-click the character, then click **Import to Moonsea Codex…**. The app saves `moonseacodeximport.csv` to your Downloads folder (`moonseacodeximport (1).csv` and so on if that name is taken), Explorer opens with it selected, and your browser opens the MSC characters page (sign in if asked).
3. On MSC, click **Import** and drop the file into the dialog.

The app never sees your MSC login. Things to know about MSC's importer:

- Each import creates a **new** character. Importing the same CSV twice makes a duplicate.
- The CSV doesn't record everything, so MSC assumes every level-up was taken and no gold or downtime was spent, and it guesses magic item details. Check the result against your records.
- The character's level comes from the class text on AdventurersLeagueLog.com (for example `Wizard-1`). If that text has no level or an out-of-date one, MSC shows level 1 or the old level; fix it on MSC after importing.
- The app marks the character as not public. On MSC that only hides it from MSC's Discord bot listings; anyone with the character's link can still view it.
- MSC's importer splits each line on commas and can't read multi-line fields, so the app's copy puts each session's notes on one line and turns commas into semicolons. Paragraph breaks in notes become spaces.
- MSC skips items with "potion" or "scroll" in the name and doesn't import purchase entries.
- Upload the `moonseacodeximport` file rather than `character_{id}.csv`, and don't re-save it in Excel: MSC checks the header lines exactly.

### Installation

Official builds are published on **[Releases](https://github.com/hoshisabi/allog_download/releases)**. On the release page, under *Assets*:

- **`AdventurersLeagueLogDownloader-vX.X.X-win-x64-selfcontained.zip`** — larger file, includes the .NET runtime. Use this if unsure.
- **`AdventurersLeagueLogDownloader-vX.X.X-win-x64-framework.zip`** — smaller file. Requires [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) for Windows x64 installed first.

**Requirements:** Windows 64-bit only.

Extract the ZIP to a folder you keep (e.g. `Documents\AllogDownloader`) and double-click `Adventure League Log Downloader.exe`.

**Credentials:** how stored passwords are handled (including limitations) is documented in [`SECURITY.md`](SECURITY.md).

### Building

```powershell
dotnet publish "Adventure League Log Downloader" -p:PublishProfile=FolderProfile
```

Output: `Adventure League Log Downloader\bin\Release\net10.0-windows\publish\win-x64\`

### Development

Open `Adventure League Log Downloader.sln` in Visual Studio 2022 or JetBrains Rider. Target framework: `.NET 10.0-windows`.

From the command line, `dotnet build "Adventure League Log Downloader.sln"` builds the app; the Debug build is at `Adventure League Log Downloader\bin\Debug\net10.0-windows\win-x64\Adventure League Log Downloader.exe`. `dotnet test --project "Adventure League Log Downloader.Tests"` runs the tests. The tests cover `Allog.Core` only, so they also run on macOS and Linux (the Windows Credential Manager tests are skipped there).

---

## Command-line tool (`allog`) — Windows, macOS, Linux

`allog` does what the desktop app's **Download** button does: signs in, downloads your character list and character CSVs, and saves `characters.json`. It reads the same settings file and saved login as the desktop app, so on Windows the two share one data folder.

### Getting it

There's no release download yet. Build it with the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0), picking your platform (`linux-x64`, `osx-arm64`, `osx-x64`, or `win-x64`):

```bash
dotnet publish Allog.Cli -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true -o out/allog-linux-x64
```

The result is one file, `out/allog-linux-x64/allog` (`allog.exe` on Windows), that runs without .NET installed. Copy it somewhere on your `PATH`, such as `~/.local/bin`.

### Signing in

Save your login once:

```bash
allog login
```

It asks for your username and password (the password isn't shown as you type), checks them on AdventurersLeagueLog.com, and saves them only if the sign-in works. `allog logout` removes the saved login.

| OS | Where the login is saved |
|---|---|
| Windows | Windows Credential Manager, shared with the desktop app |
| macOS, Linux | `~/.config/AllogDownloader/credentials/AdventurersLeague.json`. The password is obfuscated, **not encrypted**, and the file is readable only by you (`0600`) |

For scripts, or to skip saving a login, any command that signs in also accepts these:

- `--password-file PATH`: a file whose first line is the password (for example from `systemd-creds`)
- `--env-file PATH`: a `KEY=VALUE` file with `ALLOG_USERNAME` and `ALLOG_PASSWORD`
- the `ALLOG_USERNAME` and `ALLOG_PASSWORD` environment variables
- `--password PASSWORD`. Other users on the same machine can see a password given this way in the process list, so prefer the options above.

`--username NAME` sets the username for any of them. If more than one password source is given, `--password` wins, then `--password-file`, then `--env-file`, then the environment variables. With none of them, commands use the saved login. See [`SECURITY.md`](SECURITY.md) for what each option protects against.

### Downloading characters

```bash
allog characters                  # character list + CSVs, using the desktop app's settings
allog characters --csv missing    # only CSVs you don't have yet
allog characters --csv skip       # list only
allog characters --output ~/AL/characters.json
```

| Option | Default |
|---|---|
| `--output PATH` | the desktop app's data location: `%AppData%\AllogDownloader\characters.json` on Windows, `~/.config/AllogDownloader/characters.json` on macOS and Linux. Character CSVs go in the same folder |
| `--csv skip\|missing\|all` | the desktop app's setting (`missing` until you change it) |
| `--delay SECONDS` | the desktop app's setting (0.25): the pause between requests to the site |
| `--quiet` | off; with it, only the summary and errors are printed |

Progress goes to stderr and the summary to stdout. In a terminal, each step updates one line in place.

### Running it on a schedule

Exit codes: `0` ok, `1` error (for example a failed sign-in), `2` no login or bad arguments, `3` finished but some character CSVs failed, `130` cancelled.

A weekly cron job, using the saved login:

```cron
0 6 * * 1  $HOME/.local/bin/allog characters --csv missing --quiet
```

`allog --help` and `allog characters --help` list everything.

## Archived Python Scripts

The original Python implementation lives in `archive/python/`. It is **not maintained** — preserved as a reference for the logic that was ported to the C# app. See [`archive/python/README.md`](archive/python/README.md) for details.
