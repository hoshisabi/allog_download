using System.Text.Json.Serialization;

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

    /// <summary>
    /// Latest <c>date_played</c> from the character CSV session log (set after CSV download / when loading from CSV on disk).
    /// </summary>
    public string? LastSessionPlayed { get; set; }

    /// <summary>
    /// UI-only: per-character CSV is present on disk next to the characters JSON (or legacy csv subfolder).
    /// </summary>
    [JsonIgnore]
    public bool HasLocalCsv { get; set; }
}
