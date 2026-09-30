using System.Diagnostics;

namespace Adventure_League_Log_Downloader.Services;

/// <summary>
/// Hands a character off to Moonsea Codex: writes an MSC-ready copy of the character CSV to Downloads
/// (<see cref="MoonseaCodexCsvWriter"/>), selects it in Explorer, and opens the MSC characters page, where the user clicks
/// Import and drops in the file. No MSC credentials are stored by this app.
/// </summary>
public static class MoonseaCodexHandoff
{
    /// <summary>
    /// MSC characters page; its Import dialog accepts one adventurersleaguelog.com CSV per character and creates a new MSC character.
    /// </summary>
    public const string ImportPageUrl = "https://moonseacodex.com/characters";

    /// <summary>Visibility of the imported MSC character until this becomes a user setting.</summary>
    public const bool DefaultMakePublic = false;

    /// <summary>Explorer window with <paramref name="csvPath"/> selected.</summary>
    public static ProcessStartInfo BuildRevealCsvStartInfo(string csvPath) => new()
    {
        FileName = "explorer.exe",
        Arguments = $"/select,\"{csvPath}\"",
        UseShellExecute = true
    };

    /// <summary>MSC import page in the default browser.</summary>
    public static ProcessStartInfo BuildImportPageStartInfo() => new(ImportPageUrl) { UseShellExecute = true };

    /// <summary>Writes the MSC import file for <paramref name="siteCsvPath"/>, reveals it, and opens MSC. Returns the path written.</summary>
    public static string Open(string siteCsvPath, bool makePublic = DefaultMakePublic)
    {
        var importPath = MoonseaCodexCsvWriter.WriteImportFile(siteCsvPath, DownloadsFolder.GetPath(), makePublic);
        Process.Start(BuildRevealCsvStartInfo(importPath));
        Process.Start(BuildImportPageStartInfo());
        return importPath;
    }
}
