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
            OutputFileName = "characters.json"
        };
    }
}
