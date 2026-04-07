using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;

namespace Adventure_League_Log_Downloader.Services;

/// <summary>
/// Builds a single "workbook-style" CSV (one row per log entry) from downloaded <c>character_*.csv</c> files.
/// Columns align loosely with player spreadsheet conventions — see <c>docs/examples/spreadsheet-ken-ddal-log.md</c>.
/// </summary>
public static class SessionLogWorkbookCsvExporter
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

    /// <summary> Written next to the characters JSON as <c>session_log_workbook.csv</c>. </summary>
    public const string DefaultOutputFileName = "session_log_workbook.csv";

    public sealed class Result
    {
        public required string OutputPath { get; init; }
        public int RowCount { get; init; }
        public int CharactersUsed { get; init; }
        public int CharactersSkippedNoCsv { get; init; }
    }

    public static async Task<Result> ExportAsync(
        string charactersJsonPath,
        IEnumerable<CharacterRecord> characters,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(charactersJsonPath);

        var jsonDir = Path.GetDirectoryName(Path.GetFullPath(charactersJsonPath)) ?? string.Empty;
        var outputPath = Path.Combine(jsonDir, DefaultOutputFileName);

        var rows = new List<WorkbookRow>();
        var sorted = characters
            .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var used = 0;
        var skipped = 0;
        foreach (var c in sorted)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var csvPath = CharacterCsvLocator.TryFindCharacterCsvFile(charactersJsonPath, c.Id);
            if (csvPath == null)
            {
                skipped++;
                continue;
            }

            used++;
            rows.AddRange(ParseCharacterCsv(csvPath, c));
        }

        await WriteWorkbookAsync(outputPath, rows, cancellationToken).ConfigureAwait(false);

        return new Result
        {
            OutputPath = outputPath,
            RowCount = rows.Count,
            CharactersUsed = used,
            CharactersSkippedNoCsv = skipped,
        };
    }

    private static List<WorkbookRow> ParseCharacterCsv(string csvPath, CharacterRecord character)
    {
        using var stream = File.OpenRead(csvPath);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        using var csv = new CsvReader(reader, ReaderConfig);

        Dictionary<string, int>? logColumns = null;
        var nextRowIsBannerName = false;
        WorkbookRow? currentSession = null;
        var results = new List<WorkbookRow>();

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
                {
                    logColumns = ReadHeaderMap(csv, count);
                }

                continue;
            }

            if (mark0.Equals("MAGIC ITEM", StringComparison.OrdinalIgnoreCase))
            {
                var nameCell = count > 1 ? Normalize(csv.GetField(1)) : string.Empty;
                if (nameCell.Equals("name", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (nameCell.Length > 0 && currentSession != null)
                    currentSession.MagicItemNames.Add(nameCell);

                continue;
            }

            if (mark0.EndsWith("LogEntry", StringComparison.Ordinal))
            {
                currentSession = CreateRowFromEntry(csv, count, logColumns, character, mark0);
                results.Add(currentSession);
            }
        }

        return results;
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

    private static WorkbookRow CreateRowFromEntry(
        CsvReader csv,
        int count,
        Dictionary<string, int> ix,
        CharacterRecord ch,
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

        return new WorkbookRow
        {
            CharacterName = ch.Name,
            CharacterId = ch.Id,
            EntryType = entryType,
            AdvName = string.IsNullOrEmpty(advName) ? rawTitle : advName,
            AdvCode = advCode,
            SessionDate = sessionDate,
            DateDmRan = dateDmRan,
            DMName = Cell("dm_name"),
            DmDci = Cell("dm_dci_number"),
            Gold = Cell("gp_gained"),
            PlayerLevel = Cell("player_level"),
            XP = Cell("xp_gained"),
            DowntimeDays = Cell("downtime_gained"),
            Renown = Cell("renown_gained"),
            SecretMissions = Cell("num_secret_missions"),
            SessionLengthHours = Cell("session_length_hours"),
            LocationPlayed = Cell("location_played"),
            SessionNum = Cell("session_num"),
            CampaignId = Cell("campaign_id"),
            Notes = Cell("notes"),
        };
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

    private static async Task WriteWorkbookAsync(string path, List<WorkbookRow> rows, CancellationToken cancellationToken)
    {
        await using var fs = File.Create(path);
        await using var textWriter = new StreamWriter(fs, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        await using var writer = new CsvWriter(textWriter, CultureInfo.InvariantCulture);

        foreach (var h in WorkbookRow.Headers)
            writer.WriteField(h);
        await writer.NextRecordAsync().ConfigureAwait(false);

        foreach (var r in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            writer.WriteField(r.CharacterName);
            writer.WriteField(r.AdvName);
            writer.WriteField(r.AdvCode);
            writer.WriteField(r.DMName);
            writer.WriteField(r.Gold);
            writer.WriteField(r.MagicItemNames.Count);
            writer.WriteField(string.Join("; ", r.MagicItemNames));
            writer.WriteField(r.Notes);
            writer.WriteField(r.PlayerLevel);
            writer.WriteField(string.Empty);
            writer.WriteField(r.EntryType);
            writer.WriteField(r.CharacterId);
            writer.WriteField(r.SessionDate);
            writer.WriteField(r.DateDmRan);
            writer.WriteField(r.XP);
            writer.WriteField(r.DowntimeDays);
            writer.WriteField(r.Renown);
            writer.WriteField(r.SecretMissions);
            writer.WriteField(r.SessionLengthHours);
            writer.WriteField(r.LocationPlayed);
            writer.WriteField(r.SessionNum);
            writer.WriteField(r.CampaignId);
            writer.WriteField(r.DmDci);
            await writer.NextRecordAsync().ConfigureAwait(false);
        }
    }

    private static string Normalize(string? value) =>
        (value ?? string.Empty).Trim().TrimStart('\ufeff');

    private sealed class WorkbookRow
    {
        public static readonly string[] Headers =
        [
            "Name",
            "Adv Name",
            "Adv Code",
            "DM Name",
            "Gold",
            "Magic Item Count",
            "Magic Item Names",
            "Notes",
            "Level",
            "Needs Update",
            "Entry Type",
            "Character Id",
            "Session Date",
            "Date DM Ran",
            "XP",
            "Downtime Days",
            "Renown",
            "Secret Missions",
            "Session Length Hours",
            "Location Played",
            "Session Num",
            "Campaign Id",
            "DM DCI",
        ];

        public required string CharacterName { get; init; }
        public required string CharacterId { get; init; }
        public required string EntryType { get; init; }
        public required string AdvName { get; init; }
        public required string AdvCode { get; init; }
        public required string SessionDate { get; init; }
        public required string DateDmRan { get; init; }
        public required string DMName { get; init; }
        public required string DmDci { get; init; }
        public required string Gold { get; init; }
        public required string PlayerLevel { get; init; }
        public required string XP { get; init; }
        public required string DowntimeDays { get; init; }
        public required string Renown { get; init; }
        public required string SecretMissions { get; init; }
        public required string SessionLengthHours { get; init; }
        public required string LocationPlayed { get; init; }
        public required string SessionNum { get; init; }
        public required string CampaignId { get; init; }
        public required string Notes { get; init; }
        public List<string> MagicItemNames { get; } = new();
    }
}
