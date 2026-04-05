using System;
using System.IO;

namespace Adventure_League_Log_Downloader.Services;

/// <summary>
/// User-scoped, non-sensitive settings we persist between runs.
/// </summary>
public sealed class UserSettings
{
    public double DelaySeconds { get; set; } = 0.25;
    public string OutputFolder { get; set; } = string.Empty;
    public string OutputFileName { get; set; } = "characters.json";

    /// <summary>
    /// When a full site download (list + JSON + CSV pass) last completed successfully, UTC.
    /// </summary>
    public DateTimeOffset? LastWebsiteDownloadUtc { get; set; }

    /// <summary>
    /// When true, Download only refreshes the character list JSON and does not request per-character CSVs.
    /// </summary>
    public bool SkipCharacterCsvs { get; set; }

    /// <summary>
    /// When true (and <see cref="SkipCharacterCsvs"/> is false), bulk CSV download only runs for characters with no local CSV file.
    /// </summary>
    public bool DownloadOnlyMissingCharacterCsvs { get; set; }

    /// <summary>
    /// When true, a full DM session download only refreshes the list (paginated tables) and does not request per-session detail pages.
    /// </summary>
    public bool SkipDmSessionDetails { get; set; }

    /// <summary>
    /// When true (and <see cref="SkipDmSessionDetails"/> is false), detail fetch only runs for sessions not yet marked <c>detailFetched</c> in JSON.
    /// When false, all sessions are re-fetched from the site (full detail refresh).
    /// </summary>
    public bool DownloadOnlyMissingDmSessionDetails { get; set; }

    /// <summary>
    /// Default folder for exported JSON, aligned with <see cref="SettingsService"/> (%AppData%\AllogDownloader).
    /// </summary>
    public static string DefaultDataFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AllogDownloader");

    public static UserSettings CreateDefaults()
    {
        return new UserSettings
        {
            DelaySeconds = 0.25,
            OutputFolder = DefaultDataFolder,
            OutputFileName = "characters.json",
            DownloadOnlyMissingCharacterCsvs = true,
            DownloadOnlyMissingDmSessionDetails = true,
        };
    }
}
