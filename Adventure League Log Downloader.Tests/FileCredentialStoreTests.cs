using System;
using System.IO;
using System.Runtime.Versioning;
using Adventure_League_Log_Downloader.Services;

namespace Adventure_League_Log_Downloader.Tests;

/// <summary>
/// Each test instance gets its own settings folder under the temp directory. Dispose removes it.
/// </summary>
public class FileCredentialStoreTests : IDisposable
{
    private const string UnixOnly = "Unix file modes don't apply on Windows.";
    private const UnixFileMode Mode0600 = UnixFileMode.UserRead | UnixFileMode.UserWrite;
    private const UnixFileMode Mode0700 = Mode0600 | UnixFileMode.UserExecute;

    public static bool IsUnix => !OperatingSystem.IsWindows();

    private readonly string _settingsFolder = Path.Combine(Path.GetTempPath(), $"AllogDownloaderTest_{Guid.NewGuid():N}");
    private readonly FileCredentialStore _store;

    public FileCredentialStoreTests()
    {
        Directory.CreateDirectory(_settingsFolder);
        _store = FileCredentialStore.InSettingsFolder(_settingsFolder);
    }

    public void Dispose()
    {
        if (Directory.Exists(_settingsFolder))
            Directory.Delete(_settingsFolder, recursive: true);
    }

    [Fact]
    public void InSettingsFolder_UsesCredentialsSubfolderAndEntryName()
    {
        Assert.Equal(Path.Combine(_settingsFolder, "credentials", "AdventurersLeague.json"), _store.FilePath);
    }

    [Fact]
    public void Load_NothingSaved_ReturnsNull()
    {
        Assert.Null(_store.Load());
    }

    [Fact]
    public void Save_ThenLoad_RoundTripsUsernameAndPassword()
    {
        _store.Save("someone@example.com", "pässwörd ☃ with spaces");

        Assert.Equal(("someone@example.com", "pässwörd ☃ with spaces"), _store.Load());
    }

    [Fact]
    public void Save_Twice_KeepsLatest()
    {
        _store.Save("first", "one");
        _store.Save("second", "two");

        Assert.Equal(("second", "two"), _store.Load());
    }

    [Fact]
    public void Save_EmptyPassword_LoadsEmptyPassword()
    {
        _store.Save("someone", string.Empty);

        Assert.Equal(("someone", string.Empty), _store.Load());
    }

    [Fact]
    public void Save_FileDoesNotContainPasswordInPlainText()
    {
        _store.Save("someone", "hunter2-plaintext-check");

        var text = File.ReadAllText(_store.FilePath);

        Assert.DoesNotContain("hunter2-plaintext-check", text);
        Assert.Contains("\"obfuscatedPassword\"", text);
    }

    [Fact]
    public void Save_LeavesNoTempFile()
    {
        _store.Save("someone", "secret");

        Assert.Equal([_store.FilePath], Directory.GetFiles(_store.Folder));
    }

    [Fact]
    public void Delete_RemovesSavedCredential()
    {
        _store.Save("someone", "secret");

        _store.Delete();

        Assert.Null(_store.Load());
        Assert.False(File.Exists(_store.FilePath));
    }

    [Fact]
    public void Delete_NothingSaved_DoesNotThrow()
    {
        _store.Delete();
    }

    [Fact]
    public void Entries_WithDifferentNames_AreIndependent()
    {
        var other = FileCredentialStore.InSettingsFolder(_settingsFolder, "Warhorn");

        _store.Save("site-user", "site-password");
        other.Save("token", "abc123");

        Assert.Equal(("site-user", "site-password"), _store.Load());
        Assert.Equal(("token", "abc123"), other.Load());
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{}")]
    [InlineData("{\"username\":\"someone\",\"obfuscatedPassword\":\"not-v1\"}")]
    [InlineData("{\"username\":\"someone\",\"obfuscatedPassword\":\"v1:%%%\"}")]
    public void Load_UnreadableFile_ReturnsNull(string contents)
    {
        Directory.CreateDirectory(_store.Folder);
        File.WriteAllText(_store.FilePath, contents);

        Assert.Null(_store.Load());
    }

    [Fact(Skip = UnixOnly, SkipUnless = nameof(IsUnix))]
    [UnsupportedOSPlatform("windows")]
    public void Save_OnUnix_FolderIs0700AndFileIs0600()
    {
        _store.Save("someone", "secret");

        Assert.Equal(Mode0700, File.GetUnixFileMode(_store.Folder));
        Assert.Equal(Mode0600, File.GetUnixFileMode(_store.FilePath));
    }

    [Fact(Skip = UnixOnly, SkipUnless = nameof(IsUnix))]
    [UnsupportedOSPlatform("windows")]
    public void Save_OnUnix_TightensExistingLooseFolderAndFile()
    {
        var loose = Mode0700 | UnixFileMode.GroupRead | UnixFileMode.GroupExecute | UnixFileMode.OtherRead | UnixFileMode.OtherExecute;
        Directory.CreateDirectory(_store.Folder);
        File.SetUnixFileMode(_store.Folder, loose);
        File.WriteAllText(_store.FilePath, "old");
        File.SetUnixFileMode(_store.FilePath, Mode0600 | UnixFileMode.GroupRead | UnixFileMode.OtherRead);

        _store.Save("someone", "secret");

        Assert.Equal(Mode0700, File.GetUnixFileMode(_store.Folder));
        Assert.Equal(Mode0600, File.GetUnixFileMode(_store.FilePath));
    }
}
