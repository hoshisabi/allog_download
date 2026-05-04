using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CsvHelper;
using CsvHelper.Configuration;

namespace Adventure_League_Log_Downloader.Services;

/// <summary>
/// Reads session rows and magic items from a per-character site CSV for UI display.
/// Parsing aligns with <see cref="SessionLogWorkbookCsvExporter"/>.
/// </summary>
public static class CharacterCsvDetailReader
{
    private static readonly CsvConfiguration ReaderConfig = new(CultureInfo.InvariantCulture)
    {
        HasHeaderRecord = false,
        BadDataFound = null,
        MissingFieldFound = null,
    };

    private static readonly Regex AdventureLeadingCode = new(
        @"^(?<code>[A-Z][A-Z0-9]*(?:-[A-Za-z0-9{}]+)+)\s+(?<rest>.+)$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public sealed class Result
    {
        public bool CsvFileFound { get; init; }
        public IReadOnlyList<CharacterSessionDetailRow> Sessions { get; init; } = Array.Empty<CharacterSessionDetailRow>();
    }

    /// <summary>
    /// Returns sessions (log entries) with magic items grouped under each session, in file order.
    /// </summary>
    public static Result TryLoad(string? csvPath)
    {
        if (string.IsNullOrWhiteSpace(csvPath) || !File.Exists(csvPath))
            return new Result { CsvFileFound = false };

        try
        {
            using var stream = File.OpenRead(csvPath);
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            using var csv = new CsvReader(reader, ReaderConfig);

            Dictionary<string, int>? logColumns = null;
            var nextRowIsBannerName = false;
            CharacterSessionDetailRow? current = null;
            var sessions = new List<CharacterSessionDetailRow>();

            while (csv.Read())
            {
                var count = csv.ColumnCount;
                if (count < 1)
                    continue;

                var mark0 = Normalize(csv.GetField(0));

                if (logColumns == null)
                {
                    if (mark0.Equals("name", StringComparison.OrdinalIgnoreCase))
                    {
                        nextRowIsBannerName = true;
                        continue;
                    }

                    if (nextRowIsBannerName)
                    {
                        nextRowIsBannerName = false;
                        continue;
                    }

                    if (mark0.Equals("type", StringComparison.OrdinalIgnoreCase))
                        logColumns = ReadHeaderMap(csv, count);

                    continue;
                }

                if (mark0.Equals("MAGIC ITEM", StringComparison.OrdinalIgnoreCase))
                {
                    var nameCell = count > 1 ? Normalize(csv.GetField(1)) : string.Empty;
                    if (nameCell.Equals("name", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (nameCell.Length > 0 && current != null)
                        current.MagicItems.Add(nameCell);

                    continue;
                }

                if (mark0.EndsWith("LogEntry", StringComparison.Ordinal))
                {
                    current = CreateSession(csv, count, logColumns, mark0);
                    sessions.Add(current);
                }
            }

            return new Result { CsvFileFound = true, Sessions = sessions };
        }
        catch
        {
            return new Result { CsvFileFound = true, Sessions = Array.Empty<CharacterSessionDetailRow>() };
        }
    }

    private static CharacterSessionDetailRow CreateSession(
        CsvReader csv,
        int count,
        Dictionary<string, int> ix,
        string entryType)
    {
        string Cell(string key) => ix.TryGetValue(key, out var i) && i < count ? Normalize(csv.GetField(i)) : string.Empty;

        var rawTitle = Cell("adventure_title");
        SplitAdventureTitle(rawTitle, out var advCode, out var advName);

        var sessionDate = Cell("date_played");
        if (CharacterLogCsvReader.TryParseSiteCsvTimestamp(sessionDate, out var playedUtc))
            sessionDate = CharacterLogCsvReader.FormatSiteDateForWorkbook(playedUtc);

        var dateDmRan = Cell("date_dmed");
        if (CharacterLogCsvReader.TryParseSiteCsvTimestamp(dateDmRan, out var dmedUtc))
            dateDmRan = CharacterLogCsvReader.FormatSiteDateForWorkbook(dmedUtc);

        var displayTitle = string.IsNullOrEmpty(advName) ? rawTitle : advName;
        if (!string.IsNullOrEmpty(advCode) && !string.IsNullOrEmpty(displayTitle))
            displayTitle = $"{advCode} — {displayTitle}";
        else if (!string.IsNullOrEmpty(advCode))
            displayTitle = advCode;

        return new CharacterSessionDetailRow
        {
            EntryType = entryType,
            AdventureTitle = string.IsNullOrEmpty(advName) ? rawTitle : advName,
            AdventureCode = advCode,
            DisplayTitle = string.IsNullOrWhiteSpace(displayTitle) ? entryType : displayTitle,
            DatePlayed = sessionDate,
            DateDmRan = dateDmRan,
            DmName = Cell("dm_name"),
            Gold = Cell("gp_gained"),
            Xp = Cell("xp_gained"),
            PlayerLevel = Cell("player_level"),
            Notes = Cell("notes"),
            MagicItems = new List<string>(),
        };
    }

    private static Dictionary<string, int> ReadHeaderMap(CsvReader csv, int count)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < count; i++)
        {
            var h = Normalize(csv.GetField(i));
            if (h.Length > 0 && !map.ContainsKey(h))
                map[h] = i;
        }

        return map;
    }

    private static void SplitAdventureTitle(string rawTitle, out string advCode, out string advName)
    {
        advCode = string.Empty;
        advName = rawTitle.Trim();
        if (advName.Length == 0)
            return;

        var m = AdventureLeadingCode.Match(advName);
        if (m.Success)
        {
            advCode = m.Groups["code"].Value;
            advName = m.Groups["rest"].Value.Trim();
            return;
        }

        var space = advName.IndexOf(' ');
        if (space <= 0)
            return;

        var first = advName[..space];
        var rest = advName[(space + 1)..].Trim();
        if (rest.Length == 0)
            return;

        var isCode = first.Contains('-')
                     || Regex.IsMatch(first, @"^[A-Z]{2,}\d+[a-zA-Z]?$", RegexOptions.CultureInvariant);
        if (!isCode)
            return;

        advCode = first;
        advName = rest;
    }

    private static string Normalize(string? value) =>
        (value ?? string.Empty).Trim().TrimStart('\ufeff');
}

/// <summary>One session log row from the character CSV, with magic items found in following MAGIC ITEM lines.</summary>
public sealed class CharacterSessionDetailRow
{
    public string EntryType { get; init; } = string.Empty;
    public string AdventureTitle { get; init; } = string.Empty;
    public string AdventureCode { get; init; } = string.Empty;
    public string DisplayTitle { get; init; } = string.Empty;
    public string DatePlayed { get; init; } = string.Empty;
    public string DateDmRan { get; init; } = string.Empty;
    public string DmName { get; init; } = string.Empty;
    public string Gold { get; init; } = string.Empty;
    public string Xp { get; init; } = string.Empty;
    public string PlayerLevel { get; init; } = string.Empty;
    public string Notes { get; init; } = string.Empty;
    public List<string> MagicItems { get; init; } = new();

    public string DetailLine
    {
        get
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(DatePlayed))
                parts.Add($"Played {DatePlayed}");
            if (!string.IsNullOrWhiteSpace(DateDmRan))
                parts.Add($"DM run {DateDmRan}");
            if (!string.IsNullOrWhiteSpace(DmName))
                parts.Add($"DM {DmName}");
            if (!string.IsNullOrWhiteSpace(Gold))
                parts.Add($"Gold {Gold}");
            if (!string.IsNullOrWhiteSpace(Xp))
                parts.Add($"XP {Xp}");
            if (!string.IsNullOrWhiteSpace(PlayerLevel))
                parts.Add($"Level {PlayerLevel}");
            return parts.Count == 0 ? "—" : string.Join(" · ", parts);
        }
    }

    public string MagicItemsDisplay =>
        MagicItems.Count == 0 ? "No magic items listed for this session." : string.Join(Environment.NewLine, MagicItems.Select(s => $"• {s}"));

    public bool HasNotes => !string.IsNullOrWhiteSpace(Notes);
}
