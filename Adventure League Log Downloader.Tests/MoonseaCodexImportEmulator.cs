using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Adventure_League_Log_Downloader.Tests;

/// <summary>
/// Test-only reimplementation of how Moonsea Codex reads an AL Log CSV import, so tests can check what MSC would create
/// from a file. Written from the behavior of <c>MoonseaCodex/API</c> at commit 4f35e87 (2025-12-06), not copied from it
/// (that repo is GPL-3.0):
/// <list type="bullet">
/// <item><c>codex/views/imports/character.py</c> — Python <c>str.splitlines()</c> on the upload</item>
/// <item><c>codex/imports/csv.py</c> — exact header comparison, 8-field character row, <c>bool(publicly_visible)</c></item>
/// <item><c>codex/imports/parse_events.py</c>, <c>adventurersleaguelogs.py</c> — <c>line.split(",")</c> with fixed field indexes; a row that is too short is skipped silently</item>
/// <item><c>codex/imports/parse_items.py</c>, <c>items.py</c> — potion/scroll names skipped, traded items removed by name + rarity</item>
/// <item><c>codex/imports/games.py</c> — adventure code regex, <c>int()</c>/<c>int(float())</c> conversions; a failing row is skipped silently</item>
/// </list>
/// Not emulated: date parsing, database writes, item lookups against existing MSC data, and level (MSC's
/// <c>Character.save()</c> recalculates it from the class text via <c>parse_classes.py</c>).
/// </summary>
internal static class MoonseaCodexImportEmulator
{
    internal const string CharacterHeader =
        "name,race,class_and_levels,faction,background,lifestyle,portrait_url,publicly_visible";

    internal const string EventHeader =
        "type,adventure_title,session_num,date_played,session_length_hours,player_level,xp_gained,gp_gained,downtime_gained,renown_gained,num_secret_missions,location_played,dm_name,dm_dci_number,notes,date_dmed,campaign_id";

    internal sealed record Game(string Module, string Name, int Gold, int Downtime, int Hours, string Notes);

    internal sealed record Item(string Name, string Rarity);

    internal sealed record Character(string Name, bool Public, IReadOnlyList<Game> Games, IReadOnlyList<Item> Items);

    // Line boundaries recognized by Python's str.splitlines().
    private static readonly char[] PythonLineBreaks =
        ['\n', '\r', '\u000B', '\u000C', '\u001C', '\u001D', '\u001E', '\u0085', '\u2028', '\u2029'];

    private static readonly Regex ModuleCode = new(
        @"((DD|DC|PO|SJ|PS|FR|DL|EB|MCX|RMH|RV|WBW|DRW|BMG|CCC)[\-\w]+)", RegexOptions.CultureInvariant);

    /// <summary>Returns what MSC would create, or throws <see cref="FormatException"/> where MSC would reject the whole import.</summary>
    internal static Character Import(string fileText)
    {
        var lines = SplitLines(fileText);
        if (lines.Count < 3 || lines[0] != CharacterHeader || lines[2] != EventHeader)
            throw new FormatException("File does not appear to be a valid Adventurers League Logs export");

        var charFields = lines[1].Split(',');
        if (charFields.Length != 8)
            throw new FormatException($"Character row has {charFields.Length} comma-separated fields; MSC unpacks exactly 8");

        var games = new List<Game>();
        var gained = new List<Item>();
        var traded = new List<Item>();

        foreach (var line in lines.Skip(3))
        {
            var fields = line.Split(',');
            switch (fields[0])
            {
                case "CharacterLogEntry":
                    if (fields.Length < 16)
                        continue; // IndexError while reading date_dmed
                    if (TryCreateGame(fields, out var game))
                        games.Add(game);
                    break;
                case "MAGIC ITEM":
                    if (fields.Length < 7 || fields[1] == "name" || fields[1].Length == 0)
                        continue;
                    var lower = fields[1].ToLowerInvariant();
                    if (lower.Contains("scroll") || lower.Contains("potion"))
                        continue;
                    gained.Add(new Item(fields[1], fields[2]));
                    break;
                case "TRADED MAGIC ITEM":
                    if (fields.Length < 3)
                        continue;
                    traded.Add(new Item(fields[1], fields[2]));
                    break;
            }
        }

        foreach (var t in traded)
        {
            var match = gained.FindIndex(i => i.Name == t.Name && i.Rarity == t.Rarity);
            if (match >= 0)
                gained.RemoveAt(match);
        }

        var items = gained.Select(i => i with { Rarity = i.Rarity.Length == 0 ? "common" : i.Rarity }).ToList();
        return new Character(charFields[0], charFields[7].Length > 0, games, items);
    }

    /// <summary>Python <c>str.splitlines()</c>: <c>\r\n</c> counts once, and a trailing break adds no empty line.</summary>
    internal static List<string> SplitLines(string text)
    {
        var lines = new List<string>();
        var start = 0;
        for (var i = 0; i < text.Length; i++)
        {
            if (Array.IndexOf(PythonLineBreaks, text[i]) < 0)
                continue;
            lines.Add(text[start..i]);
            if (text[i] == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                i++;
            start = i + 1;
        }
        if (start < text.Length)
            lines.Add(text[start..]);
        return lines;
    }

    private static bool TryCreateGame(string[] fields, out Game game)
    {
        game = null!;
        var title = fields[1];
        var code = "?";
        var match = ModuleCode.Match(title);
        if (match.Success)
        {
            code = match.Value;
            title = title.Replace(code, "");
        }

        if (!TryPythonInt(fields[4], out var hours)
            || !TryPythonIntOfFloat(fields[8], out var downtime)
            || !TryPythonIntOfFloat(fields[7], out var gold))
        {
            return false;
        }

        game = new Game(code, title.Trim(), gold, downtime, hours, fields[14]);
        return true;
    }

    // int(value or 0): empty is 0; "4" parses; "4.0" raises in Python.
    private static bool TryPythonInt(string value, out int result)
    {
        result = 0;
        return value.Length == 0
               || int.TryParse(value.Trim(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out result);
    }

    // int(float(value or 0)): truncates toward zero.
    private static bool TryPythonIntOfFloat(string value, out int result)
    {
        result = 0;
        if (value.Length == 0)
            return true;
        if (!double.TryParse(value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
            return false;
        result = (int)Math.Truncate(d);
        return true;
    }
}
