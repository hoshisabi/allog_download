using System;
using System.IO;
using System.Threading.Tasks;
using Adventure_League_Log_Downloader.Services;

namespace Adventure_League_Log_Downloader.Tests;

/// <summary>
/// Each test instance gets its own isolated settings folder under %AppData% (unique product name).
/// Dispose removes it.
/// </summary>
public class SettingsServiceTests : IDisposable
{
    private readonly SettingsService _service;
    private readonly string _testFolder;

    public SettingsServiceTests()
    {
        var uniqueProduct = $"AllogDownloaderTest_{Guid.NewGuid():N}";
        _service = new SettingsService(product: uniqueProduct);
        _testFolder = Path.GetDirectoryName(_service.SettingsPath)!;
    }

    public void Dispose()
    {
        if (Directory.Exists(_testFolder))
            Directory.Delete(_testFolder, recursive: true);
    }

    [Fact]
    public async Task LoadAsync_MissingFile_ReturnsDefaults()
    {
        var settings = await _service.LoadAsync();

        Assert.Equal(0.25, settings.DelaySeconds);
        Assert.Equal("characters.json", settings.OutputFileName);
        Assert.False(string.IsNullOrEmpty(settings.OutputFolder));
        Assert.True(settings.DownloadOnlyMissingCharacterCsvs);
        Assert.True(settings.DownloadOnlyMissingDmSessionDetails);
    }

    [Fact]
    public async Task LoadAsync_CorruptFile_ReturnsDefaults()
    {
        File.WriteAllText(_service.SettingsPath, "}{not valid json{{");

        var settings = await _service.LoadAsync();

        Assert.Equal(0.25, settings.DelaySeconds);
        Assert.Equal("characters.json", settings.OutputFileName);
    }

    [Fact]
    public async Task SaveAsync_LoadAsync_RoundTrip()
    {
        var saved = new UserSettings
        {
            DelaySeconds = 1.5,
            OutputFolder = @"C:\test\output",
            OutputFileName = "my_chars.json",
            SkipCharacterCsvs = true,
            DownloadOnlyMissingCharacterCsvs = false,
        };

        await _service.SaveAsync(saved);
        var loaded = await _service.LoadAsync();

        Assert.Equal(1.5, loaded.DelaySeconds);
        Assert.Equal(@"C:\test\output", loaded.OutputFolder);
        Assert.Equal("my_chars.json", loaded.OutputFileName);
        Assert.True(loaded.SkipCharacterCsvs);
        Assert.False(loaded.DownloadOnlyMissingCharacterCsvs);
    }

    [Fact]
    public async Task LoadAsync_LegacyJsonMissingDownloadOnlyMissingKeys_DefaultsToTrue()
    {
        // Old settings files pre-date these keys; LoadAsync must backfill both as true.
        const string legacyJson = """
            {
              "delaySeconds": 0.5,
              "outputFolder": "C:\\old\\folder",
              "outputFileName": "chars.json"
            }
            """;
        File.WriteAllText(_service.SettingsPath, legacyJson);

        var settings = await _service.LoadAsync();

        Assert.True(settings.DownloadOnlyMissingCharacterCsvs);
        Assert.True(settings.DownloadOnlyMissingDmSessionDetails);
        Assert.Equal(0.5, settings.DelaySeconds);
    }
}
