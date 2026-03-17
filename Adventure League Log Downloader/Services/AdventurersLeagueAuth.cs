using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace Adventure_League_Log_Downloader.Services;

/// <summary>
/// Placeholder implementation. This needs to be implemented to perform the real login
/// against adventurersleaguelog.com and return an authenticated HttpClient and user id.
/// For example, you may need to:
/// - Initialize an HttpClient with a CookieContainer (via HttpClientHandler)
/// - Fetch the login page to obtain any anti-forgery tokens
/// - POST the login form with credentials
/// - Determine the user id by inspecting a profile page or API response
/// </summary>
public sealed class AdventurersLeagueAuth : IAdventurersLeagueAuth
{
    private const string BaseUrl = "https://www.adventurersleaguelog.com";
    private readonly string _username;
    private readonly string _password;
    private readonly CookieContainer _cookies = new();
    private readonly HttpClientHandler _handler;
    private readonly HttpClient _client;
    private bool _loggedIn;
    private string? _userId;

    public AdventurersLeagueAuth(string username, string password)
    {
        _username = username ?? string.Empty;
        _password = password ?? string.Empty;

        _handler = new HttpClientHandler
        {
            CookieContainer = _cookies,
            AllowAutoRedirect = true,
            AutomaticDecompression = DecompressionMethods.All
        };

        _client = new HttpClient(_handler)
        {
            BaseAddress = new Uri(BaseUrl)
        };
        _client.DefaultRequestHeaders.UserAgent.ParseAdd("AllogDownloader/1.0 (+https://github.com/)");
        _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
        _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xhtml+xml"));
        _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml", 0.9));
    }

    public async Task<HttpClient> GetAuthenticatedClientAsync()
    {
        if (!_loggedIn)
        {
            await LoginAsync();
        }
        return _client;
    }

    public async Task<string> GetUserIdAsync()
    {
        if (!_loggedIn)
        {
            await LoginAsync();
        }
        if (!string.IsNullOrEmpty(_userId))
            return _userId!;

        // Try to discover user id by scanning common pages for links like /users/{id}/...
        var pagesToProbe = new[]
        {
            "/", // home/dashboard
            "/characters", // may redirect or include links
            "/users" // directory or profile index
        };

        foreach (var rel in pagesToProbe)
        {
            using var resp = await _client.GetAsync(rel);
            if (!resp.IsSuccessStatusCode) continue;
            var html = await resp.Content.ReadAsStringAsync();
            var id = TryExtractUserIdFromHtml(html);
            if (!string.IsNullOrEmpty(id))
            {
                _userId = id;
                return _userId!;
            }
        }

        throw new InvalidOperationException("Unable to determine user id after login.");
    }

    private async Task LoginAsync()
    {
        if (_loggedIn) return;
        if (string.IsNullOrWhiteSpace(_username) || string.IsNullOrWhiteSpace(_password))
            throw new InvalidOperationException("Username and password are required.");

        // 1) Load login page to get CSRF token
        var loginPath = "/users/sign_in"; // typical Devise path
        using var loginPageResp = await _client.GetAsync(loginPath);
        if (!loginPageResp.IsSuccessStatusCode)
            throw new InvalidOperationException($"Failed to load login page: {(int)loginPageResp.StatusCode} {loginPageResp.ReasonPhrase}");
        var loginHtml = await loginPageResp.Content.ReadAsStringAsync();
        var (tokenName, tokenValue) = ExtractCsrf(loginHtml);

        // 2) Post credentials
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("user[email]", _username),
            new KeyValuePair<string, string>("user[password]", _password),
            new KeyValuePair<string, string>(tokenName, tokenValue ?? string.Empty),
            new KeyValuePair<string, string>("commit", "Log in")
        });

        using var postResp = await _client.PostAsync(loginPath, content);
        if (!postResp.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Login POST failed: {(int)postResp.StatusCode} {postResp.ReasonPhrase}");
        }

        // 3) Verify we are logged in by checking that a subsequent request shows a user link
        using var home = await _client.GetAsync("/");
        if (!home.IsSuccessStatusCode)
            throw new InvalidOperationException("Login verification failed (home not reachable)");
        var homeHtml = await home.Content.ReadAsStringAsync();
        var id = TryExtractUserIdFromHtml(homeHtml);
        _loggedIn = id != null; // consider logged in if we can see user links
        _userId = id;

        if (!_loggedIn)
        {
            throw new UnauthorizedAccessException("Login appears to have failed. Please check your credentials.");
        }
    }

    private static (string name, string? value) ExtractCsrf(string html)
    {
        // Try common CSRF patterns found in Rails/ASP.NET
        var doc = new HtmlAgilityPack.HtmlDocument();
        doc.LoadHtml(html);

        // input[name=authenticity_token]
        var input = doc.DocumentNode
            .SelectNodes("//input[@type='hidden']")
            ?.FirstOrDefault(n => string.Equals(n.GetAttributeValue("name", string.Empty), "authenticity_token", StringComparison.OrdinalIgnoreCase));
        if (input != null)
        {
            return ("authenticity_token", input.GetAttributeValue("value", string.Empty));
        }

        // input[name=__RequestVerificationToken]
        var input2 = doc.DocumentNode
            .SelectNodes("//input[@type='hidden']")
            ?.FirstOrDefault(n => string.Equals(n.GetAttributeValue("name", string.Empty), "__RequestVerificationToken", StringComparison.OrdinalIgnoreCase));
        if (input2 != null)
        {
            return ("__RequestVerificationToken", input2.GetAttributeValue("value", string.Empty));
        }

        // Default to authenticity_token with empty value if not found
        return ("authenticity_token", string.Empty);
    }

    private static string? TryExtractUserIdFromHtml(string html)
    {
        // find any /users/{id}/... link
        var rx = new Regex(@"/users/(\d+)/", RegexOptions.IgnoreCase);
        var match = rx.Match(html);
        if (match.Success)
        {
            return match.Groups[1].Value;
        }

        // Try parsing DOM for anchors to be robust
        var doc = new HtmlAgilityPack.HtmlDocument();
        doc.LoadHtml(html);
        var anchors = doc.DocumentNode.SelectNodes("//a[@href]");
        if (anchors != null)
        {
            foreach (var a in anchors)
            {
                var href = a.GetAttributeValue("href", string.Empty);
                var m = rx.Match(href);
                if (m.Success) return m.Groups[1].Value;
            }
        }
        return null;
    }
}
