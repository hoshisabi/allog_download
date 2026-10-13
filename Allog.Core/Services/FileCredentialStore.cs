using System;
using System.IO;
using System.Text.Json;

namespace Adventure_League_Log_Downloader.Services;

/// <summary>
/// One credential in a JSON file with an obfuscated password (<see cref="PasswordObfuscation"/>), for systems with no OS
/// credential store (headless Linux, macOS until Keychain support lands). On Unix the folder is <c>0700</c> and the file
/// <c>0600</c>; the file is written under a temporary name with those permissions and then renamed, so the password is
/// never on disk with looser permissions.
/// </summary>
public sealed class FileCredentialStore : ICredentialStore
{
    public const string DefaultEntryName = CredentialStores.SiteLoginEntry;

    private const UnixFileMode PrivateFolderMode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute;
    private const UnixFileMode PrivateFileMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    /// <param name="folder">Folder holding one file per entry, e.g. <c>credentials</c> beside <c>settings.json</c>.</param>
    /// <param name="entryName">File name without extension; one store per entry (site login, Warhorn token, ...).</param>
    public FileCredentialStore(string folder, string entryName = DefaultEntryName)
    {
        Folder = folder;
        FilePath = Path.Combine(folder, entryName + ".json");
    }

    /// <summary><c>credentials</c> under the given settings folder (the folder that holds <c>settings.json</c>).</summary>
    public static FileCredentialStore InSettingsFolder(string settingsFolder, string entryName = DefaultEntryName) =>
        new(Path.Combine(settingsFolder, "credentials"), entryName);

    public string Folder { get; }

    public string FilePath { get; }

    public bool IsAvailable => true;

    public string Description => $"a file on this computer (obfuscated, not encrypted): {FilePath}";

    public void Save(string username, string password)
    {
        CreateFolder();

        var json = JsonSerializer.SerializeToUtf8Bytes(
            new StoredCredential(username, PasswordObfuscation.Encode(password)), JsonOptions);
        var tempPath = FilePath + ".tmp";
        var options = new FileStreamOptions { Mode = FileMode.Create, Access = FileAccess.Write };
        if (!OperatingSystem.IsWindows())
            options.UnixCreateMode = PrivateFileMode;

        // UnixCreateMode only applies when the file is created, so clear any temp file left by an interrupted save.
        File.Delete(tempPath);
        using (var stream = new FileStream(tempPath, options))
            stream.Write(json);
        File.Move(tempPath, FilePath, overwrite: true);
    }

    /// <summary>The saved credential, or null when none is saved or the file can't be read as one.</summary>
    public (string Username, string Password)? Load()
    {
        if (!File.Exists(FilePath))
            return null;

        try
        {
            var stored = JsonSerializer.Deserialize<StoredCredential>(File.ReadAllBytes(FilePath), JsonOptions);
            if (stored?.Username is null || stored.ObfuscatedPassword is null)
                return null;
            return (stored.Username, PasswordObfuscation.Decode(stored.ObfuscatedPassword));
        }
        catch (Exception ex) when (ex is JsonException or FormatException)
        {
            return null;
        }
    }

    public void Delete()
    {
        if (File.Exists(FilePath))
            File.Delete(FilePath);
    }

    private void CreateFolder()
    {
        if (OperatingSystem.IsWindows())
        {
            Directory.CreateDirectory(Folder);
            return;
        }

        // CreateDirectory's mode only applies to folders it creates, so tighten an existing one too.
        Directory.CreateDirectory(Folder, PrivateFolderMode);
        File.SetUnixFileMode(Folder, PrivateFolderMode);
    }

    private sealed record StoredCredential(string? Username, string? ObfuscatedPassword);
}
