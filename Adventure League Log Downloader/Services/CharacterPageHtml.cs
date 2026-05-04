using System;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;

namespace Adventure_League_Log_Downloader.Services;

/// <summary>
/// Pure HTML parsing for character list pages — used by <see cref="CharacterScraper"/> and unit tests.
/// </summary>
internal static class CharacterPageHtml
{
    /// <summary>
    /// Returns the maximum page number by inspecting pagination links (strategy 1)
    /// and the "»" / "&gt;&gt;" link (strategy 2). Defaults to 1.
    /// </summary>
    public static int GetMaxPageNumber(string html)
    {
        var doc = new HtmlAgilityPack.HtmlDocument();
        doc.LoadHtml(html);

        int maxFromLinks = 1;
        var anchors = doc.DocumentNode.SelectNodes("//a[@href]");
        if (anchors != null)
        {
            foreach (var a in anchors)
            {
                var href = a.GetAttributeValue("href", string.Empty);
                if (string.IsNullOrEmpty(href)) continue;
                var idx = href.IndexOf("page=", StringComparison.OrdinalIgnoreCase);
                if (idx < 0) continue;
                var after = href[(idx + 5)..];
                var ampIdx = after.IndexOf('&');
                var pageStr = ampIdx >= 0 ? after[..ampIdx] : after;
                if (int.TryParse(pageStr, out var p))
                    if (p > maxFromLinks) maxFromLinks = p;
            }
        }

        if (maxFromLinks > 1)
            return maxFromLinks;

        var lastLink = doc.DocumentNode.SelectNodes("//a")
            ?.FirstOrDefault(a => a.InnerText.Trim().Replace("\u00BB", ">>") == ">>");
        if (lastLink != null)
        {
            var href = lastLink.GetAttributeValue("href", "");
            var idx = href.IndexOf("page=", StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
            {
                var after = href[(idx + 5)..];
                var ampIdx = after.IndexOf('&');
                var pageStr = ampIdx >= 0 ? after[..ampIdx] : after;
                if (int.TryParse(pageStr, out var p))
                    return Math.Max(1, p);
            }
        }

        return 1;
    }

    /// <summary>
    /// Parses character rows from a list page. Rows without a character id are skipped.
    /// </summary>
    public static List<CharacterRecord> ParseCharacterTableRows(string html)
    {
        var doc = new HtmlAgilityPack.HtmlDocument();
        doc.LoadHtml(html);

        var rows = doc.DocumentNode.SelectNodes("//table//tbody//tr");
        if (rows == null) return [];

        var list = new List<CharacterRecord>();

        foreach (var row in rows)
        {
            var cells = row.SelectNodes(".//td");
            if (cells == null || cells.Count < 2) continue;

            string season = HtmlEntity.DeEntitize(cells.ElementAtOrDefault(0)?.InnerText ?? string.Empty).Trim();

            var idAnchor = cells.ElementAtOrDefault(1)?.SelectSingleNode(".//a[@href]");
            string id = string.Empty;
            string name = "UNKNOWN";
            if (idAnchor != null)
            {
                var href = idAnchor.GetAttributeValue("href", "").Trim();
                if (!string.IsNullOrEmpty(href))
                {
                    var slashIdx = href.LastIndexOf('/') + 1;
                    if (slashIdx > 0 && slashIdx < href.Length)
                        id = href.Substring(slashIdx);
                }
                name = HtmlEntity.DeEntitize(idAnchor.InnerText).Trim();
            }

            string race = HtmlEntity.DeEntitize(cells.ElementAtOrDefault(2)?.InnerText ?? string.Empty).Trim();
            string @class = HtmlEntity.DeEntitize(cells.ElementAtOrDefault(3)?.InnerText ?? string.Empty).Trim();
            string level = HtmlEntity.DeEntitize(cells.ElementAtOrDefault(4)?.InnerText ?? string.Empty).Trim();
            string tag = HtmlEntity.DeEntitize(cells.ElementAtOrDefault(5)?.InnerText ?? string.Empty).Trim();

            if (string.IsNullOrEmpty(id))
                continue;

            list.Add(new CharacterRecord
            {
                Id = id,
                Name = name,
                Race = race,
                Class = @class,
                Level = level,
                Season = season,
                Tag = tag
            });
        }

        return list;
    }
}
