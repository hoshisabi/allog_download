using System.Collections;
using System.ComponentModel;
using System.Globalization;

namespace Adventure_League_Log_Downloader.Services;

public static class DmSessionDateDmedSort
{
    public static DateTime GetSortDate(string? dateDmed, ListSortDirection direction)
    {
        DateTime? parsed = null;
        if (!string.IsNullOrWhiteSpace(dateDmed))
        {
            if (DateTime.TryParse(dateDmed, CultureInfo.CurrentCulture, DateTimeStyles.None, out var dt))
                parsed = dt.Date;
            else if (DateTime.TryParse(dateDmed, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                parsed = dt.Date;
        }

        if (parsed.HasValue)
            return parsed.Value;

        return direction == ListSortDirection.Ascending ? DateTime.MaxValue : DateTime.MinValue;
    }
}

public sealed class DmSessionDateDmedComparer : IComparer
{
    private readonly ListSortDirection _direction;
    private readonly int _sign;

    public DmSessionDateDmedComparer(ListSortDirection direction)
    {
        _direction = direction;
        _sign = direction == ListSortDirection.Ascending ? 1 : -1;
    }

    public int Compare(object? x, object? y)
    {
        var sx = (DmSessionRecord)x!;
        var sy = (DmSessionRecord)y!;
        var dx = DmSessionDateDmedSort.GetSortDate(sx.DateDmed, _direction);
        var dy = DmSessionDateDmedSort.GetSortDate(sy.DateDmed, _direction);
        var cmp = dx.CompareTo(dy);
        if (cmp != 0)
            return _sign * cmp;
        return string.Compare(sx.AdventureTitle, sy.AdventureTitle, StringComparison.OrdinalIgnoreCase);
    }
}

public static class DmSessionSessionNumSort
{
    public static int ParseSortKey(string? sessionNum)
    {
        if (string.IsNullOrWhiteSpace(sessionNum))
            return int.MinValue;
        var t = sessionNum.Trim().TrimStart('#');
        return int.TryParse(t, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n)
            ? n
            : int.MinValue;
    }
}

public sealed class DmSessionSessionNumComparer : IComparer
{
    private readonly int _sign;

    public DmSessionSessionNumComparer(ListSortDirection direction)
    {
        _sign = direction == ListSortDirection.Ascending ? 1 : -1;
    }

    public int Compare(object? x, object? y)
    {
        var sx = (DmSessionRecord)x!;
        var sy = (DmSessionRecord)y!;
        var ix = DmSessionSessionNumSort.ParseSortKey(sx.SessionNum);
        var iy = DmSessionSessionNumSort.ParseSortKey(sy.SessionNum);
        var cmp = ix.CompareTo(iy);
        if (cmp != 0)
            return _sign * cmp;
        return string.Compare(sx.AdventureTitle, sy.AdventureTitle, StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class DmSessionDetailFetchedComparer : IComparer
{
    private readonly int _sign;

    public DmSessionDetailFetchedComparer(ListSortDirection direction)
    {
        _sign = direction == ListSortDirection.Ascending ? 1 : -1;
    }

    public int Compare(object? x, object? y)
    {
        var sx = (DmSessionRecord)x!;
        var sy = (DmSessionRecord)y!;
        var bx = sx.DetailFetched;
        var by = sy.DetailFetched;
        var cmp = bx == by ? 0 : bx ? 1 : -1;
        if (cmp != 0)
            return _sign * cmp;
        return string.Compare(sx.AdventureTitle, sy.AdventureTitle, StringComparison.OrdinalIgnoreCase);
    }
}
