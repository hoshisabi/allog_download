using System.Net.Http;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;

namespace Adventure_League_Log_Downloader.Services;

/// <summary>
/// Fetches lower-priority account data: saved locations, saved player DM profiles, and campaigns (list + per-campaign log pages).
/// </summary>
public sealed class AdditionalSiteDataScraper
{
    private readonly IAdventurersLeagueAuth _auth;

    public AdditionalSiteDataScraper(IAdventurersLeagueAuth auth)
    {
        _auth = auth;
    }

    public async Task<List<LocationRecord>> DownloadLocationsAsync(
        string outputFolder,
        double delaySeconds,
        CancellationToken ct = default)
    {
        var client = await _auth.GetAuthenticatedClientAsync();
        var userId = await _auth.GetUserIdAsync();
        using var resp = await client.GetAsync($"/users/{userId}/locations", ct);
        resp.EnsureSuccessStatusCode();
        var html = await resp.Content.ReadAsStringAsync(ct);
        var list = ParseLocations(html);
        await AdditionalSiteDataJson.SaveLocationsAsync(outputFolder,
            new LocationsFileDto { FetchedAtUtc = DateTimeOffset.UtcNow, Locations = list },
            ct);
        await MaybeDelayAsync(delaySeconds, ct);
        return list;
    }

    public async Task<List<PlayerDmRecord>> DownloadPlayerDmsAsync(
        string outputFolder,
        double delaySeconds,
        CancellationToken ct = default)
    {
        var client = await _auth.GetAuthenticatedClientAsync();
        var userId = await _auth.GetUserIdAsync();
        using var resp = await client.GetAsync($"/users/{userId}/player_dms", ct);
        resp.EnsureSuccessStatusCode();
        var html = await resp.Content.ReadAsStringAsync(ct);
        var list = ParsePlayerDms(html);
        await AdditionalSiteDataJson.SavePlayerDmsAsync(outputFolder,
            new PlayerDmsFileDto { FetchedAtUtc = DateTimeOffset.UtcNow, PlayerDms = list },
            ct);
        await MaybeDelayAsync(delaySeconds, ct);
        return list;
    }

    public async Task<CampaignsFileDto> DownloadCampaignsAsync(
        string outputFolder,
        double delaySeconds,
        CancellationToken ct = default)
    {
        var client = await _auth.GetAuthenticatedClientAsync();
        var userId = await _auth.GetUserIdAsync();

        using var indexResp = await client.GetAsync($"/users/{userId}/campaigns", ct);
        indexResp.EnsureSuccessStatusCode();
        var indexHtml = await indexResp.Content.ReadAsStringAsync(ct);
        await MaybeDelayAsync(delaySeconds, ct);

        var (dmRows, playingRows) = ParseCampaignIndex(indexHtml);

        var detailIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var r in dmRows)
            if (!string.IsNullOrWhiteSpace(r.Id))
                detailIds.Add(r.Id);
        foreach (var r in playingRows)
            if (!string.IsNullOrWhiteSpace(r.CampaignId))
                detailIds.Add(r.CampaignId);

        var details = new Dictionary<string, CampaignDetailRecord>(StringComparer.Ordinal);
        var orderedIds = detailIds.OrderBy(x => x, StringComparer.Ordinal).ToList();
        for (var i = 0; i < orderedIds.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            if (i > 0)
                await MaybeDelayAsync(delaySeconds, ct);

            var campaignId = orderedIds[i];
            var detail = await FetchCampaignDetailAsync(client, userId, campaignId, delaySeconds, ct);
            details[campaignId] = detail;
        }

        var dto = new CampaignsFileDto
        {
            FetchedAtUtc = DateTimeOffset.UtcNow,
            DmCampaigns = dmRows,
            Playing = playingRows,
            Details = details,
        };
        await AdditionalSiteDataJson.SaveCampaignsAsync(outputFolder, dto, ct);
        return dto;
    }

    private async Task<CampaignDetailRecord> FetchCampaignDetailAsync(
        HttpClient client,
        string userId,
        string campaignId,
        double delaySeconds,
        CancellationToken ct)
    {
        var entries = new List<CampaignLogEntryRecord>();
        string? campaignName = null;

        var basePath = $"/users/{userId}/campaigns/{Uri.EscapeDataString(campaignId)}";
        using var firstResp = await client.GetAsync(basePath, ct);
        if (!firstResp.IsSuccessStatusCode)
        {
            return new CampaignDetailRecord { Name = string.Empty, LogEntries = entries };
        }

        var html = await firstResp.Content.ReadAsStringAsync(ct);
        var maxPage = ExtractMaxCampaignLogPageFromHtml(html, campaignId);

        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        var h2 = doc.DocumentNode.SelectNodes("//h2")?.FirstOrDefault();
        if (h2 != null)
            campaignName = CleanText(h2.InnerText);

        ParseCampaignLogPage(html, entries);

        for (var page = 2; page <= maxPage; page++)
        {
            ct.ThrowIfCancellationRequested();
            await MaybeDelayAsync(delaySeconds, ct);

            using var resp = await client.GetAsync($"{basePath}?page={page}", ct);
            if (!resp.IsSuccessStatusCode)
                break;

            var pageHtml = await resp.Content.ReadAsStringAsync(ct);
            ParseCampaignLogPage(pageHtml, entries);
        }

        return new CampaignDetailRecord
        {
            Name = campaignName ?? string.Empty,
            LogEntries = entries,
        };
    }

    private static int ExtractMaxCampaignLogPageFromHtml(string html, string campaignId)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var max = 1;
        var anchors = doc.DocumentNode.SelectNodes("//a[@href]");
        if (anchors == null)
            return 1;

        var needle = "/campaigns/" + campaignId;
        foreach (var a in anchors)
        {
            var href = a.GetAttributeValue("href", string.Empty);
            if (href.IndexOf("page=", StringComparison.OrdinalIgnoreCase) < 0)
                continue;
            if (href.IndexOf(needle, StringComparison.OrdinalIgnoreCase) < 0)
                continue;

            var idx = href.IndexOf("page=", StringComparison.OrdinalIgnoreCase);
            var after = href[(idx + 5)..];
            var ampIdx = after.IndexOf('&');
            var pageStr = ampIdx >= 0 ? after[..ampIdx] : after;
            if (int.TryParse(pageStr, out var p) && p > max)
                max = p;
        }

        return max;
    }

    private static List<LocationRecord> ParseLocations(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var list = new List<LocationRecord>();
        var rows = doc.DocumentNode.SelectNodes("//table[contains(@class,'table-dms')]//tbody[@id='menu_items']/tr");
        if (rows == null)
            return list;

        foreach (var row in rows)
        {
            var cells = row.SelectNodes("./td");
            if (cells == null || cells.Count < 2)
                continue;

            var name = CleanText(cells[0].InnerText);
            var edit = cells[1].SelectSingleNode(".//a[contains(@href,'/locations/') and contains(@href,'/edit')]");
            var id = edit != null ? TryExtractIdFromLocationsEditHref(edit.GetAttributeValue("href", "")) : null;
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name))
                continue;

            list.Add(new LocationRecord { Id = id, Name = name });
        }

        return list;
    }

    private static List<PlayerDmRecord> ParsePlayerDms(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var list = new List<PlayerDmRecord>();
        var rows = doc.DocumentNode.SelectNodes("//table[contains(@class,'table-dms')]//tbody[@id='menu_items']/tr");
        if (rows == null)
            return list;

        foreach (var row in rows)
        {
            var cells = row.SelectNodes("./td");
            if (cells == null || cells.Count < 3)
                continue;

            var name = CleanText(cells[0].InnerText);
            var dci = CleanText(cells[1].InnerText);
            var edit = cells[2].SelectSingleNode(".//a[contains(@href,'/player_dms/') and contains(@href,'/edit')]");
            var id = edit != null ? TryExtractIdFromPlayerDmsEditHref(edit.GetAttributeValue("href", "")) : null;
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name))
                continue;

            list.Add(new PlayerDmRecord { Id = id, Name = name, Dci = string.IsNullOrEmpty(dci) ? null : dci });
        }

        return list;
    }

    private static (List<DmCampaignListRow> Dm, List<CampaignPlayingListRow> Playing) ParseCampaignIndex(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var dm = new List<DmCampaignListRow>();
        var playing = new List<CampaignPlayingListRow>();

        var dmingRoot = doc.DocumentNode.SelectSingleNode("//*[@id='dming_campaigns_list']");
        if (dmingRoot != null)
        {
            var rows = dmingRoot.SelectNodes(".//tbody[@id='menu_items']/tr");
            if (rows != null)
            {
                foreach (var row in rows)
                {
                    var cells = row.SelectNodes("./td");
                    if (cells == null || cells.Count < 2)
                        continue;

                    var link = cells[0].SelectSingleNode(".//a[@href]");
                    if (link == null)
                        continue;

                    var href = link.GetAttributeValue("href", "");
                    var id = TryExtractCampaignIdFromHref(href);
                    var name = CleanText(link.InnerText);
                    var count = cells.Count > 1 ? CleanText(cells[1].InnerText) : null;
                    if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name))
                        continue;

                    dm.Add(new DmCampaignListRow { Id = id, Name = name, PlayerCount = count });
                }
            }
        }

        var playingRoot = doc.DocumentNode.SelectSingleNode("//*[@id='playing_campaigns_list']");
        if (playingRoot != null)
        {
            var rows = playingRoot.SelectNodes(".//tbody[@id='menu_items']/tr");
            if (rows != null)
            {
                foreach (var row in rows)
                {
                    var cells = row.SelectNodes("./td");
                    if (cells == null || cells.Count < 2)
                        continue;

                    var campLink = cells[0].SelectSingleNode(".//a[@href]");
                    var charLink = cells[1].SelectSingleNode(".//a[@href]");
                    if (campLink == null || charLink == null)
                        continue;

                    var cHref = campLink.GetAttributeValue("href", "");
                    var chHref = charLink.GetAttributeValue("href", "");
                    var campaignId = TryExtractCampaignIdFromHref(cHref);
                    var characterId = TryExtractCharacterIdFromHref(chHref);
                    if (string.IsNullOrWhiteSpace(campaignId))
                        continue;

                    playing.Add(new CampaignPlayingListRow
                    {
                        CampaignId = campaignId,
                        CampaignName = CleanText(campLink.InnerText),
                        CharacterId = characterId ?? string.Empty,
                        CharacterName = CleanText(charLink.InnerText),
                    });
                }
            }
        }

        return (dm, playing);
    }

    private static void ParseCampaignLogPage(string html, List<CampaignLogEntryRecord> sink)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var rows = doc.DocumentNode
            .SelectNodes("//div[contains(@class,'season9_format')]//tbody[@id='log-entries']/tr");
        if (rows == null)
            return;

        foreach (var row in rows)
        {
            var cells = row.SelectNodes("./td");
            if (cells == null)
                continue;

            if (cells.Count == 1)
            {
                var note = CleanText(cells[0].InnerText);
                if (sink.Count > 0 && !string.IsNullOrEmpty(note))
                {
                    var last = sink[^1];
                    last.Notes = string.IsNullOrEmpty(last.Notes) ? note : last.Notes + "\n" + note;
                }

                continue;
            }

            if (cells.Count < 8)
                continue;

            var showLink = cells[7].SelectSingleNode(".//a[contains(@href,'campaign_log_entries')]");
            var href = showLink?.GetAttributeValue("href", "") ?? "";
            var idMatch = Regex.Match(href, @"/campaign_log_entries/(\d+)", RegexOptions.IgnoreCase);
            if (!idMatch.Success)
                continue;

            sink.Add(new CampaignLogEntryRecord
            {
                Id = idMatch.Groups[1].Value,
                DatePlayed = CleanText(cells[0].InnerText),
                AdventureTitle = CleanText(cells[1].InnerText),
                SessionNum = CleanText(cells[2].InnerText),
                LevelsOrXp = CleanText(cells[3].InnerText),
                Gp = CleanText(cells[4].InnerText),
                Downtime = CleanText(cells[5].InnerText),
                MagicItems = CleanText(cells[6].InnerText),
            });
        }
    }

    private static string? TryExtractIdFromLocationsEditHref(string href)
    {
        var m = Regex.Match(href, @"/locations/(\d+)/edit", RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value : null;
    }

    private static string? TryExtractIdFromPlayerDmsEditHref(string href)
    {
        var m = Regex.Match(href, @"/player_dms/(\d+)/edit", RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value : null;
    }

    private static string? TryExtractCampaignIdFromHref(string href)
    {
        var m = Regex.Match(href, @"/campaigns/(\d+)(?:/|$|\?)", RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value : null;
    }

    private static string? TryExtractCharacterIdFromHref(string href)
    {
        var m = Regex.Match(href, @"/characters/(\d+)(?:/|$|\?)", RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value : null;
    }

    private static string CleanText(string? raw) =>
        HtmlEntity.DeEntitize((raw ?? string.Empty).Trim());

    private static Task MaybeDelayAsync(double delaySeconds, CancellationToken ct)
    {
        if (delaySeconds <= 0)
            return Task.CompletedTask;
        return Task.Delay((int)Math.Round(delaySeconds * 1000), ct);
    }
}
