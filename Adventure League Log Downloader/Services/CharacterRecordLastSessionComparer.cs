using System.Collections;
using System.ComponentModel;
using System.Globalization;

namespace Adventure_League_Log_Downloader.Services;

public static class CharacterRecordLastSessionSort
{
    /// <summary>
    /// Parses <see cref="CharacterRecord.LastSessionPlayed"/> (short date from current culture) for ordering.
    /// Missing or unparseable values sort last for both ascending and descending.
    /// </summary>
    public static DateTime GetSortDate(string? lastSession, ListSortDirection direction)
    {
        DateTime? parsed = null;
        if (!string.IsNullOrWhiteSpace(lastSession))
        {
            if (DateTime.TryParse(lastSession, CultureInfo.CurrentCulture, DateTimeStyles.None, out var dt))
                parsed = dt.Date;
            else if (DateTime.TryParse(lastSession, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                parsed = dt.Date;
        }

        if (parsed.HasValue)
            return parsed.Value;

        return direction == ListSortDirection.Ascending ? DateTime.MaxValue : DateTime.MinValue;
    }
}

public sealed class CharacterRecordLastSessionComparer : IComparer
{
    private readonly ListSortDirection _direction;
    private readonly int _sign;

    public CharacterRecordLastSessionComparer(ListSortDirection direction)
    {
        _direction = direction;
        _sign = direction == ListSortDirection.Ascending ? 1 : -1;
    }

    public int Compare(object? x, object? y)
    {
        var cx = (CharacterRecord)x!;
        var cy = (CharacterRecord)y!;
        var dx = CharacterRecordLastSessionSort.GetSortDate(cx.LastSessionPlayed, _direction);
        var dy = CharacterRecordLastSessionSort.GetSortDate(cy.LastSessionPlayed, _direction);
        var cmp = dx.CompareTo(dy);
        if (cmp != 0)
            return _sign * cmp;
        return string.Compare(cx.Name, cy.Name, StringComparison.OrdinalIgnoreCase);
    }
}
