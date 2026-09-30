using System.Linq;
using Adventure_League_Log_Downloader.Services;

namespace Adventure_League_Log_Downloader.Tests;

/// <summary>
/// Runs site CSVs and <see cref="MoonseaCodexCsvWriter"/> output through <see cref="MoonseaCodexImportEmulator"/> to check
/// what Moonsea Codex would create.
/// </summary>
public class MoonseaCodexImportTests
{
    private const string SiteCsv = MoonseaCodexCsvWriterTests.SiteCsv;

    [Fact]
    public void Emulator_SplitLines_MatchesPythonSplitlines()
    {
        Assert.Equal(
            new[] { "a", "b", "c", "", "d" },
            MoonseaCodexImportEmulator.SplitLines("a\r\nb\u2028c\n\u000Cd\n"));
    }

    [Fact]
    public void SiteCsvAsIs_DropsSessionWithMultiLineNotesAndMarksPublic()
    {
        // Matches a real import: rows whose notes span lines without a comma on the first line are skipped,
        // and publicly_visible "false" counts as public.
        var character = MoonseaCodexImportEmulator.Import(SiteCsv);

        Assert.Equal(new[] { "SJ-DC-PAT-00", "SJ-DC-BST-01" }, character.Games.Select(g => g.Module));
        Assert.True(character.Public);
    }

    [Fact]
    public void Converted_ImportsEverySessionWithFullNotes()
    {
        var character = MoonseaCodexImportEmulator.Import(MoonseaCodexCsvWriter.Convert(SiteCsv, makePublic: false));

        Assert.Equal("Scribinator-3000", character.Name);
        Assert.Equal(new[] { "SJ-DC-PAT-00", "SJ-DC-ISL-01", "SJ-DC-BST-01" }, character.Games.Select(g => g.Module));

        var speck = character.Games[1];
        Assert.Equal("Speck in the Sky", speck.Name);
        Assert.Equal(57, speck.Gold);
        Assert.Equal(10, speck.Downtime);
        Assert.Equal("gained costume of a fleshling gained fishing gear packet of smoke powder", speck.Notes);
        Assert.Equal(string.Empty, character.Games[0].Notes);
    }

    [Fact]
    public void Converted_KeepsMagicItems()
    {
        var character = MoonseaCodexImportEmulator.Import(MoonseaCodexCsvWriter.Convert(SiteCsv, makePublic: false));

        Assert.Equal(new[] { new MoonseaCodexImportEmulator.Item("Javelin of Lightning", "uncommon") }, character.Items);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Converted_VisibilityFollowsChoice(bool makePublic)
    {
        var character = MoonseaCodexImportEmulator.Import(MoonseaCodexCsvWriter.Convert(SiteCsv, makePublic));

        Assert.Equal(makePublic, character.Public);
    }

    [Fact]
    public void Converted_NotesWithOtherPythonLineBreaksStayOnOneRow()
    {
        var csv = SiteCsv.Replace("gained fishing gear", "gained\u2028fishing\u000Cgear\u0085today");

        var character = MoonseaCodexImportEmulator.Import(MoonseaCodexCsvWriter.Convert(csv, makePublic: false));

        var speck = Assert.Single(character.Games, g => g.Module == "SJ-DC-ISL-01");
        Assert.Equal("gained costume of a fleshling gained fishing gear today packet of smoke powder", speck.Notes);
    }
}
