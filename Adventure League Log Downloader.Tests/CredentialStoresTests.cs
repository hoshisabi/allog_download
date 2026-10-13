using System;
using System.IO;
using System.Runtime.Versioning;
using Adventure_League_Log_Downloader.Services;

namespace Adventure_League_Log_Downloader.Tests;

public class CredentialStoresTests : IDisposable
{
    public static bool IsWindows => OperatingSystem.IsWindows();

    public static bool IsNotWindows => !OperatingSystem.IsWindows();

    private readonly string _settingsFolder = Path.Combine(Path.GetTempPath(), $"AllogDownloaderTest_{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_settingsFolder))
            Directory.Delete(_settingsFolder, recursive: true);
    }

    [Fact]
    public void WindowsTarget_ForSiteLogin_IsTheNameEarlierVersionsSavedUnder()
    {
        Assert.Equal("AllogDownloader:AdventurersLeague", CredentialStores.WindowsTarget(CredentialStores.SiteLoginEntry));
    }

    [Fact(Skip = "Windows only.", SkipUnless = nameof(IsWindows))]
    [SupportedOSPlatform("windows")]
    public void ForCurrentOS_OnWindows_IsCredentialManager()
    {
        var store = CredentialStores.ForCurrentOS(_settingsFolder);

        Assert.IsType<WindowsCredentialStore>(store);
        Assert.Equal("Windows Credential Manager", store.Description);
    }

    [Fact(Skip = "Off Windows only.", SkipUnless = nameof(IsNotWindows))]
    public void ForCurrentOS_OffWindows_IsFileStoreInSettingsFolder()
    {
        var store = Assert.IsType<FileCredentialStore>(CredentialStores.ForCurrentOS(_settingsFolder));

        Assert.Equal(Path.Combine(_settingsFolder, "credentials", "AdventurersLeague.json"), store.FilePath);
    }

    [Fact(Skip = "Off Windows only.", SkipUnless = nameof(IsNotWindows))]
    public void ForCurrentOS_OffWindows_OtherEntryGetsItsOwnFile()
    {
        var store = Assert.IsType<FileCredentialStore>(CredentialStores.ForCurrentOS(_settingsFolder, "Warhorn"));

        Assert.Equal(Path.Combine(_settingsFolder, "credentials", "Warhorn.json"), store.FilePath);
    }

    [Fact(Skip = "Off Windows only.", SkipUnless = nameof(IsNotWindows))]
    public void ForCurrentOS_FromSettingsService_UsesTheFolderHoldingSettingsJson()
    {
        var settings = new SettingsService(product: $"AllogDownloaderTest_{Guid.NewGuid():N}");
        var settingsFolder = Path.GetDirectoryName(settings.SettingsPath)!;
        try
        {
            var store = Assert.IsType<FileCredentialStore>(CredentialStores.ForCurrentOS(settings));

            Assert.Equal(Path.Combine(settingsFolder, "credentials", "AdventurersLeague.json"), store.FilePath);
        }
        finally
        {
            Directory.Delete(settingsFolder, recursive: true);
        }
    }

    [Fact]
    public void FileStoreDescription_SaysObfuscatedNotEncrypted()
    {
        var description = FileCredentialStore.InSettingsFolder(_settingsFolder).Description;

        Assert.Contains("obfuscated, not encrypted", description);
        Assert.DoesNotContain("secure", description, StringComparison.OrdinalIgnoreCase);
    }
}
