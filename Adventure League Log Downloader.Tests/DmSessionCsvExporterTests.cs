using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Adventure_League_Log_Downloader.Services;
using CsvHelper;
using CsvHelper.Configuration;

namespace Adventure_League_Log_Downloader.Tests;

public class DmSessionCsvExporterTests
{
    private const string ExpectedHeader =
        "Id,Date Dmed,Adventure Title,Session Num,DM Reward Choice,Character Name,Character Id," +
        "Detail Fetched,Session Length Hours,Player Level,Location Played,XP Gained,GP Gained," +
        "Downtime Gained,Renown Gained,Secret Missions,Date Played,Notes,Magic Items";

    private static DmSessionRecord SampleSession() => new()
    {
        Id = "42",
        DateDmed = "2026-01-15",
        AdventureTitle = "DDAL01-01 The Mission, Part 1",
        SessionNum = "3",
        DmRewardChoice = "Magic Item",
        CharacterName = "Alfie Allogson",
        CharacterId = "abc",
        DetailFetched = true,
        SessionLengthHours = "4",
        PlayerLevel = "5",
        LocationPlayed = "Home",
        XpGained = "4500",
        GpGained = "250",
        DowntimeGained = "10",
        RenownGained = "1",
        NumSecretMissions = "0",
        DatePlayed = "2026-01-16",
        Notes = "First line,\nsecond line",
        MagicItems =
        [
            new DmSessionMagicItem { Name = "Cloak of Protection", Rarity = "Uncommon" },
            new DmSessionMagicItem { Name = "Potion of Healing" },
        ],
    };

    [Fact]
    public async Task ExportAsync_WritesHeaderAndOneRowPerSession()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"allog_dmcsv_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var result = await DmSessionCsvExporter.ExportAsync(
                tempDir, [SampleSession()], TestContext.Current.CancellationToken);

            Assert.Equal(Path.Combine(tempDir, "dm_sessions.csv"), result.OutputPath);
            Assert.Equal(1, result.RowCount);

            var rows = ReadRows(result.OutputPath);
            Assert.Equal(ExpectedHeader, string.Join(",", rows[0]));
            Assert.Equal(2, rows.Count);
            var row = rows[1];
            Assert.Equal("42", row[0]);
            Assert.Equal("DDAL01-01 The Mission, Part 1", row[2]);
            Assert.Equal("true", row[7]);
            Assert.Equal("First line,\nsecond line", row[17].Replace("\r\n", "\n"));
            Assert.Equal("Cloak of Protection; Potion of Healing", row[18]);
        }
        finally { Directory.Delete(tempDir, recursive: true); }
    }

    [Fact]
    public async Task ExportAsync_SortsMostRecentFirstAndFallsBackToMagicItemsSummary()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"allog_dmcsv_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var older = new DmSessionRecord { Id = "1", DateDmed = "2021-10-23 14:00", MagicItemsSummary = "Bag of Holding" };
            var newer = new DmSessionRecord { Id = "2", DateDmed = "2021-11-03 19:00" };

            var result = await DmSessionCsvExporter.ExportAsync(
                tempDir, [older, newer], TestContext.Current.CancellationToken);

            var rows = ReadRows(result.OutputPath);
            Assert.Equal("2", rows[1][0]);
            Assert.Equal("1", rows[2][0]);
            Assert.Equal("Bag of Holding", rows[2][18]);
            Assert.Equal("", rows[1][18]);
        }
        finally { Directory.Delete(tempDir, recursive: true); }
    }

    private static List<string[]> ReadRows(string path)
    {
        using var reader = new StreamReader(path);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = false });
        var rows = new List<string[]>();
        while (csv.Read())
            rows.Add(csv.Parser.Record!);
        return rows;
    }

    [Fact]
    public async Task ExportAsync_QuotesFieldsWithCommasAndNewlines()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"allog_dmcsv_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var result = await DmSessionCsvExporter.ExportAsync(
                tempDir, [SampleSession()], TestContext.Current.CancellationToken);

            var text = File.ReadAllText(result.OutputPath);
            Assert.Contains("\"DDAL01-01 The Mission, Part 1\"", text);
            Assert.Contains("\"First line,\nsecond line\"", text.Replace("\r\n", "\n"));
            Assert.Contains("Cloak of Protection; Potion of Healing", text);
        }
        finally { Directory.Delete(tempDir, recursive: true); }
    }

    [Fact]
    public async Task ExportAsync_EmptyListWritesHeaderOnly()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"allog_dmcsv_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var result = await DmSessionCsvExporter.ExportAsync(
                tempDir, [], TestContext.Current.CancellationToken);

            Assert.Equal(0, result.RowCount);
            Assert.Equal(ExpectedHeader, File.ReadAllText(result.OutputPath).Trim());
        }
        finally { Directory.Delete(tempDir, recursive: true); }
    }
}
