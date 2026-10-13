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

    /// <summary>1-based index during bulk CSV download (when <see cref="CsvCount"/> is set).</summary>
    public int? CsvIndex { get; init; }

    /// <summary>Total CSV files in the current bulk download batch.</summary>
    public int? CsvCount { get; init; }

    public int CharacterCount { get; init; }
    public IReadOnlyList<CharacterRecord> Characters { get; init; } = Array.Empty<CharacterRecord>();
    public string? Detail { get; init; }
}
