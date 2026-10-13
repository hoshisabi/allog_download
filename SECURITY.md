# Security — credential and secret handling

This document records **product and engineering decisions** for how Adventurers League Log Downloader stores the site password (and related configuration). It is the single place to align implementation, UI copy, and user expectations.

## Threat model (explicit)

- The secret is the **password for AdventurersLeagueLog.com**, similar in sensitivity to many passwords users already keep in a **browser or general-purpose password manager**. It is **low importance** relative to, say, an email or banking password.
- We assume **low risk** for a dedicated attacker reverse-engineering the app or reading local files with the user’s privileges. We **do** care about casual exposure (e.g. someone glancing at a settings file) and **basic** discouragement of trivial copy-paste of the secret.
- Users who want **no** password stored on disk should **disable** stored credentials (see below).

**Posture (2026-10-13):** give users options rather than one mandated mechanism — OS secure storage where the OS has it, a plainer fallback where it doesn't, and environment variables / command-line options for scripts. If a more secure option becomes practical later, add it alongside the others.

## User-facing choice: store or not

- **“Store credentials” (or equivalent)** is a **convenience** feature. When **off**, the app must **not** persist the password (and should clear any previously stored value).
- Copy in the app describes **the store actually in use** (see [Wording](#wording)). Never call stored data **“encrypted”** unless that store really encrypts it.

## Stores by platform

All stores implement `ICredentialStore` in `Allog.Core/Services`. Each holds one entry (site login now; Warhorn token later as a second entry).

| Platform | Store | Status |
|---|---|---|
| Windows | **Windows Credential Manager** — `WindowsCredentialStore` (advapi32 `CredRead` / `CredWrite` / `CredDelete`). Entry `AllogDownloader:AdventurersLeague`, the same name earlier versions used, so saved logins carry over | In use by the WPF app |
| Linux desktop | **Secret Service** (GNOME Keyring, KWallet) via libsecret | Planned (TASKS.md A7b-3) |
| macOS | **Keychain** | Deferred until it can be tested on a Mac (A7b-4); the file store covers macOS until then |
| No OS store (headless Linux, SSH sessions, macOS for now) | **File store** — `FileCredentialStore`, see below | Implemented; not yet used by any host (A7b-5) |
| CLI / scripts | Environment variable, `--password`, `--password-file` (e.g. from a `systemd-creds` encrypted credential) | Planned (TASKS.md 7b, Phase B) |

A `gpg` (`pass`-compatible) store would allow "unlock once per session" on headless Linux; it will be added only if someone asks for it.

**Considered and not used:** `Devlooped.CredentialManager` (Git Credential Manager's stores as a library). It requires a `git` executable on the user's machine to start — most Windows users don't have one — and has no default store on Linux. Spike results: TASKS.md A7a.

## File store

- **Location:** `credentials/<entry>.json` in the settings folder, e.g. `%AppData%\AllogDownloader\credentials\AdventurersLeague.json` on Windows, `~/.config/AllogDownloader/credentials/AdventurersLeague.json` on Linux. It is a separate subfolder rather than a field in `settings.json` so the folder can be restricted without changing the settings folder's permissions.
- **Contents:** `username` and `obfuscatedPassword` (see [Obfuscation](#obfuscation-not-encryption)).
- **Permissions on Unix:** folder `0700`, file `0600`. The file is written under a temporary name created with `0600` and then renamed into place, so the password is never on disk with looser permissions; an existing folder with looser permissions is tightened on save. On Windows the file inherits the per-user ACL of `%AppData%`.
- A missing or unreadable file is treated as "no saved credential."

## Obfuscation (not encryption)

- **Obfuscation** means the password is **not stored in plaintext**, but anyone with the file and knowledge of the scheme (or the app binary) can **recover** it. Do **not** present this as cryptographic protection.
- Implementation: `PasswordObfuscation` in `Allog.Core/Services` is the only encode/decode path. Format: `v1:` + Base64 of the UTF-8 password XORed with a fixed key. The XOR step is there so pasting the value into a Base64 decoder doesn't reveal the password; it adds no real protection. The `v1:` prefix leaves room for a different scheme later.

## Unattended use

Anything an unattended job (cron, a systemd timer) can read without a person present can also be read by other processes running as the same user. The file store and `--password-file` trade that exposure for convenience; the OS stores (and a future `gpg` store) can require the user to be logged in or unlock first.

## Wording

| Store | Say | Don't say |
|---|---|---|
| Windows Credential Manager | “saved in Windows Credential Manager” | — |
| Secret Service / Keychain | “saved in your system keyring” / “saved in your Keychain” | — |
| File store | “saved in a file on this computer (obfuscated, not encrypted)” | “encrypted,” “secure storage” |

The WPF app's current strings name Windows Credential Manager, which is accurate because WPF only uses that store. Hosts that can fall back to the file store (CLI, Avalonia) must pick their wording from the store actually in use.

## Documentation maintenance

- When behavior changes (a new store, a store becoming the default somewhere, a change to the obfuscation format), update this file and the user-visible strings together.
