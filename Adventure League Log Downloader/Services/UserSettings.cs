using System;

namespace Adventure_League_Log_Downloader.Services;

/// <summary>
/// User-scoped, non-sensitive settings we persist between runs.
/// </summary>
public sealed class UserSettings
{
    public double DelaySeconds { get; set; } = 0.25;
    public string OutputFolder { get; set; } = string.Empty;
    public string OutputFileName { get; set; } = "characters.json";

    public static UserSettings CreateDefaults()
    {
        return new UserSettings
        {
            DelaySeconds = 0.25,
            OutputFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            OutputFileName = "characters.json"
        };
    }
}
