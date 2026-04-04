using System;
using System.Collections.Generic;

namespace Adventure_League_Log_Downloader.Services;

public enum CharacterScrapePhase
{
    Idle,
    DiscoveringPages,
    Scraping,
    Saving,
    DownloadingCsvs,
    Complete,
    Error
}

/// <summary>
/// Progress payload for character list scraping; safe to read on the UI thread.
/// </summary>
public sealed class CharacterScrapeReport
{
    public CharacterScrapePhase Phase { get; init; }
    public int? CurrentPage { get; init; }
    public int? TotalPages { get; init; }
    public int CharacterCount { get; init; }
    public IReadOnlyList<CharacterRecord> Characters { get; init; } = Array.Empty<CharacterRecord>();
    public string? Detail { get; init; }
}
