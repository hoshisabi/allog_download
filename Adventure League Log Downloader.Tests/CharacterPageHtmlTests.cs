using Adventure_League_Log_Downloader.Services;

namespace Adventure_League_Log_Downloader.Tests;

public class CharacterPageHtmlTests
{
    [Fact]
    public void GetMaxPageNumber_NoPagination_Returns1()
    {
        var html = "<html><body><p>hello</p></body></html>";
        Assert.Equal(1, CharacterPageHtml.GetMaxPageNumber(html));
    }

    [Fact]
    public void GetMaxPageNumber_TakesMaxFromPageQueryLinks()
    {
        var html = """
            <html><body>
            <a href="/users/99/characters?page=1">1</a>
            <a href="/users/99/characters?page=3">3</a>
            <a href="/users/99/characters?page=12">last</a>
            </body></html>
            """;
        Assert.Equal(12, CharacterPageHtml.GetMaxPageNumber(html));
    }

    [Fact]
    public void GetMaxPageNumber_PageQuery_IgnoresTrailingQueryParams()
    {
        var html = """<a href="/users/1/characters?page=5&amp;sort=name">x</a>""";
        Assert.Equal(5, CharacterPageHtml.GetMaxPageNumber(html));
    }

    [Fact]
    public void GetMaxPageNumber_LastChevronLink_UsedWhenNoHigherPageLinks()
    {
        var html = """
            <html><body>
            <a href="/users/1/characters?page=1">1</a>
            <a href="/users/1/characters?page=4">>></a>
            </body></html>
            """;
        Assert.Equal(4, CharacterPageHtml.GetMaxPageNumber(html));
    }

    [Fact]
    public void GetMaxPageNumber_LastChevron_UnicodeGuillemet()
    {
        var html = """<a href="/users/1/characters?page=9">»</a>""";
        Assert.Equal(9, CharacterPageHtml.GetMaxPageNumber(html));
    }

    [Fact]
    public void ParseCharacterTableRows_ValidRow_MapsColumns()
    {
        var html = """
            <table><tbody>
            <tr>
              <td>S10</td>
              <td><a href="/characters/abc123">Bob &amp; Co</a></td>
              <td>Elf</td>
              <td>Wizard</td>
              <td>5</td>
              <td>Tag</td>
            </tr>
            </tbody></table>
            """;
        var rows = CharacterPageHtml.ParseCharacterTableRows(html);
        Assert.Single(rows);
        var c = rows[0];
        Assert.Equal("abc123", c.Id);
        Assert.Equal("Bob & Co", c.Name);
        Assert.Equal("Elf", c.Race);
        Assert.Equal("Wizard", c.Class);
        Assert.Equal("5", c.Level);
        Assert.Equal("S10", c.Season);
        Assert.Equal("Tag", c.Tag);
    }

    [Fact]
    public void ParseCharacterTableRows_SkipsRowsWithoutCharacterId()
    {
        var html = """
            <table><tbody>
            <tr><td>x</td><td>no link here</td></tr>
            <tr><td>S1</td><td><a href="/characters/zzz">Zed</a></td><td></td><td></td><td></td><td></td></tr>
            </tbody></table>
            """;
        var rows = CharacterPageHtml.ParseCharacterTableRows(html);
        Assert.Single(rows);
        Assert.Equal("zzz", rows[0].Id);
    }

    [Fact]
    public void ParseCharacterTableRows_NoTable_ReturnsEmpty()
    {
        Assert.Empty(CharacterPageHtml.ParseCharacterTableRows("<html></html>"));
    }
}
