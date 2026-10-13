using Allog.Cli;

namespace Adventure_League_Log_Downloader.Tests;

public class EnvFileTests
{
    [Fact]
    public void Parse_ReadsShellAndDotenvStyles()
    {
        var values = EnvFile.Parse("""
            # login for allog
            ALLOG_USERNAME=someone@example.com

            export ALLOG_PASSWORD="pass with spaces"
            SINGLE='quoted'
            SPACED = value
            EMPTY=
            not a pair
            =no key
            """);

        Assert.Equal("someone@example.com", values["ALLOG_USERNAME"]);
        Assert.Equal("pass with spaces", values["ALLOG_PASSWORD"]);
        Assert.Equal("quoted", values["SINGLE"]);
        Assert.Equal("value", values["SPACED"]);
        Assert.Equal(string.Empty, values["EMPTY"]);
        Assert.Equal(5, values.Count);
    }

    [Fact]
    public void Parse_HandlesWindowsLineEndingsAndEqualsInValue()
    {
        var values = EnvFile.Parse("ALLOG_USERNAME=a\r\nALLOG_PASSWORD=x=y=z\r\n");

        Assert.Equal("a", values["ALLOG_USERNAME"]);
        Assert.Equal("x=y=z", values["ALLOG_PASSWORD"]);
    }

    [Fact]
    public void Parse_LaterLineWins()
    {
        Assert.Equal("second", EnvFile.Parse("K=first\nK=second\n")["K"]);
    }
}
