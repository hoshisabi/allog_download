using System;
using System.Globalization;
using System.IO;
using Adventure_League_Log_Downloader.Services;

namespace Adventure_League_Log_Downloader.Tests;

public class CharacterLogCsvReaderTests
{
    [Fact]
    public void TryParseSiteCsvTimestamp_SiteUtcFormat_ParsesCorrectly()
    {
        var ok = CharacterLogCsvReader.TryParseSiteCsvTimestamp("2023-05-15 12:00:00 UTC", out var dt);
        Assert.True(ok);
        Assert.Equal(DateTimeKind.Utc, dt.Kind);
        Assert.Equal(new DateTime(2023, 5, 15, 12, 0, 0, DateTimeKind.Utc), dt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryParseSiteCsvTimestamp_EmptyOrNull_ReturnsFalse(string? input)
    {
        Assert.False(CharacterLogCsvReader.TryParseSiteCsvTimestamp(input, out _));
    }

    [Fact]
    public void FormatSiteDateForWorkbook_LocalKind_ReturnsDateUnchanged()
    {
        // Kind.Local bypasses ToLocalTime() conversion, giving a deterministic result.
        var dt = new DateTime(2023, 5, 15, 0, 0, 0, DateTimeKind.Local);
        Assert.Equal("2023-05-15", CharacterLogCsvReader.FormatSiteDateForWorkbook(dt));
    }

    [Fact]
    public void FormatSiteDateForWorkbook_ReturnsIsoDateFormat()
    {
        var dt = new DateTime(2023, 5, 15, 12, 0, 0, DateTimeKind.Utc);
        var result = CharacterLogCsvReader.FormatSiteDateForWorkbook(dt);
        Assert.Matches(@"^\d{4}-\d{2}-\d{2}$", result);
    }

    [Fact]
    public void TryGetLatestSessionDatePlayed_MissingFile_ReturnsNull()
    {
        var result = CharacterLogCsvReader.TryGetLatestSessionDatePlayed(
            Path.Combine(Path.GetTempPath(), "does_not_exist_allog.csv"));
        Assert.Null(result);
    }

    [Fact]
    public void TryGetLatestSessionDatePlayed_PicksMaxAcrossAllRows()
    {
        // Fixture: CharacterLogEntry date_dmed=2023-05-16, DmLogEntry date_dmed=2023-06-20 (at index 15).
        // Expected max: 2023-06-20 12:00 UTC (the DM entry's date_dmed).
        var path = CsvTestFixtures.WriteTempCsv(CsvTestFixtures.StandardCharacterCsv);
        try
        {
            var max = CharacterLogCsvReader.TryGetLatestSessionDatePlayed(path);
            Assert.NotNull(max);
            Assert.Equal(new DateTime(2023, 6, 20, 12, 0, 0, DateTimeKind.Utc), max.Value);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
