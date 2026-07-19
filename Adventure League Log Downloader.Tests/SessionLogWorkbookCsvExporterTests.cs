using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Adventure_League_Log_Downloader.Services;

namespace Adventure_League_Log_Downloader.Tests;

public class SessionLogWorkbookCsvExporterTests
{
    private const string ExpectedHeader =
        "Name,Adv Name,Adv Code,DM Name,Gold,Magic Item Count,Magic Item Names,Notes," +
        "Level,Needs Update,Entry Type,Character Id,Session Date,Date DM Ran," +
        "XP,Downtime Days,Renown,Secret Missions,Session Length Hours,Location Played," +
        "Session Num,Campaign Id,DM DCI";

    [Fact]
    public async Task ExportAsync_SkipsCharactersWithNoCsv()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"allog_wb_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var jsonPath = Path.Combine(tempDir, "characters.json");
            File.WriteAllText(Path.Combine(tempDir, "character_abc.csv"), CsvTestFixtures.StandardCharacterCsv);

            CharacterRecord[] characters =
            [
                new() { Id = "abc", Name = "Alfie Allogson" },
                new() { Id = "xyz999", Name = "Zed Nope" },   // no CSV
            ];

            var result = await SessionLogWorkbookCsvExporter.ExportAsync(jsonPath, characters);

            Assert.Equal(1, result.CharactersUsed);
            Assert.Equal(1, result.CharactersSkippedNoCsv);
        }
        finally { Directory.Delete(tempDir, recursive: true); }
    }

    [Fact]
    public async Task ExportAsync_RowCountMatchesLogEntriesInCsv()
    {
        // Fixture has CharacterLogEntry + DmLogEntry = 2 rows.
        var tempDir = Path.Combine(Path.GetTempPath(), $"allog_wb_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var jsonPath = Path.Combine(tempDir, "characters.json");
            File.WriteAllText(Path.Combine(tempDir, "character_abc.csv"), CsvTestFixtures.StandardCharacterCsv);

            var result = await SessionLogWorkbookCsvExporter.ExportAsync(
                jsonPath, [new CharacterRecord { Id = "abc", Name = "Alfie Allogson" }]);

            Assert.Equal(2, result.RowCount);
        }
        finally { Directory.Delete(tempDir, recursive: true); }
    }

    [Fact]
    public async Task ExportAsync_OutputCsvHasCorrectHeaderRow()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"allog_wb_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var jsonPath = Path.Combine(tempDir, "characters.json");
            File.WriteAllText(Path.Combine(tempDir, "character_abc.csv"), CsvTestFixtures.StandardCharacterCsv);

            var result = await SessionLogWorkbookCsvExporter.ExportAsync(
                jsonPath, [new CharacterRecord { Id = "abc", Name = "Alfie Allogson" }]);

            var lines = File.ReadAllLines(result.OutputPath);
            Assert.Equal(ExpectedHeader, lines[0]);
        }
        finally { Directory.Delete(tempDir, recursive: true); }
    }

    [Fact]
    public async Task ExportAsync_ColumnMappingIsCorrect()
    {
        // Verifies that the CharacterLogEntry row has the right values in the right columns.
        var tempDir = Path.Combine(Path.GetTempPath(), $"allog_wb_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var jsonPath = Path.Combine(tempDir, "characters.json");
            File.WriteAllText(Path.Combine(tempDir, "character_abc.csv"), CsvTestFixtures.StandardCharacterCsv);

            var result = await SessionLogWorkbookCsvExporter.ExportAsync(
                jsonPath, [new CharacterRecord { Id = "abc", Name = "Alfie Allogson" }]);

            var lines = File.ReadAllLines(result.OutputPath);
            // lines[0] = header; lines[1] = CharacterLogEntry (sorted by name, only one character)
            var fields = lines[1].Split(',');

            // Header: Name(0),Adv Name(1),Adv Code(2),DM Name(3),Gold(4),Magic Item Count(5),
            //         Magic Item Names(6),Notes(7),Level(8),Needs Update(9),Entry Type(10),
            //         Character Id(11),Session Date(12),Date DM Ran(13),XP(14),...
            Assert.Equal("Alfie Allogson", fields[0]);
            Assert.Equal("Great Adventure", fields[1]);
            Assert.Equal("ADV-DD-01", fields[2]);
            Assert.Equal("Bob Smith", fields[3]);
            Assert.Equal("100", fields[4]);
            Assert.Equal("2", fields[5]);
            Assert.Equal("Ring of Jumping; Boots of Elvenkind", fields[6]);
            Assert.Equal("Good session", fields[7]);
            Assert.Equal("5", fields[8]);
            Assert.Equal("CharacterLogEntry", fields[10]);
            Assert.Equal("abc", fields[11]);
            Assert.Equal("300", fields[14]);

            // Dates are timezone-dependent; verify format only.
            Assert.Matches(@"^\d{4}-\d{2}-\d{2}$", fields[12]);  // Session Date
            Assert.Matches(@"^\d{4}-\d{2}-\d{2}$", fields[13]);  // Date DM Ran
        }
        finally { Directory.Delete(tempDir, recursive: true); }
    }
}
