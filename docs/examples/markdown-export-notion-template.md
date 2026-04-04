# Adventurers League Character Log - Markdown Export Template

This is an example of how AL players organize their character logs in Notion (via Markdown export). This template can serve as a reference for designing enhanced Markdown export features.

**Source**: Based on digitalmatt's Notion template structure from adventurersleague.info community

---

## Structure Overview

### 1. Character Info Table
A summary table with key character stats:

| CHARACTER INFO |  |  |  |  |
| ----- | :---: | :---: | :---: | ----- |
| **CharacterName** CharacterRace CharacterClassAndSubclass Forgotten Realms Campaign, Tier 1 | **Current  Level** | **Current Downtime** | **Current  Gold** | **Current Attunement Slots** |
|  | 5 | 15 | 250 | *[Ring of Protection] [empty] [empty]* |

### 2. Notable Inventory Tables
Organized by item type with active/vaulted status:

| NOTABLE INVENTORY |  |  |
| ----- | :---- | :---- |
| **Common Magic Items** | **Consumables & Spell Components** | **Permanent Magic Items** |
| **Active (up to 5)** *Potion of Healing, Spell Scroll (Identify)* **Vaulted** *Potion of Climbing x2* | **Active (up to 5)** *Diamond (300gp)* **Vaulted** *(none)* **Components** *Pearl (100gp)*  | **Active (up to 3)** *Ring of Protection (+1 AC, attunement)* **Vaulted** *Bag of Holding* |

### 3. Session Logs Table
Detailed session history with running totals:

| SESSION LOGS |  |  |
| ----- | :---: | :---- |
| **Adventure** | **DM** | **Advancement, Rewards, Session Notes** |
| **DDAL05-01** Treasure of the Broken Hoard 01/15/2024 | **Alice Smith** The Adventurers League |  **Milestone:** One level-up (advance to Level 5) +50 gp (to 250) +5 downtime days (to 15) ***Ring of Protection*** *rare, attunement, +1 AC and saves* **Story Award: Faction Friend** You've earned the respect of the Harpers. **Other info:** Defeated the dragon! **Party members:** Bob the Fighter, Carol the Cleric |
| **DDAL05-02** The Black Road 02/20/2024 | **Bob Jones** TAL Server | **Milestone:** No level-up +100 gp (to 350) +5 downtime days (to 20) ***Potion of Greater Healing x2*** **Other info:** Explored the Underdark |

---

## Key Features to Note

1. **Running Totals**: GP and downtime show both gained amount and new total
   - Example: `+50 gp (to 250)` instead of just `+50 gp`

2. **Rich Formatting**:
   - Bold for item names and section headers
   - Italic for metadata/descriptions
   - Combined **bold+italic** for magical items (***Item Name***)

3. **Inventory Limits**: Explicitly shows AL limits
   - Common Magic Items: up to 5 active
   - Consumables: up to 5 active
   - Permanent Magic Items: up to 3 active
   - "Vaulted" category for stored items

4. **Session Detail**: Each session includes:
   - Adventure code (DDAL05-01)
   - Adventure name
   - Date
   - DM name and server
   - Level advancement
   - GP/downtime with running totals
   - Magic items gained (with rarity/properties)
   - Story Awards
   - Other notes
   - Party composition

5. **Attunement Tracking**: Shows which magic items are attuned in the character info table

---

## Implementation Notes for Export

When implementing this format:

1. **Calculate Running Totals**: Need to sum GP and downtime across all sessions
2. **Item Categorization**: Must categorize items by type (common/consumable/permanent)
3. **Active vs Vaulted**: Track which items are "active" based on AL limits
4. **Attunement Slots**: Show attuned items in character header (max 3)
5. **Date Formatting**: Use consistent date format (MM/DD/YYYY shown here)
6. **Markdown Tables**: Use proper pipe-delimited Markdown table syntax
7. **Text Alignment**: Center-align numeric columns (`:---:`)

---

## Comparison to Current JSON Export

Our `characters.json` already has:
- ✅ Character name, race, class
- ✅ Session logs with adventures, DM names
- ✅ GP and downtime per session
- ✅ Magic items

Missing/needs enhancement:
- ⚠️ Running totals for GP/downtime
- ⚠️ Item categorization (common/consumable/permanent/rarity)
- ⚠️ Active/vaulted status
- ⚠️ Attunement tracking
- ⚠️ Story awards
- ⚠️ Party member lists
- ⚠️ Tier information

Some of these fields may not be available from AdventurersLeagueLog.com scraping.
