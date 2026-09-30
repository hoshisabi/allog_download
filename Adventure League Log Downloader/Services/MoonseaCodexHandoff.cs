using System.Diagnostics;

namespace Adventure_League_Log_Downloader.Services;

/// <summary>
/// Hands a character CSV off to Moonsea Codex: selects the file in Explorer and opens the MSC characters page,
/// where the user runs "Import AL Log" and picks the file. No MSC credentials are stored by this app.
/// </summary>
public static class MoonseaCodexHandoff
{
    /// <summary>MSC characters page; its "Import AL Log" action accepts one adventurersleaguelog.com CSV per character.</summary>
    public const string ImportPageUrl = "https://moonseacodex.com/characters";

    /// <summary>Explorer window with <paramref name="csvPath"/> selected.</summary>
    public static ProcessStartInfo BuildRevealCsvStartInfo(string csvPath) => new()
    {
        FileName = "explorer.exe",
        Arguments = $"/select,\"{csvPath}\"",
        UseShellExecute = true
    };

    /// <summary>MSC import page in the default browser.</summary>
    public static ProcessStartInfo BuildImportPageStartInfo() => new(ImportPageUrl) { UseShellExecute = true };

    public static void Open(string csvPath)
    {
        Process.Start(BuildRevealCsvStartInfo(csvPath));
        Process.Start(BuildImportPageStartInfo());
    }
}
