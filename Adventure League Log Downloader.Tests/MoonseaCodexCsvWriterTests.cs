using System;
using System.IO;
using System.Linq;
using Adventure_League_Log_Downloader.Services;

namespace Adventure_League_Log_Downloader.Tests;

public class MoonseaCodexCsvWriterTests : IDisposable
{
    // Row shapes taken from a real export that MSC partly dropped: multi-line notes whose first line has no comma,
    // multi-line notes with commas, empty quoted fields, a magic item header row, and a purchase row.
    internal const string SiteCsv = """
        name,race,class_and_levels,faction,background,lifestyle,portrait_url,publicly_visible
        Scribinator-3000,autognome,Wizard-1,,"",,https://example.com/avatar.jpeg?width=155&height=255,false
        type,adventure_title,session_num,date_played,session_length_hours,player_level,xp_gained,gp_gained,downtime_gained,renown_gained,num_secret_missions,location_played,dm_name,dm_dci_number,notes,date_dmed,campaign_id
        MAGIC ITEM,name,rarity,location_found,table,table_result,notes
        CharacterLogEntry,SJ-DC-PAT-00 The Moonshot,1,2023-01-17 20:00:00 UTC,,,,55.0,10.0,,,Roll20,Bryan Mets,1,"",,
        CharacterLogEntry,SJ-DC-ISL-01 Speck in the Sky,1,2023-01-31 19:30:00 UTC,,,,57.0,10.0,,,Roll20,Bryan Mets,1,"gained costume of a fleshling
        gained fishing gear
        packet of smoke powder",,
        MAGIC ITEM,Javelin of Lightning,uncommon,SJ-DC-ISL-01 Speck in the Sky,"","",""
        CharacterLogEntry,SJ-DC-BST-01 Slammed in the Asteroidbow,1,2023-02-21 19:30:00 UTC,,,,415.0,10.0,,,Roll20,Bryan Mets,1,"Astromancy Archive (with warleader), smokepowder
        mundane items: 1 pistol, 1 ea fishing tackle",,
        PurchaseLogEntry,,,2024-01-20 00:00:00 UTC,,,,-125.0,,,,,,,Bought scroll of endure elements,,
        """;

    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), $"allog_msc_test_{Guid.NewGuid():N}");

    public MoonseaCodexCsvWriterTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private static string[] ConvertLines(bool makePublic = false) =>
        MoonseaCodexCsvWriter.Convert(SiteCsv, makePublic).Split('\n', StringSplitOptions.RemoveEmptyEntries);

    [Fact]
    public void Convert_KeepsHeaderLinesVerbatim()
    {
        var lines = ConvertLines();

        Assert.Equal(MoonseaCodexCsvWriter.ExpectedCharacterHeader, lines[0]);
        Assert.Equal(MoonseaCodexCsvWriter.ExpectedEventHeader, lines[2]);
    }

    [Fact]
    public void Convert_EveryRecordIsOneLineWithAllFieldsWhenSplitOnCommas()
    {
        var lines = ConvertLines();

        // 3 header/character lines + magic item header + 3 sessions + 1 item + 1 purchase
        Assert.Equal(9, lines.Length);
        foreach (var line in lines.Where(l => l.StartsWith("CharacterLogEntry,")))
            Assert.Equal(17, line.Split(',').Length);
        Assert.Equal(8, lines[1].Split(',').Length);
    }

    [Fact]
    public void Convert_MultiLineNotesWithoutCommaSurviveAsOneField()
    {
        var speck = ConvertLines().Single(l => l.Contains("Speck in the Sky") && l.StartsWith("CharacterLogEntry"));
        var fields = speck.Split(',');

        Assert.Equal("57.0", fields[7]);
        Assert.Equal("gained costume of a fleshling gained fishing gear packet of smoke powder", fields[14]);
    }

    [Fact]
    public void Convert_CommasInsideFieldsBecomeSemicolons()
    {
        var bst = ConvertLines().Single(l => l.Contains("SJ-DC-BST-01"));

        Assert.Equal(
            "Astromancy Archive (with warleader); smokepowder mundane items: 1 pistol; 1 ea fishing tackle",
            bst.Split(',')[14]);
    }

    [Fact]
    public void Convert_EmptyQuotedFieldsBecomeEmpty()
    {
        var moonshot = ConvertLines().Single(l => l.Contains("The Moonshot"));

        Assert.Equal(string.Empty, moonshot.Split(',')[14]);
        Assert.DoesNotContain("\"", moonshot);
    }

    [Fact]
    public void Convert_PrivateByDefault_WritesEmptyPubliclyVisible()
    {
        Assert.Equal(string.Empty, ConvertLines()[1].Split(',')[7]);
    }

    [Fact]
    public void Convert_MakePublic_WritesTrue()
    {
        Assert.Equal("true", ConvertLines(makePublic: true)[1].Split(',')[7]);
    }

    [Fact]
    public void Convert_UnexpectedHeader_Throws()
    {
        Assert.Throws<InvalidDataException>(() => MoonseaCodexCsvWriter.Convert(CsvTestFixtures.StandardCharacterCsv, false));
    }

    [Fact]
    public void WriteImportFile_UsesBaseNameThenNumbersAndWritesNoBom()
    {
        var source = Path.Combine(_tempDir, "character_1.csv");
        File.WriteAllText(source, SiteCsv);

        var first = MoonseaCodexCsvWriter.WriteImportFile(source, _tempDir, makePublic: false);
        var second = MoonseaCodexCsvWriter.WriteImportFile(source, _tempDir, makePublic: false);
        var third = MoonseaCodexCsvWriter.WriteImportFile(source, _tempDir, makePublic: false);

        Assert.Equal("moonseacodeximport.csv", Path.GetFileName(first));
        Assert.Equal("moonseacodeximport (1).csv", Path.GetFileName(second));
        Assert.Equal("moonseacodeximport (2).csv", Path.GetFileName(third));
        Assert.Equal((byte)'n', File.ReadAllBytes(first)[0]);
    }
}
