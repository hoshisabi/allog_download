using System;
using CredentialManagement;

namespace Adventure_League_Log_Downloader.Services;

public sealed class WindowsCredentialStore : ICredentialStore
{
    private const string Target = "AllogDownloader:AdventurersLeague";

    public bool IsAvailable => true; // On Windows desktop, Credential Manager is available.

    public void Save(string username, string password)
    {
        using var cred = new Credential
        {
            Target = Target,
            Username = username,
            Password = password,
            PersistanceType = PersistanceType.LocalComputer,
            Type = CredentialType.Generic
        };
        cred.Save();
    }

    public (string Username, string Password)? Load()
    {
        using var cred = new Credential { Target = Target, Type = CredentialType.Generic };
        return cred.Load() ? (cred.Username ?? string.Empty, cred.Password ?? string.Empty) : null;
    }

    public void Delete()
    {
        using var cred = new Credential { Target = Target, Type = CredentialType.Generic };
        cred.Delete();
    }
}
