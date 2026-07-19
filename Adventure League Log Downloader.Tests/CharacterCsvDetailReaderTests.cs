using System;
using System.Globalization;
using System.IO;
using Adventure_League_Log_Downloader.Services;

namespace Adventure_League_Log_Downloader.Tests;

public class CharacterCsvDetailReaderTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryLoad_NullOrEmptyPath_ReturnsCsvNotFound(string? path)
    {
        var result = CharacterCsvDetailReader.TryLoad(path);
        Assert.False(result.CsvFileFound);
        Assert.Empty(result.Sessions);
    }

    [Fact]
    public void TryLoad_MissingFile_ReturnsCsvNotFound()
    {
        var result = CharacterCsvDetailReader.TryLoad(
            Path.Combine(Path.GetTempPath(), "does_not_exist_allog_detail.csv"));
        Assert.False(result.CsvFileFound);
    }

    [Fact]
    public void TryLoad_ValidFixture_SetsCsvFileFound()
    {
        var path = CsvTestFixtures.WriteTempCsv(CsvTestFixtures.StandardCharacterCsv);
        try
        {
            var result = CharacterCsvDetailReader.TryLoad(path);
            Assert.True(result.CsvFileFound);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void TryLoad_ValidFixture_ParsesTwoSessions()
    {
        var path = CsvTestFixtures.WriteTempCsv(CsvTestFixtures.StandardCharacterCsv);
        try
        {
            var result = CharacterCsvDetailReader.TryLoad(path);
            Assert.Equal(2, result.Sessions.Count);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void TryLoad_ValidFixture_ParsesCharacterLogEntryFields()
    {
        var path = CsvTestFixtures.WriteTempCsv(CsvTestFixtures.StandardCharacterCsv);
        try
        {
            var session = CharacterCsvDetailReader.TryLoad(path).Sessions[0];

            Assert.Equal("CharacterLogEntry", session.EntryType);
            Assert.Equal("ADV-DD-01", session.AdventureCode);
            Assert.Equal("Great Adventure", session.AdventureTitle);
            Assert.Equal("Bob Smith", session.DmName);
            Assert.Equal("100", session.Gold);
            Assert.Equal("300", session.Xp);
            Assert.Equal("5", session.PlayerLevel);
            Assert.Equal("Good session", session.Notes);

            // Dates convert UTC → local; compute expected the same way the code does.
            var expectedPlayed = new DateTime(2023, 5, 15, 12, 0, 0, DateTimeKind.Utc)
                .ToLocalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var expectedDmRan = new DateTime(2023, 5, 16, 12, 0, 0, DateTimeKind.Utc)
                .ToLocalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            Assert.Equal(expectedPlayed, session.DatePlayed);
            Assert.Equal(expectedDmRan, session.DateDmRan);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void TryLoad_ValidFixture_GroupsMagicItemsUnderCorrectSession()
    {
        var path = CsvTestFixtures.WriteTempCsv(CsvTestFixtures.StandardCharacterCsv);
        try
        {
            var sessions = CharacterCsvDetailReader.TryLoad(path).Sessions;

            // Magic items follow the CharacterLogEntry (session 0); DmLogEntry (session 1) has none.
            Assert.Equal(["Ring of Jumping", "Boots of Elvenkind"], sessions[0].MagicItems);
            Assert.Empty(sessions[1].MagicItems);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void TryLoad_ValidFixture_MagicItemHeaderRowSkipped()
    {
        // The "MAGIC ITEM,name" row is a section header and must not appear in MagicItems.
        var path = CsvTestFixtures.WriteTempCsv(CsvTestFixtures.StandardCharacterCsv);
        try
        {
            var items = CharacterCsvDetailReader.TryLoad(path).Sessions[0].MagicItems;
            Assert.DoesNotContain("name", items);
        }
        finally { File.Delete(path); }
    }
}
