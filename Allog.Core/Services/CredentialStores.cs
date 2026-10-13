using System;
using System.IO;

namespace Adventure_League_Log_Downloader.Services;

/// <summary>Picks the credential store for this OS (see SECURITY.md, "Stores by platform").</summary>
public static class CredentialStores
{
    /// <summary>The site login. Also the Credential Manager target suffix and the file store's file name.</summary>
    public const string SiteLoginEntry = "AdventurersLeague";

    /// <summary>
    /// Windows: Credential Manager entry <c>AllogDownloader:&lt;entry&gt;</c> (the name earlier versions used for the site
    /// login). Elsewhere: <see cref="FileCredentialStore"/> under <paramref name="settingsFolder"/>, until the Linux
    /// keyring and macOS Keychain stores land (TASKS.md A7b-3, A7b-4).
    /// </summary>
    /// <param name="settingsFolder">Folder holding <c>settings.json</c>.</param>
    public static ICredentialStore ForCurrentOS(string settingsFolder, string entryName = SiteLoginEntry)
    {
        if (OperatingSystem.IsWindows())
            return new WindowsCredentialStore(WindowsTarget(entryName));

        return FileCredentialStore.InSettingsFolder(settingsFolder, entryName);
    }

    /// <summary>Credential Manager target name for <paramref name="entryName"/>.</summary>
    public static string WindowsTarget(string entryName) => "AllogDownloader:" + entryName;

    /// <summary><see cref="ForCurrentOS(string, string)"/> for the folder <paramref name="settings"/> saves to.</summary>
    public static ICredentialStore ForCurrentOS(ISettingsService settings, string entryName = SiteLoginEntry) =>
        ForCurrentOS(Path.GetDirectoryName(settings.SettingsPath)!, entryName);
}
