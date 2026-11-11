using System;

namespace Adventure_League_Log_Downloader.Services;

public interface ICredentialStore
{
    bool IsAvailable { get; }

    /// <summary>
    /// Where a saved credential goes, worded per SECURITY.md for "saved in ..." messages, e.g. "Windows Credential Manager".
    /// </summary>
    string Description { get; }

    void Save(string username, string password);
    (string Username, string Password)? Load();
    void Delete();
}
