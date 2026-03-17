using System;

namespace Adventure_League_Log_Downloader.Services;

public interface ICredentialStore
{
    bool IsAvailable { get; }
    void Save(string username, string password);
    (string Username, string Password)? Load();
    void Delete();
}
