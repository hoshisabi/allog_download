using System;
using System.Threading;
using System.Threading.Tasks;
using Adventure_League_Log_Downloader.Services;

namespace Adventure_League_Log_Downloader.Tests;

internal sealed class MemoryCredentialStore : ICredentialStore
{
    public (string Username, string Password)? Saved { get; set; }

    public bool IsAvailable => true;

    public string Description => "test store";

    public void Save(string username, string password) => Saved = (username, password);

    public (string Username, string Password)? Load() => Saved;

    public void Delete() => Saved = null;
}

internal sealed class MemorySettingsService(UserSettings settings) : ISettingsService
{
    public UserSettings Current { get; private set; } = settings;

    public int SaveCount { get; private set; }

    public string SettingsPath => "/memory/settings.json";

    public Task<UserSettings> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(Current);

    public Task SaveAsync(UserSettings settings)
    {
        Current = settings;
        SaveCount++;
        return Task.CompletedTask;
    }
}
