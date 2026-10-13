using System;
using System.Globalization;
using System.IO;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;

namespace Adventure_League_Log_Downloader.Services;

/// <summary>
/// Reads Adventurers League per-character CSV exports and finds the latest session log date.
/// Matches the live site's <c>CharacterCsvExporter</c>: log rows are STI <c>type</c> values ending in <c>LogEntry</c>,
/// with <c>date_played</c> at index 3 and <c>date_dmed</c> at index 15 (DM rows often leave <c>date_played</c> blank).
/// Dates are exported like <c>2016-08-20 00:00:00 UTC</c>, which <see cref="DateTime.TryParse"/> does not accept as-is.
/// </summary>
public static class CharacterLogCsvReader
{
    private static readonly CsvConfiguration CsvConfig = new(CultureInfo.InvariantCulture)
    {
        HasHeaderRecord = false,
        BadDataFound = null,
        MissingFieldFound = null,
    };

    /// <summary>
    /// Parses timestamps as they appear in per-character site CSV (<c>date_played</c>, <c>date_dmed</c>, etc.).
    /// </summary>
    public static bool TryParseSiteCsvTimestamp(string? text, out DateTime utc)
    {
        utc = default;
        return !string.IsNullOrWhiteSpace(text) && TryParseSessionDate(text.Trim(), out utc);
    }

    /// <summary>
    /// Formats a parsed CSV timestamp for flat exports (ISO date in local time, Excel-friendly sorting).
    /// </summary>
    public static string FormatSiteDateForWorkbook(DateTime utc)
    {
        var local = utc.Kind switch
        {
            DateTimeKind.Utc => utc.ToLocalTime(),
            DateTimeKind.Unspecified => DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime(),
            _ => utc
        };
        return local.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Returns the latest <c>date_played</c> from session rows, or null if none could be parsed.
    /// </summary>
    public static DateTime? TryGetLatestSessionDatePlayed(string csvPath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(csvPath) || !File.Exists(csvPath))
                return null;

            using var stream = File.OpenRead(csvPath);
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            using var csv = new CsvReader(reader, CsvConfig);

            DateTime? max = null;
            while (csv.Read())
            {
                if (csv.ColumnCount < 4)
                    continue;

                var rowType = NormalizeCell(csv.GetField(0));
                if (rowType.Length == 0 || IsNoiseRowType(rowType))
                    continue;

                // STI log entry types from the site (CharacterLogEntry, DmLogEntry, TradeLogEntry, …)
                if (!rowType.EndsWith("LogEntry", StringComparison.Ordinal))
                    continue;

                var title = NormalizeCell(csv.GetField(1));
                if (title.Length == 0)
                    continue;

                // Legacy human-readable header; current site uses snake_case attribute names in a header row.
                if (title.Equals("Adventure Title", StringComparison.OrdinalIgnoreCase)
                    || title.Equals("adventure_title", StringComparison.OrdinalIgnoreCase))
                    continue;

                // Latest date on this row: max of date_played (3) and date_dmed (15) when present.
                DateTime? rowMax = null;
                if (csv.ColumnCount > 3)
                    ConsiderDate(NormalizeCell(csv.GetField(3)), ref rowMax);
                if (csv.ColumnCount > 15)
                    ConsiderDate(NormalizeCell(csv.GetField(15)), ref rowMax);

                if (!rowMax.HasValue)
                    continue;

                if (!max.HasValue || rowMax.Value > max.Value)
                    max = rowMax;
            }

            return max;
        }
        catch
        {
            return null;
        }
    }

    private static string NormalizeCell(string? value) =>
        (value ?? string.Empty).Trim().TrimStart('\ufeff');

    private static void ConsiderDate(string text, ref DateTime? rowMax)
    {
        if (text.Length == 0 || !TryParseSessionDate(text, out var dt))
            return;
        if (!rowMax.HasValue || dt > rowMax.Value)
            rowMax = dt;
    }

    private static bool IsNoiseRowType(string row0) =>
        row0.Equals("name", StringComparison.OrdinalIgnoreCase) // character section header
        || row0.Equals("type", StringComparison.OrdinalIgnoreCase) // log section header
        || row0.Equals("MAGIC ITEM", StringComparison.OrdinalIgnoreCase)
        || row0.Equals("TRADED MAGIC ITEM", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// When a CSV exists next to the given characters JSON path (see <see cref="CharacterCsvLocator"/>), sets <see cref="CharacterRecord.LastSessionPlayed"/>.
    /// Does not clear an existing value when the file is missing or has no parseable dates.
    /// </summary>
    public static void ApplyLatestSessionFromCsvIfPresent(CharacterRecord character, string charactersJsonPath)
    {
        var path = CharacterCsvLocator.TryFindCharacterCsvFile(charactersJsonPath, character.Id);
        if (path == null)
            return;

        var latest = TryGetLatestSessionDatePlayed(path);
        if (!latest.HasValue)
            return;

        character.LastSessionPlayed = FormatSessionDateForDisplay(latest.Value);
    }

    public static string FormatSessionDateForDisplay(DateTime dt)
    {
        var local = dt.Kind switch
        {
            DateTimeKind.Utc => dt.ToLocalTime(),
            DateTimeKind.Unspecified => DateTime.SpecifyKind(dt, DateTimeKind.Utc).ToLocalTime(),
            _ => dt
        };
        return local.ToString("d", CultureInfo.CurrentCulture);
    }

    private static bool TryParseSessionDate(string text, out DateTime dt)
    {
        dt = default;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        text = text.Trim();
        var utcStyles = DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal;

        // Site / Rails: "2016-08-20 00:00:00 UTC" — DateTime.TryParse rejects the literal " UTC" suffix.
        if (text.EndsWith(" UTC", StringComparison.OrdinalIgnoreCase))
        {
            var core = text[..^4].Trim();
            if (DateTime.TryParseExact(core, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, utcStyles, out dt))
                return true;
            if (DateTime.TryParseExact(core, "yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture, utcStyles, out dt))
                return true;
            if (DateTime.TryParseExact(core, "yyyy-MM-dd", CultureInfo.InvariantCulture, utcStyles, out dt))
                return true;
        }

        if (DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var dto))
        {
            dt = dto.UtcDateTime;
            return true;
        }

        var styles = DateTimeStyles.AllowWhiteSpaces;
        if (DateTime.TryParse(text, CultureInfo.CurrentCulture, styles, out dt))
            return true;
        if (DateTime.TryParse(text, CultureInfo.InvariantCulture, styles, out dt))
            return true;

        foreach (var fmt in SessionDateFormats)
        {
            if (DateTime.TryParseExact(text, fmt, CultureInfo.InvariantCulture, styles, out dt))
                return true;
            if (DateTime.TryParseExact(text, fmt, CultureInfo.CurrentCulture, styles, out dt))
                return true;
        }

        return false;
    }

    private static readonly string[] SessionDateFormats =
    {
        "M/d/yyyy",
        "MM/dd/yyyy",
        "yyyy-MM-dd",
        "d/M/yyyy",
        "dd/MM/yyyy",
        "yyyy/MM/dd",
        "MMM d, yyyy",
        "MMMM d, yyyy",
        "ddd, MMM d, yyyy",
        "yyyy-MM-dd HH:mm:ss",
        "yyyy-MM-dd HH:mm:ss.fff",
    };
}
