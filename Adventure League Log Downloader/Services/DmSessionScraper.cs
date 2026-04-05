using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;

namespace Adventure_League_Log_Downloader.Services;

/// <summary>
/// Scrapes DM log entries from AdventurersLeagueLog.com in two passes:
///   Pass 1 — paginated list pages: fast, gets basic fields + all session IDs.
///   Pass 2 — individual detail pages: one request per session, gets full reward/item data.
///
/// Recoverability: after each detail fetch the JSON is saved to <c>outputPath</c>.
/// On re-run the file is loaded first and sessions with <see cref="DmSessionRecord.DetailFetched"/> == true are skipped,
/// so an interrupted run resumes from where it left off.
/// </summary>
public sealed class DmSessionScraper
{
    private readonly IAdventurersLeagueAuth _auth;

    public DmSessionScraper(IAdventurersLeagueAuth auth)
    {
        _auth = auth;
    }

    /// <summary>
    /// Runs the full scrape: list pages then detail pages.
    /// Saves progress to <paramref name="outputPath"/> after the list phase and after each detail fetch.
    /// </summary>
    /// <param name="skipDetailPages">If true, stops after the list phase (no per-session show pages).</param>
    /// <param name="onlyFetchMissingDetailPages">If true, only sessions with <see cref="DmSessionRecord.DetailFetched"/> == false are fetched; if false, every session is fetched.</param>
    public async Task<IReadOnlyDictionary<string, DmSessionRecord>> ScrapeAsync(
        string outputPath,
        double delaySeconds = 0.25,
        IProgress<DmSessionScrapeReport>? progress = null,
        bool skipDetailPages = false,
        bool onlyFetchMissingDetailPages = true,
        CancellationToken ct = default)
    {
        var client = await _auth.GetAuthenticatedClientAsync();
        var userId = await _auth.GetUserIdAsync();

        // ── Load existing file for recoverability ────────────────────────────
        var sessions = await DmSessionJsonFile.TryLoadAsync(outputPath, ct)
                       ?? new Dictionary<string, DmSessionRecord>();

        // ── Pass 1: scrape list pages ─────────────────────────────────────────
        progress?.Report(new DmSessionScrapeReport
        {
            Phase = DmSessionScrapePhase.DiscoveringPages,
            SessionCount = sessions.Count,
            Detail = "Checking how many pages of DM sessions exist…"
        });

        var maxPage = await GetMaxPageAsync(client, userId, ct);

        progress?.Report(new DmSessionScrapeReport
        {
            Phase = DmSessionScrapePhase.DiscoveringPages,
            TotalPages = maxPage,
            SessionCount = sessions.Count,
            Detail = maxPage == 1 ? "Found 1 page." : $"Found {maxPage} pages to load."
        });

        for (var page = 1; page <= maxPage; page++)
        {
            ct.ThrowIfCancellationRequested();

            var url = $"https://www.adventurersleaguelog.com/users/{userId}/dm_log_entries?page={page}";
            using var resp = await client.GetAsync(url, ct);

            if (!resp.IsSuccessStatusCode)
            {
                progress?.Report(new DmSessionScrapeReport
                {
                    Phase = DmSessionScrapePhase.ScrapingList,
                    CurrentPage = page,
                    TotalPages = maxPage,
                    SessionCount = sessions.Count,
                    Detail = $"Page {page} of {maxPage}: HTTP {(int)resp.StatusCode}; skipped."
                });
                continue;
            }

            var html = await resp.Content.ReadAsStringAsync(ct);
            var parsed = ParseListPage(html, sessions);

            progress?.Report(new DmSessionScrapeReport
            {
                Phase = DmSessionScrapePhase.ScrapingList,
                CurrentPage = page,
                TotalPages = maxPage,
                SessionCount = sessions.Count,
                Detail = $"Page {page} of {maxPage}: {parsed} session(s) found; {sessions.Count} total so far."
            });

            if (delaySeconds > 0 && page < maxPage)
                await Task.Delay((int)Math.Round(delaySeconds * 1000), ct);
        }

        // Save after list phase so we have all IDs even if detail fetching is interrupted
        progress?.Report(new DmSessionScrapeReport
        {
            Phase = DmSessionScrapePhase.Saving,
            SessionCount = sessions.Count,
            Detail = "Saving session list…"
        });
        await DmSessionJsonFile.SaveAsync(outputPath, sessions, ct);

        // ── Pass 2: fetch detail pages ────────────────────────────────────────
        if (skipDetailPages)
        {
            progress?.Report(new DmSessionScrapeReport
            {
                Phase = DmSessionScrapePhase.Complete,
                SessionCount = sessions.Count,
                DetailsFetched = 0,
                DetailsTotal = 0,
                Detail = $"Done. {sessions.Count} DM session(s) saved; detail pages skipped (list only)."
            });
            return sessions;
        }

        var toFetch = onlyFetchMissingDetailPages
            ? sessions.Values.Where(s => !s.DetailFetched).ToList()
            : sessions.Values.ToList();
        var fetched = 0;

        progress?.Report(new DmSessionScrapeReport
        {
            Phase = DmSessionScrapePhase.FetchingDetails,
            SessionCount = sessions.Count,
            DetailsFetched = fetched,
            DetailsTotal = toFetch.Count,
            Detail = toFetch.Count == 0
                ? (onlyFetchMissingDetailPages
                    ? "All session details already fetched."
                    : "No sessions to fetch.")
                : onlyFetchMissingDetailPages
                    ? $"Fetching details for {toFetch.Count} session(s) still missing detail…"
                    : $"Re-fetching details for all {toFetch.Count} session(s)…"
        });

        for (var i = 0; i < toFetch.Count; i++)
        {
            ct.ThrowIfCancellationRequested();

            var session = toFetch[i];
            var (ok, errCode) = await TryFetchDetailPageAsync(client, userId, session, ct);

            if (!ok)
            {
                progress?.Report(new DmSessionScrapeReport
                {
                    Phase = DmSessionScrapePhase.FetchingDetails,
                    SessionCount = sessions.Count,
                    DetailsFetched = fetched,
                    DetailsTotal = toFetch.Count,
                    Detail = $"Session {session.Id} ({session.AdventureTitle}): HTTP {errCode}; skipped."
                });
            }
            else
            {
                fetched++;

                // Save after every successful detail fetch for recoverability
                await DmSessionJsonFile.SaveAsync(outputPath, sessions, ct);

                progress?.Report(new DmSessionScrapeReport
                {
                    Phase = DmSessionScrapePhase.FetchingDetails,
                    SessionCount = sessions.Count,
                    DetailsFetched = fetched,
                    DetailsTotal = toFetch.Count,
                    Detail = $"Fetched detail {fetched} of {toFetch.Count}: {session.AdventureTitle}"
                });
            }

            if (delaySeconds > 0 && i < toFetch.Count - 1)
                await Task.Delay((int)Math.Round(delaySeconds * 1000), ct);
        }

        progress?.Report(new DmSessionScrapeReport
        {
            Phase = DmSessionScrapePhase.Complete,
            SessionCount = sessions.Count,
            DetailsFetched = fetched,
            DetailsTotal = toFetch.Count,
            Detail =
                $"Done. {sessions.Count} DM session(s) saved to {System.IO.Path.GetFileName(outputPath)}."
        });

        return sessions;
    }

    /// <summary>
    /// Fetches the show page for one session and writes <paramref name="outputPath"/>.
    /// If the id is not in the file yet, a row is created from <paramref name="session"/> (list fields only).
    /// </summary>
    public async Task<bool> FetchSingleSessionDetailAsync(
        string outputPath,
        DmSessionRecord session,
        double delaySeconds = 0.25,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(session.Id);

        var sessions = await DmSessionJsonFile.TryLoadAsync(outputPath, ct)
                       ?? new Dictionary<string, DmSessionRecord>();

        if (!sessions.TryGetValue(session.Id, out var target))
        {
            target = CopyListFieldsForNewEntry(session);
            sessions[session.Id] = target;
        }

        var client = await _auth.GetAuthenticatedClientAsync();
        var userId = await _auth.GetUserIdAsync();

        var (ok, _) = await TryFetchDetailPageAsync(client, userId, target, ct);
        if (!ok)
            return false;

        await DmSessionJsonFile.SaveAsync(outputPath, sessions, ct);
        if (delaySeconds > 0)
            await Task.Delay((int)Math.Round(delaySeconds * 1000), ct);
        return true;
    }

    /// <summary>
    /// Fetches detail pages for multiple sessions with one JSON load up front and a save after each success.
    /// </summary>
    public async Task<(int Succeeded, int Failed)> FetchSessionDetailsBatchAsync(
        string outputPath,
        IReadOnlyList<DmSessionRecord> toFetch,
        double delaySeconds = 0.25,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(toFetch);
        if (toFetch.Count == 0)
            return (0, 0);

        var dict = await DmSessionJsonFile.TryLoadAsync(outputPath, ct)
                   ?? new Dictionary<string, DmSessionRecord>();

        var client = await _auth.GetAuthenticatedClientAsync();
        var userId = await _auth.GetUserIdAsync();

        var succeeded = 0;
        var failed = 0;
        var delayMs = (int)Math.Round(delaySeconds * 1000);

        for (var i = 0; i < toFetch.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var session = toFetch[i];
            if (string.IsNullOrWhiteSpace(session.Id))
            {
                failed++;
                continue;
            }

            if (!dict.TryGetValue(session.Id, out var target))
            {
                target = CopyListFieldsForNewEntry(session);
                dict[session.Id] = target;
            }

            var (ok, _) = await TryFetchDetailPageAsync(client, userId, target, ct);
            if (!ok)
            {
                failed++;
                continue;
            }

            succeeded++;
            await DmSessionJsonFile.SaveAsync(outputPath, dict, ct);

            if (delayMs > 0 && i < toFetch.Count - 1)
                await Task.Delay(delayMs, ct);
        }

        return (succeeded, failed);
    }

    private static DmSessionRecord CopyListFieldsForNewEntry(DmSessionRecord s) =>
        new()
        {
            Id = s.Id,
            DateDmed = s.DateDmed,
            AdventureTitle = s.AdventureTitle,
            SessionNum = s.SessionNum,
            DmRewardChoice = s.DmRewardChoice,
            MagicItemsSummary = s.MagicItemsSummary,
            CharacterName = s.CharacterName,
            CharacterId = s.CharacterId,
            DetailFetched = false,
        };

    private static async Task<(bool Ok, int StatusCode)> TryFetchDetailPageAsync(
        System.Net.Http.HttpClient client,
        string userId,
        DmSessionRecord session,
        CancellationToken ct)
    {
        var url = $"https://www.adventurersleaguelog.com/users/{userId}/dm_log_entries/{Uri.EscapeDataString(session.Id)}";
        using var resp = await client.GetAsync(url, ct);
        if (!resp.IsSuccessStatusCode)
            return (false, (int)resp.StatusCode);

        var html = await resp.Content.ReadAsStringAsync(ct);
        ParseDetailPage(html, session);
        return (true, 0);
    }

    // ── Pagination ────────────────────────────────────────────────────────────

    private async Task<int> GetMaxPageAsync(System.Net.Http.HttpClient client, string userId, CancellationToken ct)
    {
        var url = $"https://www.adventurersleaguelog.com/users/{userId}/dm_log_entries?page=1";
        using var resp = await client.GetAsync(url, ct);
        if (!resp.IsSuccessStatusCode)
            return 1;

        var html = await resp.Content.ReadAsStringAsync(ct);
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Strategy 1: gather all hrefs containing "page=" and take the max
        int max = 1;
        var anchors = doc.DocumentNode.SelectNodes("//a[@href]");
        if (anchors != null)
        {
            foreach (var a in anchors)
            {
                var href = a.GetAttributeValue("href", string.Empty);
                var idx = href.IndexOf("page=", StringComparison.OrdinalIgnoreCase);
                if (idx < 0) continue;
                var after = href[(idx + 5)..];
                var ampIdx = after.IndexOf('&');
                var pageStr = ampIdx >= 0 ? after[..ampIdx] : after;
                if (int.TryParse(pageStr, out var p) && p > max)
                    max = p;
            }
        }

        return max;
    }

    // ── List page parsing ─────────────────────────────────────────────────────

    /// <summary>
    /// Parses the season9_format table from a list page, merging new/updated sessions into <paramref name="sessions"/>.
    /// Preserves <see cref="DmSessionRecord.DetailFetched"/> and detail fields for sessions that already exist.
    /// Returns the number of rows parsed.
    /// </summary>
    private static int ParseListPage(string html, Dictionary<string, DmSessionRecord> sessions)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // All three format tables are rendered; always read the season9_format div
        var rows = doc.DocumentNode
            .SelectNodes("//div[contains(@class,'season9_format')]//tbody[@id='menu_items']//tr");
        if (rows == null) return 0;

        int count = 0;
        foreach (var row in rows)
        {
            var cells = row.SelectNodes(".//td");
            if (cells == null || cells.Count < 7) continue; // notes/colspan rows have fewer cells

            var dateDmed       = CleanText(cells[0].InnerText);
            var adventureTitle = CleanText(cells[1].InnerText);
            var sessionNum     = CleanText(cells[2].InnerText);
            var rewardChoice   = CleanText(cells[3].InnerText);
            var magicSummary   = CleanText(cells[4].InnerText);

            // Character link: <a href="/users/X/characters/Y">Name</a> (optional)
            var charLink = cells[5].SelectSingleNode(".//a[@href]");
            var charName = charLink != null ? CleanText(charLink.InnerText) : null;
            var charId   = charLink != null ? LastSegment(charLink.GetAttributeValue("href", "")) : null;

            // Session ID: from the "Show" button link in the last cell
            var showLink = cells[6].SelectSingleNode(".//a[@title='Show Log Entry']");
            if (showLink == null) continue;
            var sessionId = LastSegment(showLink.GetAttributeValue("href", ""));
            if (string.IsNullOrWhiteSpace(sessionId)) continue;

            if (sessions.TryGetValue(sessionId, out var existing))
            {
                // Update list fields; preserve detail fields and DetailFetched flag
                existing.DateDmed       = dateDmed;
                existing.AdventureTitle = adventureTitle;
                existing.SessionNum     = sessionNum;
                existing.DmRewardChoice = rewardChoice;
                existing.MagicItemsSummary = NullIfEmpty(magicSummary);
                existing.CharacterName  = NullIfEmpty(charName);
                existing.CharacterId    = NullIfEmpty(charId);
            }
            else
            {
                sessions[sessionId] = new DmSessionRecord
                {
                    Id             = sessionId,
                    DateDmed       = dateDmed,
                    AdventureTitle = adventureTitle,
                    SessionNum     = sessionNum,
                    DmRewardChoice = rewardChoice,
                    MagicItemsSummary = NullIfEmpty(magicSummary),
                    CharacterName  = NullIfEmpty(charName),
                    CharacterId    = NullIfEmpty(charId),
                    DetailFetched  = false
                };
            }

            count++;
        }

        return count;
    }

    // ── Detail page parsing ───────────────────────────────────────────────────

    /// <summary>
    /// Parses the show page for a single DM session and populates <paramref name="record"/>'s detail fields.
    /// </summary>
    private static void ParseDetailPage(string html, DmSessionRecord record)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        record.AdventureTitle      = ParseLabeledField(doc, "Adventure Title") ?? record.AdventureTitle;
        record.SessionNum          = ParseLabeledField(doc, "Session")         ?? record.SessionNum;
        record.DateDmed            = ParseLabeledField(doc, "Date DMed")       ?? record.DateDmed;
        record.SessionLengthHours  = ParseLabeledField(doc, "Session Length (Hours)");
        record.PlayerLevel         = ParseLabeledField(doc, "Avg Party Level");
        record.LocationPlayed      = ParseLabeledField(doc, "Location DMed");
        record.XpGained            = ParseLabeledField(doc, "XP Gained");
        record.GpGained            = ParseLabeledField(doc, "GP +/-");
        record.DowntimeGained      = ParseLabeledField(doc, "Downtime +/-");
        record.RenownGained        = ParseLabeledField(doc, "Renown");
        record.NumSecretMissions   = ParseLabeledField(doc, "Missions");
        record.DatePlayed          = ParseLabeledField(doc, "Date Assigned");
        record.Notes               = ParseLabeledField(doc, "Notes");

        record.MagicItems = ParseMagicItemsTable(doc);
        record.DetailFetched = true;
    }

    /// <summary>
    /// Finds a <c>&lt;strong&gt;Label&lt;/strong&gt;</c> inside a form-group div and returns the adjacent text value.
    /// </summary>
    private static string? ParseLabeledField(HtmlDocument doc, string label)
    {
        var strong = doc.DocumentNode
            .SelectNodes("//div[contains(@class,'form-group')]//strong")
            ?.FirstOrDefault(n => n.InnerText.Trim().Equals(label, StringComparison.OrdinalIgnoreCase));
        if (strong == null) return null;

        // InnerText of the parent div contains "Label\nValue"; strip the label to get the value
        var fullText = HtmlEntity.DeEntitize(strong.ParentNode.InnerText).Trim();
        if (fullText.StartsWith(label, StringComparison.OrdinalIgnoreCase))
            fullText = fullText[label.Length..].Trim();

        return NullIfEmpty(fullText);
    }

    /// <summary>
    /// Parses the magic items table on the detail page.
    /// Columns: Character, Name, Rarity, Location, Table, Result, Counts? (Character column present when @character is nil, which is always the case on the DM session show page).
    /// </summary>
    private static List<DmSessionMagicItem>? ParseMagicItemsTable(HtmlDocument doc)
    {
        // Find the table that has a "Name" header (magic items table)
        var tables = doc.DocumentNode.SelectNodes("//table");
        if (tables == null) return null;

        HtmlNode? magicTable = null;
        Dictionary<string, int>? columnIndex = null;

        foreach (var table in tables)
        {
            var headers = table.SelectNodes(".//th");
            if (headers == null) continue;

            var headerTexts = headers.Select((h, i) => (Text: CleanText(h.InnerText), Index: i)).ToList();
            if (!headerTexts.Any(h => h.Text.Equals("Name", StringComparison.OrdinalIgnoreCase)))
                continue;

            magicTable = table;
            columnIndex = headerTexts.ToDictionary(
                h => h.Text.ToLowerInvariant(),
                h => h.Index);
            break;
        }

        if (magicTable == null || columnIndex == null) return null;

        var items = new List<DmSessionMagicItem>();
        var rows = magicTable.SelectNodes(".//tbody//tr");
        if (rows == null) return items;

        foreach (var row in rows)
        {
            var cells = row.SelectNodes(".//td");
            if (cells == null || cells.Count == 0) continue;

            string? Get(string header) =>
                columnIndex.TryGetValue(header, out var idx) && idx < cells.Count
                    ? NullIfEmpty(CleanText(cells[idx].InnerText))
                    : null;

            var countsText = Get("counts?");
            var counts = countsText != null &&
                         (countsText.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                          countsText == "1");

            var name = Get("name");
            if (string.IsNullOrWhiteSpace(name)) continue;

            items.Add(new DmSessionMagicItem
            {
                Name             = name,
                Character        = Get("character"),
                Rarity           = Get("rarity"),
                LocationFound    = Get("location"),
                Table            = Get("table"),
                TableResult      = Get("result"),
                CountsTowardLimit = counts
            });
        }

        return items.Count > 0 ? items : null;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string CleanText(string raw) =>
        HtmlEntity.DeEntitize(raw).Trim();

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    private static string LastSegment(string href)
    {
        if (string.IsNullOrEmpty(href)) return string.Empty;
        var trimmed = href.TrimEnd('/');
        var idx = trimmed.LastIndexOf('/');
        return idx >= 0 ? trimmed[(idx + 1)..] : trimmed;
    }
}
