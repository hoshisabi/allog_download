using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Adventure_League_Log_Downloader.Services;

namespace Adventure_League_Log_Downloader.Tests;

/// <summary>
/// Process launches go to recording fakes, so nothing is started. Each test instance gets its own fake home folder.
/// </summary>
public class UnixPlatformShellTests : IDisposable
{
    private const string UnixPaths = "Uses Unix absolute paths.";

    public static bool IsUnix => !OperatingSystem.IsWindows();

    private readonly string _home = Path.Combine(Path.GetTempPath(), $"allog_shell_test_{Guid.NewGuid():N}");
    private readonly List<ProcessStartInfo> _ranAndWaited = [];
    private readonly List<ProcessStartInfo> _started = [];
    private int _runExitCode;

    public UnixPlatformShellTests() => Directory.CreateDirectory(_home);

    public void Dispose()
    {
        if (Directory.Exists(_home))
            Directory.Delete(_home, recursive: true);
    }

    private UnixPlatformShell Shell(bool isMacOS = false, string? xdgConfigHome = null) =>
        new(isMacOS, _home, xdgConfigHome, info => { _ranAndWaited.Add(info); return _runExitCode; }, _started.Add);

    private static string Describe(ProcessStartInfo info) => string.Join(" ", info.ArgumentList.Prepend(info.FileName));

    [Theory]
    [InlineData("XDG_DOWNLOAD_DIR=\"$HOME/Downloads\"", "Downloads")]
    [InlineData("XDG_DOWNLOAD_DIR=\"$HOME/Téléchargements\"", "Téléchargements")]
    [InlineData("  XDG_DOWNLOAD_DIR=\"$HOME/My Downloads\"  ", "My Downloads")]
    [InlineData("XDG_DOWNLOAD_DIR=\"$HOME/\"", "")]
    public void ParseXdgDownloadDir_HomeRelative_ResolvesUnderHome(string line, string relative)
    {
        var contents = $"# written by xdg-user-dirs-update\nXDG_DESKTOP_DIR=\"$HOME/Desktop\"\n{line}\n";

        var expected = relative.Length == 0 ? _home : Path.Combine(_home, relative);
        Assert.Equal(expected, UnixPlatformShell.ParseXdgDownloadDir(contents, _home));
    }

    [Fact]
    public void ParseXdgDownloadDir_AbsolutePath_ReturnsIt()
    {
        Assert.Equal("/data/downloads", UnixPlatformShell.ParseXdgDownloadDir("XDG_DOWNLOAD_DIR=\"/data/downloads\"", _home));
    }

    [Theory]
    [InlineData("")]
    [InlineData("XDG_DESKTOP_DIR=\"$HOME/Desktop\"")]
    [InlineData("# XDG_DOWNLOAD_DIR=\"$HOME/Commented\"")]
    [InlineData("XDG_DOWNLOAD_DIR=\"relative/not/allowed\"")]
    public void ParseXdgDownloadDir_NotSetOrInvalid_ReturnsNull(string contents)
    {
        Assert.Null(UnixPlatformShell.ParseXdgDownloadDir(contents, _home));
    }

    [Fact]
    public void GetDownloadsFolder_Linux_ReadsUserDirsFromDotConfig()
    {
        Directory.CreateDirectory(Path.Combine(_home, ".config"));
        File.WriteAllText(Path.Combine(_home, ".config", "user-dirs.dirs"), "XDG_DOWNLOAD_DIR=\"$HOME/Téléchargements\"\n");

        Assert.Equal(Path.Combine(_home, "Téléchargements"), Shell().GetDownloadsFolder());
    }

    [Fact]
    public void GetDownloadsFolder_Linux_HonorsXdgConfigHome()
    {
        var configHome = Path.Combine(_home, "elsewhere");
        Directory.CreateDirectory(configHome);
        File.WriteAllText(Path.Combine(configHome, "user-dirs.dirs"), "XDG_DOWNLOAD_DIR=\"$HOME/Fetched\"\n");

        Assert.Equal(Path.Combine(_home, "Fetched"), Shell(xdgConfigHome: configHome).GetDownloadsFolder());
    }

    [Fact]
    public void GetDownloadsFolder_Linux_NoUserDirs_FallsBackToHomeDownloads()
    {
        Assert.Equal(Path.Combine(_home, "Downloads"), Shell().GetDownloadsFolder());
    }

    [Fact]
    public void GetDownloadsFolder_MacOS_IsHomeDownloadsEvenWithUserDirs()
    {
        Directory.CreateDirectory(Path.Combine(_home, ".config"));
        File.WriteAllText(Path.Combine(_home, ".config", "user-dirs.dirs"), "XDG_DOWNLOAD_DIR=\"$HOME/Elsewhere\"\n");

        Assert.Equal(Path.Combine(_home, "Downloads"), Shell(isMacOS: true).GetDownloadsFolder());
    }

    [Fact(Skip = UnixPaths, SkipUnless = nameof(IsUnix))]
    public void RevealFile_Linux_FileManagerAnswers_DoesNotOpenFolder()
    {
        _runExitCode = 0;

        Shell().RevealFile("/home/someone/Downloads/moonseacodeximport.csv");

        var call = Assert.Single(_ranAndWaited);
        Assert.Equal("dbus-send", call.FileName);
        Assert.Contains("org.freedesktop.FileManager1.ShowItems", call.ArgumentList);
        Assert.Contains("array:string:file:///home/someone/Downloads/moonseacodeximport.csv", call.ArgumentList);
        Assert.Empty(_started);
    }

    [Fact(Skip = UnixPaths, SkipUnless = nameof(IsUnix))]
    public void RevealFile_Linux_NoFileManager_OpensContainingFolder()
    {
        _runExitCode = 1;

        Shell().RevealFile("/home/someone/Downloads/moonseacodeximport.csv");

        Assert.Single(_ranAndWaited);
        Assert.Equal(["xdg-open /home/someone/Downloads"], _started.Select(Describe));
    }

    [Fact]
    public void RevealFile_MacOS_UsesOpenDashR()
    {
        Shell(isMacOS: true).RevealFile("/Users/someone/Downloads/moonseacodeximport.csv");

        Assert.Empty(_ranAndWaited);
        Assert.Equal(["open -R /Users/someone/Downloads/moonseacodeximport.csv"], _started.Select(Describe));
    }

    [Theory]
    [InlineData(false, "xdg-open")]
    [InlineData(true, "open")]
    public void OpenFolderAndOpenUrl_UseOsOpener(bool isMacOS, string opener)
    {
        var shell = Shell(isMacOS);

        shell.OpenFolder("/some/folder with spaces");
        shell.OpenUrl("https://moonseacodex.com/characters");

        Assert.Equal(
            [$"{opener} /some/folder with spaces", $"{opener} https://moonseacodex.com/characters"],
            _started.Select(Describe));
        Assert.All(_started, info => Assert.False(info.UseShellExecute));
    }

    [Fact(Skip = UnixPaths, SkipUnless = nameof(IsUnix))]
    public void ToFileUri_EscapesSpacesNonAsciiAndCommas()
    {
        Assert.Equal(
            "file:///home/someone/My%20Data/T%C3%A9l%C3%A9chargements/a%2Cb.csv",
            UnixPlatformShell.ToFileUri("/home/someone/My Data/Téléchargements/a,b.csv"));
    }
}
