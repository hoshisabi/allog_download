# Security — credential and secret handling

This document records **product and engineering decisions** for how Adventurers League Log Downloader stores the site password (and related configuration). It is the single place to align implementation, UI copy, and user expectations.

## Threat model (explicit)

- The secret is the **password for AdventurersLeagueLog.com**, similar in sensitivity to many passwords users already keep in a **browser or general-purpose password manager**.
- We assume **low risk** for a dedicated attacker reverse-engineering the app or reading local files with the user’s privileges. We **do** care about casual exposure (e.g. someone glancing at a settings file) and **basic** discouragement of trivial copy-paste of the secret.
- Users who want **no** password stored on disk should **disable** stored credentials (see below).

## User-facing choice: store or not

- **“Store credentials” (or equivalent)** is a **convenience** feature. When **off**, the app must **not** persist the password (and should clear any previously stored value).
- Copy in the app should **not** describe stored data as **“encrypted”** unless we implement real cryptography. For the portable / settings-based path we use **obfuscation** and should say **“obfuscated password”** (or similar honest wording).

## Approaches by platform / host (planned and current)

### Windows WPF (current)

- **Windows Credential Manager** may continue to be used via the existing `CredentialManagement`-based path for “remember me.” This is separate from settings JSON.
- **Future consolidation:** we may also store an **obfuscated** password alongside other settings for portability or a single code path; that is a product decision when Core/CLI work lands.

### Portable console (CLI / TUI / cross-platform)

- **Non-interactive / scripting:** environment variables and command-line arguments (documented; avoid logging values).
- **Optional persistence:** settings under the user’s app data / home directory, with the **password field obfuscated** — **not** strong encryption — **colocated with other settings** (e.g. same folder as `settings.json`) unless we split files later for clarity.
- **No** macOS Keychain / Linux Secret Service in the **first** cross-platform wave unless we revisit this document.

## Obfuscation (not encryption)

- **Obfuscation** means the password is **not stored in plaintext** in the JSON/file, but anyone with the file and knowledge of the scheme (or the app binary) can **recover** it. Do **not** present this as cryptographic protection.
- Implementation detail (when built): centralize encode/decode in one module; use consistent field naming such as `obfuscatedPassword` in persisted data.
- Optional: mild file permissions on Unix (`0600`) for the settings file — **defense in depth**, not a substitute for honest labeling.

## Documentation maintenance

- When behavior changes (e.g. move from Credential Manager-only to settings obfuscation on Windows), update this file and user-visible strings together.
