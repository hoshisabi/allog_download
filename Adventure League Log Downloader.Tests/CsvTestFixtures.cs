using System;
using System.IO;

namespace Adventure_League_Log_Downloader.Tests;

/// <summary>
/// Shared CSV fixture used by CharacterLogCsvReader, CharacterCsvDetailReader, and SessionLogWorkbookCsvExporter tests.
/// Column order is chosen so that date_played is at index 3 and date_dmed at index 15 (matching CharacterLogCsvReader's
/// positional assumptions), while the named-column readers use the "type" header row to build their own map.
/// Dates use 12:00 UTC to stay on the same calendar day across all US timezones.
/// </summary>
internal static class CsvTestFixtures
{
    internal const string StandardCharacterCsv = """
        name
        Alfie Allogson
        type,adventure_title,session_name,date_played,gp_gained,xp_gained,player_level,dm_name,notes,location_played,session_length_hours,session_num,campaign_id,renown_gained,downtime_gained,date_dmed,num_secret_missions,dm_dci_number
        CharacterLogEntry,ADV-DD-01 Great Adventure,Session 1,2023-05-15 12:00:00 UTC,100,300,5,Bob Smith,Good session,Local Store,4,1,CAMP1,1,5,2023-05-16 12:00:00 UTC,0,12345
        MAGIC ITEM,name
        MAGIC ITEM,Ring of Jumping
        MAGIC ITEM,Boots of Elvenkind
        DmLogEntry,ADV-DD-02 DM Adventure,,,0,0,,Jane DM,,,,,,,,2023-06-20 12:00:00 UTC,,
        """;

    internal static string WriteTempCsv(string content)
    {
        var path = Path.Combine(Path.GetTempPath(), $"allog_test_{Guid.NewGuid():N}.csv");
        File.WriteAllText(path, content);
        return path;
    }
}
