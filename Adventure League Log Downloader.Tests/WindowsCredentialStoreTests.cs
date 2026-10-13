using System;
using System.Runtime.Versioning;
using Adventure_League_Log_Downloader.Services;

namespace Adventure_League_Log_Downloader.Tests;

/// <summary>
/// Uses the real Credential Manager with a unique target per test instance. Dispose removes it.
/// Skipped on other OSes.
/// </summary>
[SupportedOSPlatform("windows")]
public class WindowsCredentialStoreTests : IDisposable
{
    private const string WindowsOnly = "Windows Credential Manager is only available on Windows.";

    public static bool IsWindows => OperatingSystem.IsWindows();

    private readonly WindowsCredentialStore _store = new($"AllogDownloaderTest:{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (IsWindows)
            _store.Delete();
    }

    [Fact(Skip = WindowsOnly, SkipUnless = nameof(IsWindows))]
    public void Load_NothingSaved_ReturnsNull()
    {
        Assert.Null(_store.Load());
    }

    [Fact(Skip = WindowsOnly, SkipUnless = nameof(IsWindows))]
    public void Save_ThenLoad_RoundTripsUsernameAndPassword()
    {
        _store.Save("someone@example.com", "pässwörd ☃ with spaces");

        var loaded = _store.Load();

        Assert.Equal(("someone@example.com", "pässwörd ☃ with spaces"), loaded);
    }

    [Fact(Skip = WindowsOnly, SkipUnless = nameof(IsWindows))]
    public void Save_Twice_KeepsLatest()
    {
        _store.Save("first", "one");
        _store.Save("second", "two");

        Assert.Equal(("second", "two"), _store.Load());
    }

    [Fact(Skip = WindowsOnly, SkipUnless = nameof(IsWindows))]
    public void Save_EmptyPassword_LoadsEmptyPassword()
    {
        _store.Save("someone", string.Empty);

        Assert.Equal(("someone", string.Empty), _store.Load());
    }

    [Fact(Skip = WindowsOnly, SkipUnless = nameof(IsWindows))]
    public void Delete_RemovesSavedCredential()
    {
        _store.Save("someone", "secret");

        _store.Delete();

        Assert.Null(_store.Load());
    }

    [Fact(Skip = WindowsOnly, SkipUnless = nameof(IsWindows))]
    public void Delete_NothingSaved_DoesNotThrow()
    {
        _store.Delete();
    }

    [Fact(Skip = WindowsOnly, SkipUnless = nameof(IsWindows))]
    public void Entries_WithDifferentTargets_AreIndependent()
    {
        var other = new WindowsCredentialStore($"AllogDownloaderTest:{Guid.NewGuid():N}");
        try
        {
            _store.Save("site-user", "site-password");
            other.Save("token", "abc123");

            Assert.Equal(("site-user", "site-password"), _store.Load());
            Assert.Equal(("token", "abc123"), other.Load());
        }
        finally
        {
            other.Delete();
        }
    }
}
