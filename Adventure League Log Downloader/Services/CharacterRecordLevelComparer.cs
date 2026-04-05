using System.Collections;
using System.ComponentModel;
using System.Globalization;

namespace Adventure_League_Log_Downloader.Services;

public static class CharacterRecordLevelSort
{
    /// <summary>
    /// Parses level text for sorting: whole number, or sum of slash-separated class levels (e.g. 5/3 → 8).
    /// </summary>
    public static int ParseSortKey(string? level)
    {
        if (string.IsNullOrWhiteSpace(level))
            return int.MinValue;

        var t = level.Trim();
        if (int.TryParse(t, NumberStyles.Integer, CultureInfo.InvariantCulture, out var single))
            return single;

        var parts = t.Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var sum = 0;
        var any = false;
        foreach (var part in parts)
        {
            if (int.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
            {
                sum += n;
                any = true;
            }
        }

        return any ? sum : int.MinValue;
    }
}

public sealed class CharacterRecordLevelComparer : IComparer
{
    private readonly int _sign;

    public CharacterRecordLevelComparer(ListSortDirection direction)
    {
        _sign = direction == ListSortDirection.Ascending ? 1 : -1;
    }

    public int Compare(object? x, object? y)
    {
        var cx = (CharacterRecord)x!;
        var cy = (CharacterRecord)y!;
        var ix = CharacterRecordLevelSort.ParseSortKey(cx.Level);
        var iy = CharacterRecordLevelSort.ParseSortKey(cy.Level);
        var cmp = ix.CompareTo(iy);
        if (cmp != 0)
            return _sign * cmp;
        return string.Compare(cx.Name, cy.Name, StringComparison.OrdinalIgnoreCase);
    }
}
