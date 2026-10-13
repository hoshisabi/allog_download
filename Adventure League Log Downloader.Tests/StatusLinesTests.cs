using System.IO;
using Adventure_League_Log_Downloader.Services;
using Allog.Cli;

namespace Adventure_League_Log_Downloader.Tests;

public class StatusLinesTests
{
    private static CharacterScrapeReport Report(CharacterScrapePhase phase, string detail) => new()
    {
        Phase = phase,
        CharacterCount = 0,
        Characters = [],
        Detail = detail
    };

    private static string Run(bool terminal)
    {
        var writer = new StringWriter { NewLine = "\n" };
        var lines = new StatusLines(writer, terminal);
        lines.Report(Report(CharacterScrapePhase.Scraping, "Page 1 of 2: loaded 20 character(s) so far."));
        lines.Report(Report(CharacterScrapePhase.Scraping, "Page 2 of 2: loaded 25 character(s) so far."));
        lines.Report(Report(CharacterScrapePhase.DownloadingCsvs, "Downloading character CSVs: 1 of 25…"));
        lines.Report(Report(CharacterScrapePhase.DownloadingCsvs, "Downloading character CSVs: 1 of 25…"));
        lines.Report(Report(CharacterScrapePhase.DownloadingCsvs, "Downloading character CSVs: 2 of 25…"));
        lines.Report(Report(CharacterScrapePhase.Saving, "Saving character list…"));
        lines.Finish();
        return writer.ToString();
    }

    [Fact]
    public void NotATerminal_OneLinePerMessage_DuplicatesDropped()
    {
        Assert.Equal(
            """
              Page 1 of 2: loaded 20 character(s) so far.
              Page 2 of 2: loaded 25 character(s) so far.
              Downloading character CSVs: 1 of 25…
              Downloading character CSVs: 2 of 25…
              Saving character list…

            """.Replace("\r\n", "\n"),
            Run(terminal: false));
    }

    [Fact]
    public void Terminal_SameStepRewritesTheLine_NewStepStartsANewLine()
    {
        Assert.Equal(
            "  Page 1 of 2: loaded 20 character(s) so far."
            + "\r  Page 2 of 2: loaded 25 character(s) so far.\n"
            + "  Downloading character CSVs: 1 of 25…"
            + "\r  Downloading character CSVs: 2 of 25…\n"
            + "  Saving character list…\n",
            Run(terminal: true));
    }

    [Fact]
    public void Terminal_ShorterMessagePadsOverTheLongerOne()
    {
        var writer = new StringWriter { NewLine = "\n" };
        var lines = new StatusLines(writer, terminal: true);
        lines.Report(Report(CharacterScrapePhase.Scraping, "a much longer message"));
        lines.Report(Report(CharacterScrapePhase.Scraping, "short"));

        Assert.Equal("  a much longer message\r  short                ", writer.ToString());
    }

    [Fact]
    public void Finish_WithNothingWritten_WritesNothing()
    {
        var writer = new StringWriter();
        new StatusLines(writer, terminal: true).Finish();

        Assert.Equal(string.Empty, writer.ToString());
    }
}
