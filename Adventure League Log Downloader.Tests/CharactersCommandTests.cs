using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Adventure_League_Log_Downloader.Services;
using Allog.Cli;

namespace Adventure_League_Log_Downloader.Tests;

/// <summary>Runs <c>allog characters</c> against the in-memory <see cref="FakeSite"/>.</summary>
public class CharactersCommandTests : IDisposable
{
    private readonly string _dataDir = Path.Combine(Path.GetTempPath(), $"allog_cli_test_{Guid.NewGuid():N}");
    private readonly FakeSite _site = new();
    private readonly MemoryCredentialStore _store = new();
    private readonly MemorySettingsService _settings;
    private readonly StringWriter _out = new();
    private readonly StringWriter _error = new();
    private readonly List<(string Username, string Password)> _signIns = [];
    private Exception? _signInFailure;

    public CharactersCommandTests()
    {
        Directory.CreateDirectory(_dataDir);
        _site.Characters.Add(("zed1", "Zed"));
        _site.Characters.Add(("amy2", "Amy"));
        _settings = new MemorySettingsService(new UserSettings
        {
            OutputFolder = _dataDir,
            OutputFileName = "characters.json",
            DelaySeconds = 0,
            DownloadOnlyMissingCharacterCsvs = true
        });
        _store.Saved = ("saved-user", "saved-pass");
    }

    public void Dispose()
    {
        if (Directory.Exists(_dataDir))
            Directory.Delete(_dataDir, recursive: true);
    }

    private Task<int> RunAsync(CharactersCommand.Args args, CancellationToken ct = default) =>
        CharactersCommand.RunAsync(
            args,
            new CliContext(
                _settings,
                _store,
                (username, password) =>
                {
                    _signIns.Add((username, password));
                    if (_signInFailure is not null)
                        throw _signInFailure;
                    return _site.Auth();
                },
                _ => null,
                _out,
                _error),
            ct);

    private static CharactersCommand.Args Args(
        FileInfo? output = null, string? csv = null, double? delay = null, string? username = null, bool quiet = false) =>
        new(output, csv, delay, username, Password: null, PasswordFile: null, quiet);

    [Fact]
    public async Task Defaults_UseSettingsAndSavedLogin()
    {
        var exit = await RunAsync(Args(), TestContext.Current.CancellationToken);

        Assert.Equal(0, exit);
        Assert.Equal([("saved-user", "saved-pass")], _signIns);
        Assert.True(File.Exists(Path.Combine(_dataDir, "characters.json")));
        Assert.Equal(["amy2", "zed1"], _site.CsvRequests);
        Assert.Contains($"Saved 2 character(s) to {Path.Combine(_dataDir, "characters.json")}", _out.ToString());
        Assert.Contains("Downloaded 2 character CSV file(s)", _out.ToString());
    }

    [Fact]
    public async Task Success_RecordsLastDownloadInSettings()
    {
        await RunAsync(Args(), TestContext.Current.CancellationToken);

        Assert.NotNull(_settings.Current.LastWebsiteDownloadUtc);
        Assert.Equal(1, _settings.SaveCount);
    }

    [Fact]
    public async Task OutputOption_OverridesSettings()
    {
        var output = new FileInfo(Path.Combine(_dataDir, "elsewhere", "mine.json"));

        var exit = await RunAsync(Args(output: output), TestContext.Current.CancellationToken);

        Assert.Equal(0, exit);
        Assert.True(output.Exists);
        Assert.True(File.Exists(Path.Combine(_dataDir, "elsewhere", "character_amy2.csv")));
    }

    [Theory]
    [InlineData("skip", 0)]
    [InlineData("all", 2)]
    public async Task CsvOption_ControlsDownloads(string csv, int expectedRequests)
    {
        await RunAsync(Args(csv: csv), TestContext.Current.CancellationToken);

        Assert.Equal(expectedRequests, _site.CsvRequests.Count);
    }

    [Fact]
    public async Task NoCsvOption_FollowsSkipSetting()
    {
        _settings.Current.SkipCharacterCsvs = true;

        await RunAsync(Args(), TestContext.Current.CancellationToken);

        Assert.Empty(_site.CsvRequests);
    }

    [Fact]
    public async Task CsvFailures_ExitThree()
    {
        _site.CsvStatus["zed1"] = HttpStatusCode.InternalServerError;

        Assert.Equal(3, await RunAsync(Args(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task NoLogin_ExitsTwoWithoutSigningIn()
    {
        _store.Saved = null;

        var exit = await RunAsync(Args(), TestContext.Current.CancellationToken);

        Assert.Equal(2, exit);
        Assert.Empty(_signIns);
        Assert.StartsWith("No login found.", _error.ToString());
    }

    [Fact]
    public async Task NoOutputLocation_ExitsTwo()
    {
        _settings.Current.OutputFolder = string.Empty;

        Assert.Equal(2, await RunAsync(Args(), TestContext.Current.CancellationToken));
        Assert.Contains("--output", _error.ToString());
    }

    [Fact]
    public async Task NegativeDelay_ExitsTwo()
    {
        Assert.Equal(2, await RunAsync(Args(delay: -1), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SignInFailure_ExitsOneWithMessage()
    {
        _signInFailure = new UnauthorizedAccessException("Login appears to have failed. Please check your credentials.");

        var exit = await RunAsync(Args(), TestContext.Current.CancellationToken);

        Assert.Equal(1, exit);
        Assert.Contains("Error: Login appears to have failed.", _error.ToString());
        Assert.Equal(0, _settings.SaveCount);
    }

    [Fact]
    public async Task Cancelled_Exits130()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.Equal(130, await RunAsync(Args(), cts.Token));
    }

    [Fact]
    public async Task Progress_GoesToErrorStream_UnlessQuiet()
    {
        await RunAsync(Args(), TestContext.Current.CancellationToken);
        Assert.Contains("Signing in as saved-user (login from saved login (test store))", _error.ToString());
        Assert.Contains("Saving character list", _error.ToString());

        _error.GetStringBuilder().Clear();
        await RunAsync(Args(quiet: true), TestContext.Current.CancellationToken);
        Assert.Equal(string.Empty, _error.ToString());
    }
}
