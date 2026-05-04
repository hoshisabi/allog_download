using System;
using System.Linq;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace Adventure_League_Log_Downloader.Services;

/// <summary>
/// HTML helpers for login discovery — used by <see cref="AdventurersLeagueAuth"/> and unit tests.
/// </summary>
internal static class AdventurersLeagueHtmlParsing
{
    public static (string name, string? value) ExtractCsrf(string html)
    {
        var doc = new HtmlAgilityPack.HtmlDocument();
        doc.LoadHtml(html);

        var input = doc.DocumentNode
            .SelectNodes("//input[@type='hidden']")
            ?.FirstOrDefault(n => string.Equals(n.GetAttributeValue("name", string.Empty), "authenticity_token", StringComparison.OrdinalIgnoreCase));
        if (input != null)
            return ("authenticity_token", input.GetAttributeValue("value", string.Empty));

        var input2 = doc.DocumentNode
            .SelectNodes("//input[@type='hidden']")
            ?.FirstOrDefault(n => string.Equals(n.GetAttributeValue("name", string.Empty), "__RequestVerificationToken", StringComparison.OrdinalIgnoreCase));
        if (input2 != null)
            return ("__RequestVerificationToken", input2.GetAttributeValue("value", string.Empty));

        return ("authenticity_token", string.Empty);
    }

    public static string? TryExtractUserIdFromHtml(string html)
    {
        var rx = new Regex(@"/users/(\d+)/", RegexOptions.IgnoreCase);
        var match = rx.Match(html);
        if (match.Success)
            return match.Groups[1].Value;

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
