namespace Adventure_League_Log_Downloader.Services;

public sealed class LocationRecord
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public sealed class PlayerDmRecord
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Dci { get; set; }
}

public sealed class DmCampaignListRow
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? PlayerCount { get; set; }
}

public sealed class CampaignPlayingListRow
{
    public string CampaignId { get; set; } = string.Empty;
    public string CampaignName { get; set; } = string.Empty;
    public string CharacterId { get; set; } = string.Empty;
    public string CharacterName { get; set; } = string.Empty;
}

public sealed class CampaignLogEntryRecord
{
    public string Id { get; set; } = string.Empty;
    public string? DatePlayed { get; set; }
    public string? AdventureTitle { get; set; }
    public string? SessionNum { get; set; }
    public string? LevelsOrXp { get; set; }
    public string? Gp { get; set; }
    public string? Downtime { get; set; }
    public string? MagicItems { get; set; }
    public string? Notes { get; set; }
}

public sealed class CampaignDetailRecord
{
    public string Name { get; set; } = string.Empty;
    public List<CampaignLogEntryRecord> LogEntries { get; set; } = new();
}

public sealed class LocationsFileDto
{
    public DateTimeOffset? FetchedAtUtc { get; set; }
    public List<LocationRecord> Locations { get; set; } = new();
}

public sealed class PlayerDmsFileDto
{
    public DateTimeOffset? FetchedAtUtc { get; set; }
    public List<PlayerDmRecord> PlayerDms { get; set; } = new();
}

public sealed class CampaignsFileDto
{
    public DateTimeOffset? FetchedAtUtc { get; set; }
    public List<DmCampaignListRow> DmCampaigns { get; set; } = new();
    public List<CampaignPlayingListRow> Playing { get; set; } = new();
    public Dictionary<string, CampaignDetailRecord> Details { get; set; } = new();
}

/// <summary>Flattened row for the additional-data window campaign summary grid.</summary>
public sealed class CampaignListPreviewRow
{
    public string Role { get; set; } = string.Empty;
    public string CampaignName { get; set; } = string.Empty;
    public string CampaignId { get; set; } = string.Empty;
    public string? Detail { get; set; }
}
