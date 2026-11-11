using System;
using Adventure_League_Log_Downloader.Services;

namespace Adventure_League_Log_Downloader.Tests;

public class WindowsPlatformShellTests
{
    [Fact]
    public void BuildRevealFileStartInfo_SelectsQuotedPathInExplorer()
    {
        var info = WindowsPlatformShell.BuildRevealFileStartInfo(@"C:\My Data\character_123.csv");

        Assert.Equal("explorer.exe", info.FileName);
        Assert.Equal(@"/select,""C:\My Data\character_123.csv""", info.Arguments);
        Assert.True(info.UseShellExecute);
    }

    [Theory]
    [InlineData(@"C:\Users\someone\AppData\Roaming\AllogDownloader")]
    [InlineData("https://moonseacodex.com/characters")]
    public void BuildShellOpenStartInfo_OpensTargetViaShell(string target)
    {
        var info = WindowsPlatformShell.BuildShellOpenStartInfo(target);

        Assert.Equal(target, info.FileName);
        Assert.Equal(string.Empty, info.Arguments);
        Assert.True(info.UseShellExecute);
    }

    [Fact(Skip = "Uses the Windows shell's known-folder API.", SkipUnless = nameof(IsWindows))]
    public void GetDownloadsFolder_OnWindows_ReturnsAnAbsolutePath()
    {
        var path = new WindowsPlatformShell().GetDownloadsFolder();

        Assert.True(System.IO.Path.IsPathFullyQualified(path), path);
    }

    public static bool IsWindows => OperatingSystem.IsWindows();
}
