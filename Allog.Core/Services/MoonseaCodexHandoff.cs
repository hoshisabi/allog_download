namespace Adventure_League_Log_Downloader.Services;

/// <summary>
/// Hands a character off to Moonsea Codex: writes an MSC-ready copy of the character CSV to Downloads
/// (<see cref="MoonseaCodexCsvWriter"/>), reveals it in the file manager, and opens the MSC characters page, where the user
/// clicks Import and drops in the file. No MSC credentials are stored by this app.
/// </summary>
public static class MoonseaCodexHandoff
{
    /// <summary>
    /// MSC characters page; its Import dialog accepts one adventurersleaguelog.com CSV per character and creates a new MSC character.
    /// </summary>
    public const string ImportPageUrl = "https://moonseacodex.com/characters";

    /// <summary>Visibility of the imported MSC character until this becomes a user setting.</summary>
    public const bool DefaultMakePublic = false;

    /// <summary>Writes the MSC import file for <paramref name="siteCsvPath"/>, reveals it, and opens MSC. Returns the path written.</summary>
    public static string Open(IPlatformShell shell, string siteCsvPath, bool makePublic = DefaultMakePublic)
    {
        var importPath = MoonseaCodexCsvWriter.WriteImportFile(siteCsvPath, shell.GetDownloadsFolder(), makePublic);
        shell.RevealFile(importPath);
        shell.OpenUrl(ImportPageUrl);
        return importPath;
    }
}
