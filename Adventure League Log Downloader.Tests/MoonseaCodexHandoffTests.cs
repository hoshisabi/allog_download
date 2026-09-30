using Adventure_League_Log_Downloader.Services;

namespace Adventure_League_Log_Downloader.Tests;

public class MoonseaCodexHandoffTests
{
    [Fact]
    public void BuildRevealCsvStartInfo_SelectsQuotedPathInExplorer()
    {
        var info = MoonseaCodexHandoff.BuildRevealCsvStartInfo(@"C:\My Data\character_123.csv");

        Assert.Equal("explorer.exe", info.FileName);
        Assert.Equal(@"/select,""C:\My Data\character_123.csv""", info.Arguments);
        Assert.True(info.UseShellExecute);
    }

    [Fact]
    public void BuildImportPageStartInfo_OpensMsCharactersPageViaShell()
    {
        var info = MoonseaCodexHandoff.BuildImportPageStartInfo();

        Assert.Equal("https://moonseacodex.com/characters", info.FileName);
        Assert.True(info.UseShellExecute);
    }
}
