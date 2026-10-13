using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
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
        return CharacterPageHtml.GetMaxPageNumber(html);
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
        var rows = CharacterPageHtml.ParseCharacterTableRows(html);
        foreach (var r in rows)
            _characters[r.Id] = r;
        return rows.Count;
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
