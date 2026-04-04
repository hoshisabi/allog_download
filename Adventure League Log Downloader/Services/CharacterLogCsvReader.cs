using System;
using System.Globalization;
using System.IO;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;

namespace Adventure_League_Log_Downloader.Services;

/// <summary>
/// Reads Adventurers League per-character CSV exports and finds the latest session log date
/// (same row shape as <c>character_csv_parser.py</c>: CharacterLogEntry / DmLogEntry, <c>date_played</c> in column 4).
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
                if (!csv.TryGetField(0, out string? rowType) || string.IsNullOrWhiteSpace(rowType))
                    continue;

                rowType = rowType.Trim();
                if (rowType is not ("CharacterLogEntry" or "DmLogEntry"))
                    continue;

                if (!csv.TryGetField(1, out string? title) || string.IsNullOrWhiteSpace(title))
                    continue;

                if (string.Equals(title.Trim(), "Adventure Title", StringComparison.Ordinal))
                    continue;

                if (!csv.TryGetField(3, out string? datePlayed) || string.IsNullOrWhiteSpace(datePlayed))
                    continue;

                if (!TryParseSessionDate(datePlayed.Trim(), out var dt))
                    continue;

                if (!max.HasValue || dt > max.Value)
                    max = dt;
            }

            return max;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// When a CSV file exists and yields at least one session date, sets <see cref="CharacterRecord.LastSessionPlayed"/>.
    /// Does not clear an existing value when the file is missing or has no parseable dates.
    /// </summary>
    public static void ApplyLatestSessionFromCsvIfPresent(CharacterRecord character, string csvDirectory)
    {
        var path = Path.Combine(csvDirectory, $"character_{character.Id}.csv");
        var latest = TryGetLatestSessionDatePlayed(path);
        if (!latest.HasValue)
            return;

        character.LastSessionPlayed = FormatSessionDateForDisplay(latest.Value);
    }

    public static string FormatSessionDateForDisplay(DateTime dt) =>
        dt.ToString("d", CultureInfo.CurrentCulture);

    private static bool TryParseSessionDate(string text, out DateTime dt)
    {
        dt = default;
        if (string.IsNullOrWhiteSpace(text))
            return false;

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
    };
}
