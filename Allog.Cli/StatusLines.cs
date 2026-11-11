using Adventure_League_Log_Downloader.Services;

namespace Allog.Cli;

/// <summary>
/// Writes progress messages. On a terminal, messages from the same step replace each other on one line ("Page 3 of 4",
/// "Downloading character CSVs: 12 of 65"); a new step starts a new line. Otherwise (a pipe, a log file), one line each.
/// </summary>
internal sealed class StatusLines(TextWriter writer, bool terminal) : IProgress<CharacterScrapeReport>
{
    private string? _lastMessage;
    private CharacterScrapePhase? _lastPhase;
    private int _openLineLength;

    public void Report(CharacterScrapeReport value)
    {
        if (string.IsNullOrWhiteSpace(value.Detail) || value.Detail == _lastMessage)
            return;
        _lastMessage = value.Detail;

        var text = "  " + value.Detail;
        if (!terminal)
        {
            writer.WriteLine(text);
            return;
        }

        if (_openLineLength > 0 && value.Phase == _lastPhase)
            writer.Write("\r" + text.PadRight(_openLineLength));
        else
        {
            Finish();
            writer.Write(text);
        }

        _openLineLength = Math.Max(_openLineLength, text.Length);
        _lastPhase = value.Phase;
    }

    /// <summary>Ends a line left open for in-place updates, so the next output starts on its own line.</summary>
    public void Finish()
    {
        if (_openLineLength == 0)
            return;
        writer.WriteLine();
        _openLineLength = 0;
    }
}
