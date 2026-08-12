# AL Service Rewards Tracker — Design

Tracks service hours, bonuses, and (later) reward redemptions under the **D&D Adventurers League Service Rewards** document (**ALSR v2026.1**, released 2026-08-10). The primary automated data source is Warhorn (event `pandodnd`), where every session Dan runs is online, publicly announced, streamed, and run with safety tools.

Decision record (2026-08-11):
- **Importer lives in C#** — a `WarhornClient` in `Services/`, plain `HttpClient` POST to `https://warhorn.net/graphql`. UI-free so it lifts into `Allog.Core` during Phase A (TASKS.md §7b). The Python Warhorn clients in `al_adventure_catalog/maintaindb/warhorn_api.py` and `scrollcase/process_session.py` remain for their own repos' purposes; this repo does not shell out to them.
- **v1 scope = redemption tracking** (per-document reward menus, dual assignment, historical import, publish-back). Originally v1 was "ledger + Warhorn import," but reviewing the manual tracking file (see *Prior art* below) showed hours were never the constraint — redemption/assignment capture is where manual tracking died. Hours ledger + Warhorn import moved to v2; achievements checklist is v3.
- Destination repo is this one (`allog_download`), not the Rails `adventurers-league-log` clone.

---

## ALSR rules digest (what the tracker must model)

Per **service type**, one service hour per hour (30–59 min rounds up):

| Service type | Rate | Per-session bonuses |
|---|---|---|
| Dungeon Mastering | 1/session hour (may include prep, session zeroes, mentoring) | +1 safety tools, +1 per new AL player |
| Streaming | 1/hour of publicly-available AL actual play / informative video | (player bonuses don't apply to DMs — DMs take theirs under Dungeon Mastering) |
| Organizing | 40+/event day (EO or staff, 2+ table public event) | +1/session if every DM used safety tools, +1 per new AL player |
| Posting | 1 per 4 platforms posted to | — |
| Dungeoncraft | 1 per 2 DMsGuild reviews / cited error reports | — |

Accounting rules that shape the data model:

- **Session-time hours are special**: Adventure Rewards (DM claims a player's rewards) can only be paid with *session time* hours — not prep, safety, or new-player bonus hours. The ledger must keep these buckets distinct.
- **Version accounting**: hours belong to an ALSR version. Max **40 saved hours** transfer to the next version; a 1–4 leftover balance can buy one non-Unique Common/Uncommon reward from either version.
- **Rewards by Rarity** costs: 5 (Common/Uncommon), 10 (Rare), 20 (Very Rare), 40 (Legendary / Repeatable); Unique and Steeping items are double cost. Each redemption also grants a level + DT/GP and is assigned to one character.
- **Achievements** cost 0 hours but follow a 3-checkbox progression (first claim → Unique claim → repeatable ∞), some flagged **Retroactive** (requires verifiable documentation). Evidence links matter.

## Hours ledger data model (v2)

`ServiceLogEntry` (one row per session or manual act of service):

- `Id` (GUID), `Source` (`warhorn` | `manual`), `WarhornUuid` (idempotency key for imports)
- `Date`, `ServiceType` (enum: `DungeonMastering`, `Streaming`, `Organizing`, `Posting`, `Dungeoncraft`)
- `SessionHours` — the session-time bucket (Adventure-Rewards-eligible)
- `PrepHours` — self-reported, counts as DM service hours but not session-time
- `SafetyToolsBonus` (bool → +1) — default **true** for `pandodnd` imports
- `NewPlayerCount` (confirmed) and `NewPlayerCandidates` (names awaiting confirmation)
- `ScenarioName`, `EventSlug`, `Notes`, `EvidenceUrl` (VOD link, announcement link, …)
- `AlsrVersion` (e.g. `"2026.1"`)

Persisted as `service_rewards.json` in the data location folder (same family as `character_*.csv` / DM session JSON). Re-import merges by `WarhornUuid` — never duplicates, never clobbers user-entered fields (confirmed new players, prep hours, notes).

Totals derived, not stored: per service type, session-time vs bonus split, grand total, per-version balance.

## Warhorn import

**Philosophy (per Dan, 2026-08-11): a VERY rough import is fine.** The job is not precise accounting — it's the nudge: *"you ran sessions, you earned hours, don't forget to track them."* Hours earned always dwarf what the menus can absorb, so estimates are plenty; nothing here blocks on getting durations exactly right.

Query (extends the one in scrollcase): `eventSessions(events: [slug])` → `uuid`, `name`, `startsAt`, `endsAt`, `gmSignups { user { name } }`, `playerSignups { user { name } }`, `scenario { name }`.

- Filter to sessions where `gmSignups` contains the configured Warhorn display name.
- Hours: use a configurable **default hours per session** (e.g. 4). If `endsAt` turns out to exist on `EventSession` (scrollcase only queries `startsAt` — check opportunistically, not as a blocker), use the real duration instead. Entries are user-editable either way.
- **The nudge**: on opening the app (or the Service Rewards window), a lightweight check for sessions since the last import surfaces a non-blocking hint — "N sessions since &lt;date&gt;, roughly M hours unlogged" — in the same spirit as the planned update-notification hint (TASKS.md §8). Optionally also: "you have ~K unspent hours in the current document."
- **New-player candidates**: import full event history ordered by date; a player's first-ever appearance across all sessions flags them as a candidate on that session. This proves "new to my table," not "new to AL" — the UI asks the user to confirm/deny each candidate before the +1 counts.
- Auth: Warhorn application token. Stored via the existing credential-store abstraction (`ICredentialStore`), not in `settings.json` (see SECURITY.md); `WARHORN_APPLICATION_TOKEN` env var honored as an override for CLI/scripting parity later.
- Settings additions (`UserSettings`): `WarhornEventSlugs` (default `["pandodnd"]`), `WarhornDisplayName`.

## Hours ledger UI (v2)

A `ServiceRewardsWindow` in the pattern of `DmSessionWindow`:

- Grid of ledger entries (date, type, scenario, session hours, bonuses, source), sortable.
- Totals panel: per-type totals, session-time vs bonus split, grand total for the active ALSR version.
- **Import from Warhorn** button (async, cancellable, progress like existing downloads).
- **Confirm new players** flow: list of candidates with session context; confirm/deny.
- Manual entry dialog for the non-Warhorn types (Posting, Dungeoncraft, Organizing, Streaming hours, prep time).

## Prior art: the manual tracking file (lessons)

`C:\Users\decha\dev\hoshisabi.github.io\rpg\al\service_rewards.md` is Dan's hand-maintained redemption tracker covering ALSR versions 11A (Nov 2021) through 2.1 — abandoned from tracking fatigue. It reshapes several assumptions:

1. **Hours were never the constraint.** Every document shows earned hours far exceeding the redeemable menu ("total 210 … earned >400"; "total 210 + ? … earned 480 + ucon 7 rewards"). The tracking failure was on the *redemption* side: dozens of claimed rewards sit at `(I?/L?)` — item claimed, but which character got the item and which got the level was never recorded. Precise hour counting is evidence/bookkeeping; redemption + assignment capture is the real product.
2. **Dual assignment per redemption.** The `I:x/L:y` notation means the *item* and the *level* can go to different characters (newer docs add a downtime-vs-gp choice). The redemption model needs `ItemAssignedTo`, `LevelAssignedTo`, and `SecondaryRewardChoice` (DT | GP) — not one "character" field.
3. **Flavor names + functions-as mapping.** Rewards are often custom-flavored ("Siren's caress … functions as a wand of paralysis"). Fields: `DisplayName`, `FunctionsAs`, `SourceUrl` (D&D Beyond links are the established habit), free-text flavor description.
4. **Reward menus are per-document data.** Tier structures, costs, and slot counts changed repeatedly (tier 3 was 15h in 11A, 20h later; tier 4 was 80h then 40h; 2.1 renamed tiers to rarities; 2026.1 added achievements). Each ALSR version needs its own menu definition — same data-driven approach planned for v3 achievements. Slots-checked-off is the primary redemption view.
5. **Adventure-reward write-ins are structured.** "Gain Rewards as if You Had Been a Player" entries carry an adventure code, hour cost (e.g. `-4hr`), a claim limit per document (5, later 10), and a bundle of rewards. Model as a distinct redemption category with `AdventureCode` and `HoursSpent`.
6. **Historical import.** A one-time parser for the manual file's format (`- [x] Name + secondary (I:x/L:y)`) seeds the redemption history back to 2021 instead of abandoning it. Unknowns stay unknown (`I?` imports as unassigned) but become visible as a to-resolve list.
7. **Publish-back.** Since the manual file lives in the GitHub Pages repo, an exporter that regenerates that markdown page from tracker data keeps the public page alive with zero manual editing — and replaces the habit that died, rather than competing with it.

## v1 — Redemption tracking

- **Per-document reward menus as data**: each ALSR version is a JSON definition (id, date range, sections with cost/slot lists — fixed items with links vs open "campaign-available" slots — repeatables, unique/steeping double-cost flags, adventure-reward write-in limits). Start by encoding 2026.1; legacy versions (11A → 2.1) get encoded alongside the historical import.
- **`ServiceRedemption`**: slot reference (version + section + slot), `DisplayName`, `FunctionsAs`, `SourceUrl`, flavor text, `ItemAssignedTo`, `LevelAssignedTo`, `SecondaryRewardChoice` (DT | GP + amount), `AdventureCode` + `HoursSpent` for write-ins, date. Persisted in `service_rewards.json` alongside the (v2) hour ledger.
- **Historical import**: one-time parser for the manual file format; unknown assignments import as unassigned and surface in a to-resolve list.
- **Publish-back**: exporter regenerates the `hoshisabi.github.io/rpg/al/service_rewards.md` page from tracker data.
- **UI**: menu view per version with checkbox slots, claim dialog capturing both assignments, unassigned-resolution list.

## Later phases

**v2 — Hours ledger + Warhorn import:** the `ServiceLogEntry` model, `WarhornClient`, and import pipeline described above. Balance = earned − spent per version; carryover math (40-hour cap, 1–4 leftover rule) surfaced at version rollover.

**v3 — Achievements:** ALSR achievement list encoded as embedded JSON data (name, category, checkbox progression, rarity of each reward option, retroactive flag). `AchievementClaim` rows link to evidence (`WarhornUuid` or URL). Some auto-suggest from import: *Online DMs* (every pandodnd session), *Last Minute DMs* (GM signup < 4h before start — needs signup timestamp, verify availability), *Gifts All Around* (session in birthday week).

**Ideas beyond that** (not committed):
- Streaming verification: check VODs still publicly available + duration via YouTube Data API.
- Posting checklist per announced event (Discord / Facebook / subreddit / BlueSky → 1 hour per 4).
- Cross-check ledger against this app's own scraped DM sessions (`DmSessionRecord`) to catch sessions logged on adventurersleaguelog.com but missing from Warhorn, or vice versa.
- Printable service log export matching ALSR Appendix A.

## Open questions

- Does `EventSession` expose `endsAt` (or a duration) in Warhorn's GraphQL schema? Nice-to-have only — rough import uses a default hours-per-session either way.
- Can one streamed DM session legitimately earn both Dungeon Mastering hours *and* Streaming hours? The ALSR reads as yes (they are separate service types with separate duties), but worth confirming with AL admins before double-logging. The data model supports either answer (two entries, one per type).
