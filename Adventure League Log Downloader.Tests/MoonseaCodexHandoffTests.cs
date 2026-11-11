using System;
using System.Collections.Generic;
using System.IO;
using Adventure_League_Log_Downloader.Services;

namespace Adventure_League_Log_Downloader.Tests;

public class MoonseaCodexHandoffTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), $"allog_handoff_test_{Guid.NewGuid():N}");
    private readonly string _downloads;
    private readonly RecordingShell _shell;

    public MoonseaCodexHandoffTests()
    {
        _downloads = Path.Combine(_tempDir, "Downloads");
        Directory.CreateDirectory(_tempDir);
        _shell = new RecordingShell(_downloads);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public void Open_WritesToDownloadsRevealsFileThenOpensImportPage()
    {
        var siteCsv = Path.Combine(_tempDir, "character_1.csv");
        File.WriteAllText(siteCsv, MoonseaCodexCsvWriterTests.SiteCsv);

        var importPath = MoonseaCodexHandoff.Open(_shell, siteCsv);

        Assert.Equal(Path.Combine(_downloads, "moonseacodeximport.csv"), importPath);
        Assert.True(File.Exists(importPath));
        Assert.Equal(
            [$"reveal {importPath}", "url https://moonseacodex.com/characters"],
            _shell.Calls);
    }

    [Fact]
    public void Open_UnreadableSiteCsv_ThrowsWithoutOpeningAnything()
    {
        var siteCsv = Path.Combine(_tempDir, "not_a_site_csv.csv");
        File.WriteAllText(siteCsv, CsvTestFixtures.StandardCharacterCsv);

        Assert.Throws<InvalidDataException>(() => MoonseaCodexHandoff.Open(_shell, siteCsv));
        Assert.Empty(_shell.Calls);
    }

    private sealed class RecordingShell(string downloadsFolder) : IPlatformShell
    {
        public List<string> Calls { get; } = [];

        public string GetDownloadsFolder() => downloadsFolder;

        public void RevealFile(string path) => Calls.Add($"reveal {path}");

        public void OpenFolder(string path) => Calls.Add($"folder {path}");

        public void OpenUrl(string url) => Calls.Add($"url {url}");
    }
}
