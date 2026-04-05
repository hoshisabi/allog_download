using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Adventure_League_Log_Downloader.Services;

public interface ISettingsService
{
    Task<UserSettings> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(UserSettings settings);
    string SettingsPath { get; }
}

/// <summary>
/// Persists <see cref="UserSettings"/> as JSON in the user's AppData folder.
/// </summary>
public sealed class SettingsService : ISettingsService
{
    private readonly string _appFolder;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public SettingsService(string? company = null, string? product = null)
    {
        // e.g., %AppData%\AllogDownloader
        var baseAppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var folderName = string.IsNullOrWhiteSpace(product) ? "AllogDownloader" : product;
        if (!string.IsNullOrWhiteSpace(company))
            _appFolder = Path.Combine(baseAppData, company, folderName);
        else
            _appFolder = Path.Combine(baseAppData, folderName);

        Directory.CreateDirectory(_appFolder);
        SettingsPath = Path.Combine(_appFolder, "settings.json");
    }

    public string SettingsPath { get; }

    public async Task<UserSettings> LoadAsync(CancellationToken ct = default)
    {
        try
        {
            if (!File.Exists(SettingsPath))
                return UserSettings.CreateDefaults();

            var text = await File.ReadAllTextAsync(SettingsPath, ct);
            using var doc = JsonDocument.Parse(text);
            var loaded = JsonSerializer.Deserialize<UserSettings>(text, _jsonOptions) ?? UserSettings.CreateDefaults();

            // Older settings files omit these keys; default is "only download missing" for both flows.
            if (!doc.RootElement.TryGetProperty("downloadOnlyMissingCharacterCsvs", out _))
                loaded.DownloadOnlyMissingCharacterCsvs = true;
            if (!doc.RootElement.TryGetProperty("downloadOnlyMissingDmSessionDetails", out _))
                loaded.DownloadOnlyMissingDmSessionDetails = true;

            return loaded;
        }
        catch
        {
            return UserSettings.CreateDefaults();
        }
    }

    public async Task SaveAsync(UserSettings settings)
    {
        Directory.CreateDirectory(_appFolder);
        await using var fs = File.Create(SettingsPath);
        await JsonSerializer.SerializeAsync(fs, settings, _jsonOptions);
    }
}
