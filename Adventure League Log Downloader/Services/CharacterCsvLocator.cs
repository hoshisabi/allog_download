using System;
using System.IO;

namespace Adventure_League_Log_Downloader.Services;

/// <summary>
/// Resolves per-character CSV paths relative to the configured characters JSON file.
/// Current app and Python both use <c>{jsonDir}/character_{id}.csv</c>. Older WPF builds used <c>{jsonDir}/csv/</c> — still supported when reading.
/// </summary>
public static class CharacterCsvLocator
{
    /// <summary>
    /// Finds an existing CSV for the character, or null. Prefers the same folder as the JSON, then a legacy <c>csv</c> subfolder.
    /// </summary>
    public static string? TryFindCharacterCsvFile(string charactersJsonPath, string characterId)
    {
        if (string.IsNullOrWhiteSpace(charactersJsonPath) || string.IsNullOrWhiteSpace(characterId))
            return null;

        string jsonDir;
        try
        {
            var fullJson = Path.GetFullPath(charactersJsonPath);
            jsonDir = Path.GetDirectoryName(fullJson) ?? string.Empty;
        }
        catch
        {
            return null;
        }

        if (string.IsNullOrEmpty(jsonDir))
            return null;

        var fileName = $"character_{characterId}.csv";

        var alongsideJson = Path.Combine(jsonDir, fileName);
        if (File.Exists(alongsideJson))
            return alongsideJson;

        var legacyCsvSubfolder = Path.Combine(jsonDir, "csv", fileName);
        if (File.Exists(legacyCsvSubfolder))
            return legacyCsvSubfolder;

        return null;
    }
}
