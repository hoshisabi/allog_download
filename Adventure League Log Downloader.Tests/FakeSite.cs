using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Adventure_League_Log_Downloader.Services;

namespace Adventure_League_Log_Downloader.Tests;

/// <summary>
/// In-memory stand-in for adventurersleaguelog.com. Page 1 of the character list shows <see cref="Characters"/>; later
/// pages are empty, so the scraper's extra-page probing stops. <c>/users/42/characters/{id}.csv</c> serves the standard
/// CSV fixture unless <see cref="CsvStatus"/> says otherwise.
/// </summary>
internal sealed class FakeSite : HttpMessageHandler
{
    public const string UserId = "42";

    public List<(string Id, string Name)> Characters { get; } = [];
    public Dictionary<string, HttpStatusCode> CsvStatus { get; } = [];
    public List<string> CsvRequests { get; } = [];

    /// <summary>A signed-in session on this site.</summary>
    public IAdventurersLeagueAuth Auth() => new FakeAuth(this);

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var path = request.RequestUri!.AbsolutePath;
        var query = request.RequestUri.Query;

        if (path == $"/users/{UserId}/characters")
            return Task.FromResult(Html(query == "?page=1" ? CharacterPage() : "<html><body><table><tbody></tbody></table></body></html>"));

        var csvPrefix = $"/users/{UserId}/characters/";
        if (path.StartsWith(csvPrefix, StringComparison.Ordinal) && path.EndsWith(".csv", StringComparison.Ordinal))
        {
            var id = path[csvPrefix.Length..^".csv".Length];
            CsvRequests.Add(id);
            if (CsvStatus.TryGetValue(id, out var status))
                return Task.FromResult(new HttpResponseMessage(status));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(CsvTestFixtures.StandardCharacterCsv, Encoding.UTF8, "text/csv")
            });
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }

    private string CharacterPage()
    {
        var rows = string.Concat(Characters.Select(c =>
            $"<tr><td>S10</td><td><a href=\"/characters/{c.Id}\">{c.Name}</a></td><td>Elf</td><td>Wizard</td><td>5</td><td></td></tr>"));
        return $"<html><body><table><tbody>{rows}</tbody></table></body></html>";
    }

    private static HttpResponseMessage Html(string html) =>
        new(HttpStatusCode.OK) { Content = new StringContent(html, Encoding.UTF8, "text/html") };

    private sealed class FakeAuth(FakeSite site) : IAdventurersLeagueAuth
    {
        private readonly HttpClient _client = new(site) { BaseAddress = new Uri("https://www.adventurersleaguelog.com") };

        public Task<HttpClient> GetAuthenticatedClientAsync() => Task.FromResult(_client);

        public Task<string> GetUserIdAsync() => Task.FromResult(UserId);
    }
}
