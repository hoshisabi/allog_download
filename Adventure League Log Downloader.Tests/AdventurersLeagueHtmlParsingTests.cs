using Adventure_League_Log_Downloader.Services;

namespace Adventure_League_Log_Downloader.Tests;

public class AdventurersLeagueHtmlParsingTests
{
    [Fact]
    public void ExtractCsrf_RailsAuthenticityToken()
    {
        var html = """
            <form>
              <input type="hidden" name="authenticity_token" value="TOKEN123" />
            </form>
            """;
        var (name, value) = AdventurersLeagueHtmlParsing.ExtractCsrf(html);
        Assert.Equal("authenticity_token", name);
        Assert.Equal("TOKEN123", value);
    }

    [Fact]
    public void ExtractCsrf_RequestVerificationToken()
    {
        var html = """<input type="hidden" name="__RequestVerificationToken" value="RVF999" />""";
        var (name, value) = AdventurersLeagueHtmlParsing.ExtractCsrf(html);
        Assert.Equal("__RequestVerificationToken", name);
        Assert.Equal("RVF999", value);
    }

    [Fact]
    public void ExtractCsrf_NoneFound_DefaultsToAuthenticityTokenEmpty()
    {
        var html = "<html><body></body></html>";
        var (name, value) = AdventurersLeagueHtmlParsing.ExtractCsrf(html);
        Assert.Equal("authenticity_token", name);
        Assert.Equal(string.Empty, value);
    }

    [Fact]
    public void TryExtractUserIdFromHtml_FindsFirstNumericUserSegment()
    {
        var html = """<p>Welcome <a href="/users/424242/profile">you</a></p>""";
        Assert.Equal("424242", AdventurersLeagueHtmlParsing.TryExtractUserIdFromHtml(html));
    }

    [Fact]
    public void TryExtractUserIdFromHtml_AnchorHrefWhenNotInRawText()
    {
        var html = """
            <html><body>
            <nav><a href="/characters">List</a></nav>
            <a href="/users/777/characters">My chars</a>
            </body></html>
            """;
        Assert.Equal("777", AdventurersLeagueHtmlParsing.TryExtractUserIdFromHtml(html));
    }

    [Fact]
    public void TryExtractUserIdFromHtml_NoMatch_ReturnsNull()
    {
        Assert.Null(AdventurersLeagueHtmlParsing.TryExtractUserIdFromHtml("<html>No user links</html>"));
    }
}
