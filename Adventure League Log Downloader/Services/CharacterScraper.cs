using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace Adventure_League_Log_Downloader.Services;

/// <summary>
/// C# port of the Python CharacterScraper: discovers pagination, scrapes rows, and writes JSON.
/// </summary>
public sealed class CharacterScraper
{
    private readonly IAdventurersLeagueAuth _auth;
    private readonly Dictionary<string, CharacterRecord> _characters = new();

    public CharacterScraper(IAdventurersLeagueAuth auth)
    {
        _auth = auth;
    }

    /// <summary>
    /// Returns the maximum page number by inspecting the pagination and finding the ">>" link.
    /// Defaults to 1 if not found or on non-200 responses.
    /// </summary>
    public async Task<int> GetMaxPageAsync(CancellationToken ct = default)
    {
        var client = await _auth.GetAuthenticatedClientAsync();
        var userId = await _auth.GetUserIdAsync();
        var url = $"https://www.adventurersleaguelog.com/users/{userId}/characters?page=1";

        using var resp = await client.GetAsync(url, ct);
        if (!resp.IsSuccessStatusCode)
        {
            return 1;
        }

        var html = await resp.Content.ReadAsStringAsync(ct);
        var doc = new HtmlAgilityPack.HtmlDocument();
        doc.LoadHtml(html);

        // Strategy 1: gather all anchors with an href containing "page=" and take the max
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

        // Strategy 2: look for a visible ">>" link specifically
        var lastLink = doc.DocumentNode.SelectNodes("//a")?.FirstOrDefault(a => a.InnerText.Trim().Replace("\u00BB", ">>") == ">>");
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
    /// Scrapes all pages and returns a dictionary keyed by character id.
    /// Reports progress after pagination discovery and after each page is parsed.
    /// </summary>
    public async Task<IReadOnlyDictionary<string, CharacterRecord>> ScrapeAsync(
        double delaySeconds = 0.25,
        IProgress<CharacterScrapeReport>? progress = null,
        CancellationToken ct = default)
    {
        _characters.Clear();

        var client = await _auth.GetAuthenticatedClientAsync();
        var userId = await _auth.GetUserIdAsync();

        progress?.Report(new CharacterScrapeReport
        {
            Phase = CharacterScrapePhase.DiscoveringPages,
            CharacterCount = 0,
            Characters = Array.Empty<CharacterRecord>(),
            Detail = "Checking how many pages of characters exist…"
        });

        var maxPage = await GetMaxPageAsync(ct);
        if (maxPage < 1) maxPage = 1;

        progress?.Report(new CharacterScrapeReport
        {
            Phase = CharacterScrapePhase.DiscoveringPages,
            TotalPages = maxPage,
            CharacterCount = 0,
            Characters = Array.Empty<CharacterRecord>(),
            Detail = maxPage == 1
                ? "Found 1 page; will probe for more if the site hides pagination."
                : $"Found {maxPage} pages to load."
        });

        for (var page = 1; page <= maxPage; page++)
        {
            ct.ThrowIfCancellationRequested();

            var url = $"https://www.adventurersleaguelog.com/users/{userId}/characters?page={page}";
            using var resp = await client.GetAsync(url, ct);
            if (!resp.IsSuccessStatusCode)
            {
                progress?.Report(new CharacterScrapeReport
                {
                    Phase = CharacterScrapePhase.Scraping,
                    CurrentPage = page,
                    TotalPages = maxPage,
                    CharacterCount = _characters.Count,
                    Characters = SnapshotCharacters(),
                    Detail = $"Page {page} of {maxPage}: HTTP {(int)resp.StatusCode}; skipped."
                });
                continue;
            }

            var html = await resp.Content.ReadAsStringAsync(ct);
            _ = ParseCharacterTable(html);

            progress?.Report(new CharacterScrapeReport
            {
                Phase = CharacterScrapePhase.Scraping,
                CurrentPage = page,
                TotalPages = maxPage,
                CharacterCount = _characters.Count,
                Characters = SnapshotCharacters(),
                Detail = $"Page {page} of {maxPage}: loaded {_characters.Count} character(s) so far."
            });

            if (delaySeconds > 0)
            {
                var delayMs = (int)Math.Round(delaySeconds * 1000);
                await Task.Delay(delayMs, ct);
            }
        }

        // Fallback probing: if we only detected a single page, try subsequent pages until empty
        if (maxPage == 1)
        {
            for (var page = 2; page <= 50; page++) // hard upper bound to be safe
            {
                ct.ThrowIfCancellationRequested();
                var url = $"https://www.adventurersleaguelog.com/users/{userId}/characters?page={page}";
                using var resp = await client.GetAsync(url, ct);
                if (!resp.IsSuccessStatusCode)
                    break;
                var html = await resp.Content.ReadAsStringAsync(ct);
                var parsed = ParseCharacterTable(html);
                if (parsed <= 0)
                    break; // stop when a page yields no rows

                progress?.Report(new CharacterScrapeReport
                {
                    Phase = CharacterScrapePhase.Scraping,
                    CurrentPage = page,
                    TotalPages = null,
                    CharacterCount = _characters.Count,
                    Characters = SnapshotCharacters(),
                    Detail = $"Extra page {page}: loaded {_characters.Count} character(s) so far."
                });

                if (delaySeconds > 0)
                {
                    var delayMs = (int)Math.Round(delaySeconds * 1000);
                    await Task.Delay(delayMs, ct);
                }
            }
        }

        return _characters;
    }

    private List<CharacterRecord> SnapshotCharacters()
    {
        return _characters.Values
            .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private int ParseCharacterTable(string html)
    {
        var doc = new HtmlAgilityPack.HtmlDocument();
        doc.LoadHtml(html);

        // Select all rows under any table body
        var rows = doc.DocumentNode.SelectNodes("//table//tbody//tr");
        if (rows == null) return 0;

        int parsedCount = 0;

        foreach (var row in rows)
        {
            var cells = row.SelectNodes(".//td");
            if (cells == null || cells.Count < 2) continue; // need at least id/name column

            string season = HtmlEntity.DeEntitize(cells.ElementAtOrDefault(0)?.InnerText ?? string.Empty).Trim();

            // The id and name are in the 2nd column with an <a href="/characters/{id}">Name</a>
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
                continue; // skip malformed rows

            _characters[id] = new CharacterRecord
            {
                Id = id,
                Name = name,
                Race = race,
                Class = @class,
                Level = level,
                Season = season,
                Tag = tag
            };
            parsedCount++;
        }

        return parsedCount;
    }

    /// <summary>
    /// Writes the scraped dictionary to JSON at the given path.
    /// The JSON keys are the character ids, with values in camelCase to closely match the Python output.
    /// </summary>
    public async Task SaveJsonAsync(string path, CancellationToken ct = default)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        await using var fs = System.IO.File.Create(path);
        await JsonSerializer.SerializeAsync(fs, _characters, options, ct);
    }
}
