using System.Collections.Generic;

namespace Adventure_League_Log_Downloader.Services;

/// <summary>
/// A single DM log entry, built in two passes:
///   1. Basic fields from the paginated list (<c>season9_format</c> table).
///   2. Detail fields from the individual session page — only fetched when <see cref="DetailFetched"/> is false.
/// Keeping <see cref="DetailFetched"/> in the JSON enables recovery: a run interrupted mid-detail-fetch can
/// resume from where it stopped without re-downloading sessions that were already fully scraped.
/// </summary>
public sealed class DmSessionRecord
{
    // ── From the list page ──────────────────────────────────────────────────

    public string Id { get; set; } = string.Empty;
    public string? DateDmed { get; set; }
    public string? AdventureTitle { get; set; }
    public string? SessionNum { get; set; }
    public string? DmRewardChoice { get; set; }

    /// <summary>Comma-separated magic item names as shown on the list page (summary only).</summary>
    public string? MagicItemsSummary { get; set; }

    public string? CharacterName { get; set; }
    public string? CharacterId { get; set; }

    // ── From the detail page ─────────────────────────────────────────────────

    /// <summary>True once the detail page has been successfully fetched and parsed.</summary>
    public bool DetailFetched { get; set; }

    public string? SessionLengthHours { get; set; }
    public string? PlayerLevel { get; set; }
    public string? LocationPlayed { get; set; }
    public string? XpGained { get; set; }
    public string? GpGained { get; set; }
    public string? DowntimeGained { get; set; }
    public string? RenownGained { get; set; }
    public string? NumSecretMissions { get; set; }

    /// <summary>Date the DM rewards were assigned to a character (separate from <see cref="DateDmed"/>).</summary>
    public string? DatePlayed { get; set; }

    public string? Notes { get; set; }

    /// <summary>Full magic item records scraped from the detail page (richer than <see cref="MagicItemsSummary"/>).</summary>
    public List<DmSessionMagicItem>? MagicItems { get; set; }
}
