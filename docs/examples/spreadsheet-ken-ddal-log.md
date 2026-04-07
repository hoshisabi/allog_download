# Adventurers League Character Log — Spreadsheet Example (Ken DDAL Log)

This is an example of how some players maintain AL character history in a **flat spreadsheet**: one row per session (plus “Start” / rebuild rows and per-character **Total** rows). It can serve as a reference for CSV/Excel-style export or for aligning scraped fields with a familiar layout.

The sheet title shown in Google Sheets is **Ken DDAL Log.xlsx**.

## Reference URLs (bookmark these)

| Link | Purpose |
| ---- | ------- |
| [Ken DDAL Log — share link](https://docs.google.com/spreadsheets/d/1bqbClFX-MMgIWDKbnEEmmxBvwzO6wYSXm_ig690kojA/edit?usp=sharing) | **Canonical URL** for this example (same spreadsheet; use for docs and future look-ups). |
| [Same spreadsheet, tab `gid=2020762004`](https://docs.google.com/spreadsheets/d/1bqbClFX-MMgIWDKbnEEmmxBvwzO6wYSXm_ig690kojA/edit?gid=2020762004#gid=2020762004) | Deep link to the tab that was used when the layout was captured (if the share link opens a different default tab). |

Spreadsheet ID: `1bqbClFX-MMgIWDKbnEEmmxBvwzO6wYSXm_ig690kojA`.

---

## Changelog (repo)

What was added or changed in this project in relation to this spreadsheet style:

| Item | Detail |
| ---- | ------ |
| **`session_log_workbook.csv` export** | WPF: **File → Export session log workbook (CSV)…** merges all local `character_{id}.csv` files next to `characters.json` into one workbook-style CSV. |
| **Implementation** | `Adventure League Log Downloader/Services/SessionLogWorkbookCsvExporter.cs` — parses site CSV sections, maps columns, attaches interleaved `MAGIC ITEM` rows to the preceding `*LogEntry` row. |
| **Date parsing reuse** | `CharacterLogCsvReader.TryParseSiteCsvTimestamp` and `FormatSiteDateForWorkbook` — shared parsing/formatting for CSV timestamps. |
| **Documentation** | This file documents the Ken example layout, the real site CSV layout, the workbook column mapping, gaps vs. the Ken sheet, and suggestions below. |

---

## Suggestions and future enhancements

These are **not** implemented yet; they would move exports closer to the Ken sheet or improve usability.

1. **Derived rows** — Generate `{Name} Total` (and optional per-tier subtotals) from **Gold** / **Magic Item Count** / **Level** with explicit rules, since the site does not emit those rows.
2. **Start / Rebuild rows** — Manual or templated rows (player convention only); could be optional blank rows or a second “template” CSV for merge.
3. **Fractional level on totals** — The Ken sample uses non-integer **Level** on total rows; treat as a player tracker unless AL rules are encoded explicitly.
4. **Single “Magic Items” column** — First pass splits **Magic Item Count** and **Magic Item Names**; could add a Ken-style combined column or formula-friendly layout.
5. **Excel `.xlsx`** — Same data with column widths, freeze header row, filters (requires a library or interop).
6. **Export scope** — Optional “selected characters only” or output path picker.
7. **Needs Update** — Could pre-fill from heuristics (e.g. empty **DM Name** on a **CharacterLogEntry**) or leave for manual use only.

---

## Column layout

| Column | Role |
| ------ | ---- |
| **Name** | Player or PC identifier (same name repeated each session; subtotal rows use `{Name} Total`) |
| **Adv Name** | Adventure title, or labels like `Start` / `Rebuild` for character setup rows |
| **Adv Code** | Adventure code (e.g. `LoG-CORE 1-1`) when applicable; **Start** rows sometimes use a **date** in this column instead of a code |
| **DM Name** | DM for that session |
| **Gold** | Gold change or running-style value for that row (numbers or blank; **Total** rows aggregate) |
| **Magic Items** | Count or marker for items (e.g. `1`, `0`, or `*` where noted) |
| **Notes** | Free text: purchases, story awards, marks of prestige, declined rewards, etc. |
| **Level** | Level associated with the row (e.g. after **Start**); **Total** rows may repeat effective level |
| **Needs Update** | Optional flag column (often empty in the sample) |

---

## Example rows (abbreviated)

Long **Notes** cells are shortened with `[…]`; see the linked sheet for full text.

| Name | Adv Name | Adv Code | DM Name | Gold | Magic Items | Notes | Level | Needs Update |
| ---- | -------- | -------- | ------- | ---- | ----------- | ----- | ----- | ------------- |
| Ke Niao | Start | 7/5/2025 | Ken Beckman | 1.5 | 1 | Building for level 3 LoG playtest. Rogue + Charlatan; Soft Steps (Boots of Elvenkind); gear list. Body double for a Prince of Verbobonc. […] | 3 | |
| Ke Niao | Rebuild | 10/31/2025 | Ken Beckman | | | Adjusted to use Shortsword +1 for another playtest. | | |
| Ke Naio Total | | | | 1.5 | 1 | | 3 | |
| Weng Ki | Start | 7/1/2025 | Ken Beckman | 20 | 1 | Building for level 7 one shot. Barbarian + Soldier; Adamantine Plate Barding; gear and horse. […] | 7 | |
| Weng Ki | Battle of Emridy Meadows | 7/11/2025 | Matt Brown | | | Mustered for forces vs. Temple of Elemental Evil; perished in combat; used 1 Healing Potion; physical cert / “Hero of Emridy Meadows” prestige to Xen. […] | | * |
| Weng Ki Total | | | | 20 | 1 | | 7 | |
| Xen | Start | 6/30/2025 | Ken Beckman | 20 | 0 | Fighter + Farmer; gear and mastiff. `LoG-218673640` | 1 | |
| Xen | A Village Called Hommlet | LoG-CORE 1-1 | 7/12/2025 | Amy Jordan | 0 | 1 | Hommlet investigation; Guardian Shield +1; Militia of Hommlet prestige. […] | 1 | |
| Xen | Darkness in Nulb | LoG-CORE 1-2 | 8/15/2025 | Liam James | 75 | 0 | Road to Nulb; Rezzalyn rescue; Favor of the Pirate-Queen; declined shortsword, took gp bundle. […] | 0.5 | |
| Xen | Shadows Beneath the Skull | LOI-002 | 10/25/2025 | | -41.5 | 0 | Isles trilogy; Brinejacks / Daughters of Syrul; Worthy Adversary prestige; declined boots, bundle 2; armor changes. […] | 0.5 | |
| Xen | Tempest of the Hollow Maw | LOI-S001 | 10/25/2025 | | 125 | 0 | Turtleback Cove; Tempest Tamer prestige; declined whip bundle, took gp. […] | 0.3333333333 | |
| Xen Total | | | | 178.5 | 1 | | 3.333333333 | |

---

## Patterns to note

1. **Start / Rebuild rows** — Not every row is a played session; setup and rebuild rows use **Adv Name** and carry narrative in **Notes**.
2. **Dates in Adv Code** — For **Start** rows, **Adv Code** may hold a session date instead of a LoG/DDAL-style code.
3. **Subtotal rows** — `{Name} Total` rolls up **Gold**, **Magic Items**, and **Level** (here **Level** on totals looks like a fractional or aggregate advancement value, not only integer level).
4. **Sparse columns** — Some sessions leave **Gold** or **Level** blank and put everything in **Notes**.
5. **Markers** — **Magic Items** sometimes uses `*` (meaning depends on the author; treat as opaque unless clarified).

---

## AdventurersLeagueLog.com per-character CSV (source layout)

Downloaded files are named `character_{id}.csv` (next to `characters.json`). A typical file has:

1. **Character banner** — row with `name,race,class_and_levels,…` then a data row.  
2. **Log header** — row starting with `type`, then snake_case column names, for example:  
   `type`, `adventure_title`, `session_num`, `date_played`, `session_length_hours`, `player_level`, `xp_gained`, `gp_gained`, `downtime_gained`, `renown_gained`, `num_secret_missions`, `location_played`, `dm_name`, `dm_dci_number`, `notes`, `date_dmed`, `campaign_id`  
3. **Magic item header** — `MAGIC ITEM`, `name`, `rarity`, `location_found`, `table`, `table_result`, `notes`  
4. **Body** — rows whose first column is a log entry type (`CharacterLogEntry`, `DmLogEntry`, `PurchaseLogEntry`, `TradeLogEntry`, …), optionally followed by one or more **`MAGIC ITEM`** rows tied to that entry.

Quoted fields may span multiple lines (long **notes** or magic item descriptions).

---

## App export: `session_log_workbook.csv` (first pass)

The WPF app can merge all local `character_*.csv` files into one UTF-8 (with BOM) workbook-style CSV:

- **Menu:** File → **Export session log workbook (CSV)…**  
- **Output:** `{same folder as characters.json}/session_log_workbook.csv`  
- **Rows:** one per `*LogEntry` line; **Magic Item Names** / **Magic Item Count** come from following `MAGIC ITEM` rows until the next log entry.

**Column order (spreadsheet-style fields first, then extra site fields):**

| Output column | Source |
|---------------|--------|
| Name | Character list (`characters.json`) |
| Adv Name | `adventure_title` with leading code removed when detected |
| Adv Code | Leading token from `adventure_title` when it looks like a code (e.g. `DDAL07-01`, `DDIA05`, `SJ-DC-ISL-01`) |
| DM Name | `dm_name` |
| Gold | `gp_gained` |
| Magic Item Count | Count of `MAGIC ITEM` lines after this entry |
| Magic Item Names | `name` cells from those lines, `; ` separated |
| Notes | `notes` |
| Level | `player_level` |
| Needs Update | Left empty (tracker column for manual use in Excel) |
| Entry Type | Row `type` (e.g. `CharacterLogEntry`) |
| Character Id | From JSON |
| Session Date | `date_played` → local `yyyy-MM-dd` when parseable |
| Date DM Ran | `date_dmed` → same |
| XP | `xp_gained` |
| Downtime Days | `downtime_gained` |
| Renown | `renown_gained` |
| Secret Missions | `num_secret_missions` |
| Session Length Hours | `session_length_hours` |
| Location Played | `location_played` |
| Session Num | `session_num` |
| Campaign Id | `campaign_id` |
| DM DCI | `dm_dci_number` |

This does **not** recreate manual **Start** / **Rebuild** / **Total** rows from the Ken example; those remain future enhancements if you add derived rows or templates.

---

## Comparison to app export goals

Useful alignment with data the downloader may already scrape or plan to expose:

- ✅ Character name, adventure name/code, DM, dates, gold, magic items, narrative notes  
- ⚠️ **Total** rows would need to be **generated** (sums / rules), not scraped from the site  
- ⚠️ **Level** as fractional totals is a **player-specific convention** — not assumed from AL log HTML  

Some columns (**Needs Update**, `*` in **Magic Items**) are **tracker-only** and may not map to AdventurersLeagueLog.com fields.
