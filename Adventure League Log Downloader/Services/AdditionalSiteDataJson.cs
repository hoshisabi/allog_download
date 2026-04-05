using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Adventure_League_Log_Downloader.Services;

public static class AdditionalSiteDataJson
{
    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static string LocationsPath(string outputFolder) =>
        Path.Combine(outputFolder, "locations.json");

    public static string PlayerDmsPath(string outputFolder) =>
        Path.Combine(outputFolder, "player_dms.json");

    public static string CampaignsPath(string outputFolder) =>
        Path.Combine(outputFolder, "campaigns.json");

    public static async Task<LocationsFileDto?> TryLoadLocationsAsync(string outputFolder, CancellationToken ct = default)
    {
        var path = LocationsPath(outputFolder);
        try
        {
            if (!File.Exists(path))
                return null;
            await using var fs = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<LocationsFileDto>(fs, ReadOptions, ct);
        }
        catch
        {
            return null;
        }
    }

    public static async Task<PlayerDmsFileDto?> TryLoadPlayerDmsAsync(string outputFolder, CancellationToken ct = default)
    {
        var path = PlayerDmsPath(outputFolder);
        try
        {
            if (!File.Exists(path))
                return null;
            await using var fs = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<PlayerDmsFileDto>(fs, ReadOptions, ct);
        }
        catch
        {
            return null;
        }
    }

    public static async Task<CampaignsFileDto?> TryLoadCampaignsAsync(string outputFolder, CancellationToken ct = default)
    {
        var path = CampaignsPath(outputFolder);
        try
        {
            if (!File.Exists(path))
                return null;
            await using var fs = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<CampaignsFileDto>(fs, ReadOptions, ct);
        }
        catch
        {
            return null;
        }
    }

    public static async Task SaveLocationsAsync(string outputFolder, LocationsFileDto dto, CancellationToken ct = default)
    {
        Directory.CreateDirectory(outputFolder);
        await using var fs = File.Create(LocationsPath(outputFolder));
        await JsonSerializer.SerializeAsync(fs, dto, WriteOptions, ct);
    }

    public static async Task SavePlayerDmsAsync(string outputFolder, PlayerDmsFileDto dto, CancellationToken ct = default)
    {
        Directory.CreateDirectory(outputFolder);
        await using var fs = File.Create(PlayerDmsPath(outputFolder));
        await JsonSerializer.SerializeAsync(fs, dto, WriteOptions, ct);
    }

    public static async Task SaveCampaignsAsync(string outputFolder, CampaignsFileDto dto, CancellationToken ct = default)
    {
        Directory.CreateDirectory(outputFolder);
        await using var fs = File.Create(CampaignsPath(outputFolder));
        await JsonSerializer.SerializeAsync(fs, dto, WriteOptions, ct);
    }
}
