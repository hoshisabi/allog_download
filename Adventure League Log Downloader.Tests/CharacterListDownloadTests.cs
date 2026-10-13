using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Adventure_League_Log_Downloader.Services;

namespace Adventure_League_Log_Downloader.Tests;

/// <summary>
/// Runs the full flow against a fake site: canned character-list pages and CSVs served by an in-memory HTTP handler.
/// Each test instance gets its own data folder.
/// </summary>
public class CharacterListDownloadTests : IDisposable
{
    private readonly string _dataDir = Path.Combine(Path.GetTempPath(), $"allog_download_test_{Guid.NewGuid():N}");
    private readonly FakeSite _site = new();
    private readonly string _jsonPath;

    public CharacterListDownloadTests()
    {
        Directory.CreateDirectory(_dataDir);
        _jsonPath = Path.Combine(_dataDir, "characters.json");
        _site.Characters.Add(("zed1", "Zed"));
        _site.Characters.Add(("amy2", "Amy"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_dataDir))
            Directory.Delete(_dataDir, recursive: true);
    }

    private Task<CharacterListDownloadResult> RunAsync(CharacterCsvMode mode, IProgress<CharacterScrapeReport>? progress = null, CancellationToken ct = default) =>
        CharacterListDownload.RunAsync(
            _site.Auth(), new CharacterListDownloadOptions(_jsonPath, DelaySeconds: 0, mode), progress, ct);

    private string CsvPath(string id) => Path.Combine(_dataDir, $"character_{id}.csv");

    [Fact]
    public async Task All_SavesSortedListAndEveryCsv()
    {
        var result = await RunAsync(CharacterCsvMode.All, ct: TestContext.Current.CancellationToken);

        Assert.Equal(["Amy", "Zed"], result.Characters.Select(c => c.Name));
        Assert.Equal((2, 0), (result.CsvRequested, result.CsvFailed));
        Assert.True(File.Exists(CsvPath("amy2")));
        Assert.True(File.Exists(CsvPath("zed1")));
        Assert.All(result.Characters, c => Assert.True(c.HasLocalCsv));
        Assert.Equal(_dataDir, result.DataDirectory);

        var saved = await CharacterJsonFile.TryLoadSortedAsync(_jsonPath, TestContext.Current.CancellationToken);
        Assert.Equal(["Amy", "Zed"], saved!.Select(c => c.Name));
        Assert.Equal("Downloaded 2 character CSV file(s) next to your character list.", result.CsvSummary);
    }

    [Fact]
    public async Task All_SetsLastSessionFromDownloadedCsv()
    {
        var result = await RunAsync(CharacterCsvMode.All, ct: TestContext.Current.CancellationToken);

        var expected = CharacterLogCsvReader.FormatSessionDateForDisplay(
            CharacterLogCsvReader.TryGetLatestSessionDatePlayed(CsvPath("amy2"))!.Value);
        Assert.Equal(expected, result.Characters.Single(c => c.Id == "amy2").LastSessionPlayed);
    }

    [Fact]
    public async Task OnlyMissing_DownloadsOnlyCharactersWithoutACsv()
    {
        File.WriteAllText(CsvPath("zed1"), CsvTestFixtures.StandardCharacterCsv);

        var result = await RunAsync(CharacterCsvMode.OnlyMissing, ct: TestContext.Current.CancellationToken);

        Assert.Equal(["amy2"], _site.CsvRequests);
        Assert.Equal((1, 0), (result.CsvRequested, result.CsvFailed));
    }

    [Fact]
    public async Task OnlyMissing_AllPresent_DownloadsNothing()
    {
        File.WriteAllText(CsvPath("zed1"), CsvTestFixtures.StandardCharacterCsv);
        File.WriteAllText(CsvPath("amy2"), CsvTestFixtures.StandardCharacterCsv);

        var result = await RunAsync(CharacterCsvMode.OnlyMissing, ct: TestContext.Current.CancellationToken);

        Assert.Empty(_site.CsvRequests);
        Assert.Equal("All character CSV files were already present; no CSV downloads.", result.CsvSummary);
    }

    [Fact]
    public async Task Skip_SavesListWithoutRequestingCsvs()
    {
        var result = await RunAsync(CharacterCsvMode.Skip, ct: TestContext.Current.CancellationToken);

        Assert.Empty(_site.CsvRequests);
        Assert.Equal(0, result.CsvRequested);
        Assert.True(File.Exists(_jsonPath));
        Assert.Equal("CSV downloads skipped (list only).", result.CsvSummary);
    }

    [Fact]
    public async Task CsvHttpError_CountsAsFailedAndKeepsGoing()
    {
        _site.CsvStatus["zed1"] = HttpStatusCode.NotFound;

        var result = await RunAsync(CharacterCsvMode.All, ct: TestContext.Current.CancellationToken);

        Assert.Equal((2, 1), (result.CsvRequested, result.CsvFailed));
        Assert.True(File.Exists(CsvPath("amy2")));
        Assert.False(File.Exists(CsvPath("zed1")));
        Assert.Contains("1 failed", result.CsvSummary);
    }

    [Fact]
    public async Task NoCsv_KeepsLastSessionFromPreviousList()
    {
        await CharacterJsonFile.SaveAsync(_jsonPath, new Dictionary<string, CharacterRecord>
        {
            ["zed1"] = new() { Id = "zed1", Name = "Zed", LastSessionPlayed = "2024-01-02" }
        }, TestContext.Current.CancellationToken);

        var result = await RunAsync(CharacterCsvMode.Skip, ct: TestContext.Current.CancellationToken);

        Assert.Equal("2024-01-02", result.Characters.Single(c => c.Id == "zed1").LastSessionPlayed);
        Assert.Null(result.Characters.Single(c => c.Id == "amy2").LastSessionPlayed);
    }

    [Fact]
    public async Task Progress_EndsWithSavingThenComplete()
    {
        var reports = new List<CharacterScrapeReport>();
        var progress = new SynchronousProgress(reports.Add);

        await RunAsync(CharacterCsvMode.All, progress, TestContext.Current.CancellationToken);

        Assert.Equal(
            [CharacterScrapePhase.Saving, CharacterScrapePhase.Complete],
            reports.TakeLast(2).Select(r => r.Phase));
        Assert.Contains(reports, r => r.Phase == CharacterScrapePhase.DownloadingCsvs);
        Assert.Equal(
            "Saved characters.json. Downloaded 2 character CSV file(s) next to your character list.",
            reports[^1].Detail);
    }

    [Fact]
    public async Task Cancelled_ThrowsAndSavesNothing()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => RunAsync(CharacterCsvMode.All, ct: cts.Token));
        Assert.False(File.Exists(_jsonPath));
    }

    /// <summary>Reports inline, so the test sees every report before RunAsync returns.</summary>
    private sealed class SynchronousProgress(Action<CharacterScrapeReport> onReport) : IProgress<CharacterScrapeReport>
    {
        public void Report(CharacterScrapeReport value) => onReport(value);
    }
}
