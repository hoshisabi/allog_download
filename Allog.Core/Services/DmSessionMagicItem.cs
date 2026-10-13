namespace Adventure_League_Log_Downloader.Services;

/// <summary>
/// A magic item gained during a DM session, as scraped from the session detail page.
/// </summary>
public sealed class DmSessionMagicItem
{
    public string Name { get; set; } = string.Empty;
    public string? Character { get; set; }
    public string? Rarity { get; set; }
    public string? LocationFound { get; set; }
    public string? Table { get; set; }
    public string? TableResult { get; set; }
    public bool CountsTowardLimit { get; set; }
}
