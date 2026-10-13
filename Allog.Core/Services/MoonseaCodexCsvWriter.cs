using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using CsvHelper;
using CsvHelper.Configuration;

namespace Adventure_League_Log_Downloader.Services;

/// <summary>
/// Rewrites an adventurersleaguelog.com character CSV into the shape Moonsea Codex's importer can read.
/// MSC (<c>MoonseaCodex/API</c> <c>codex/imports/csv.py</c>) splits the file on line breaks and each line on commas, with no
/// CSV quoting support, and requires the character and event header lines verbatim. This writer keeps those two header
/// lines exact and flattens every other field so that line breaks and commas inside a field no longer shift columns.
/// </summary>
public static class MoonseaCodexCsvWriter
{
    public const string ExpectedCharacterHeader =
        "name,race,class_and_levels,faction,background,lifestyle,portrait_url,publicly_visible";

    public const string ExpectedEventHeader =
        "type,adventure_title,session_num,date_played,session_length_hours,player_level,xp_gained,gp_gained,downtime_gained,renown_gained,num_secret_missions,location_played,dm_name,dm_dci_number,notes,date_dmed,campaign_id";

    public const string ImportFileBaseName = "moonseacodeximport";

    private const int PubliclyVisibleIndex = 7;

    private static readonly CsvConfiguration ReaderConfig = new(CultureInfo.InvariantCulture)
    {
        HasHeaderRecord = false,
        BadDataFound = null,
        MissingFieldFound = null,
    };

    // Everything Python's str.splitlines() treats as a line boundary, since MSC splits the upload that way.
    private static readonly Regex LineBreakRun = new(
        @"\s*(?:\r\n|[\r\n\v\f\x1C\x1D\x1E\u0085\u2028\u2029])+\s*",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    /// <summary>
    /// Converts site CSV text to MSC import text. MSC treats any non-empty <c>publicly_visible</c> value as true, so a
    /// private character is written with an empty value.
    /// </summary>
    /// <exception cref="InvalidDataException">The text does not start with the site's character and event header lines.</exception>
    public static string Convert(string siteCsv, bool makePublic)
    {
        var records = ReadRecords(siteCsv);

        if (records.Count < 3
            || string.Join(",", records[0]) != ExpectedCharacterHeader
            || string.Join(",", records[2]) != ExpectedEventHeader)
        {
            throw new InvalidDataException("This file does not look like an Adventurers League Log character CSV export.");
        }

        var character = records[1];
        while (character.Length <= PubliclyVisibleIndex)
            Array.Resize(ref character, character.Length + 1);
        character[PubliclyVisibleIndex] = makePublic ? "true" : string.Empty;

        var sb = new StringBuilder();
        sb.Append(ExpectedCharacterHeader).Append('\n');
        AppendRow(sb, character);
        sb.Append(ExpectedEventHeader).Append('\n');
        for (var i = 3; i < records.Count; i++)
            AppendRow(sb, records[i]);
        return sb.ToString();
    }

    /// <summary>
    /// Converts <paramref name="siteCsvPath"/> and writes it into <paramref name="outputFolder"/> as
    /// <c>moonseacodeximport.csv</c>, or <c>moonseacodeximport (N).csv</c> when that name is taken. UTF-8 without a BOM,
    /// since a BOM would break MSC's header comparison. Returns the path written.
    /// </summary>
    public static string WriteImportFile(string siteCsvPath, string outputFolder, bool makePublic)
    {
        var converted = Convert(File.ReadAllText(siteCsvPath, Encoding.UTF8), makePublic);
        Directory.CreateDirectory(outputFolder);
        var path = UniqueFilePath.Next(outputFolder, ImportFileBaseName, ".csv");
        File.WriteAllText(path, converted, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        return path;
    }

    /// <summary>Line breaks become a single space and commas become semicolons; surrounding whitespace is trimmed.</summary>
    public static string FlattenField(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;
        return LineBreakRun.Replace(value, " ").Replace(',', ';').Trim();
    }

    private static void AppendRow(StringBuilder sb, string[] fields)
    {
        for (var i = 0; i < fields.Length; i++)
        {
            if (i > 0)
                sb.Append(',');
            sb.Append(FlattenField(fields[i]));
        }
        sb.Append('\n');
    }

    private static List<string[]> ReadRecords(string csvText)
    {
        var records = new List<string[]>();
        using var reader = new StringReader(csvText.TrimStart('\uFEFF'));
        using var csv = new CsvReader(reader, ReaderConfig);
        while (csv.Read())
            records.Add(csv.Parser.Record ?? Array.Empty<string>());
        return records;
    }
}
