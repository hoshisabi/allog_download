namespace Adventure_League_Log_Downloader.Services;

public enum DmSessionScrapePhase
{
    Idle,
    DiscoveringPages,
    ScrapingList,
    FetchingDetails,
    Saving,
    Complete,
    Error
}

/// <summary>
/// Progress payload for DM session scraping; safe to read on the UI thread.
/// </summary>
public sealed class DmSessionScrapeReport
{
    public DmSessionScrapePhase Phase { get; init; }
    public int? CurrentPage { get; init; }
    public int? TotalPages { get; init; }
    public int SessionCount { get; init; }

    /// <summary>How many detail pages have been fetched so far (during <see cref="DmSessionScrapePhase.FetchingDetails"/>).</summary>
    public int? DetailsFetched { get; init; }

    /// <summary>Total number of detail pages to fetch.</summary>
    public int? DetailsTotal { get; init; }

    public string? Detail { get; init; }
}
