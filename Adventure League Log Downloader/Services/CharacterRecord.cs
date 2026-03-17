namespace Adventure_League_Log_Downloader.Services;

/// <summary>
/// Represents a single character entry as scraped from the Adventurers League character list.
/// Property names are PascalCase in C#, but will serialize to camelCase JSON.
/// </summary>
public sealed class CharacterRecord
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Race { get; set; } = string.Empty;
    public string Class { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string Season { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
}
