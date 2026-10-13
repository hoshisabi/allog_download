using Adventure_League_Log_Downloader.Services;

namespace Allog.Cli;

/// <summary>Everything a command touches outside itself, so tests can run commands against a fake site.</summary>
internal sealed record CliContext(
    ISettingsService Settings,
    ICredentialStore CredentialStore,
    Func<string, string, IAdventurersLeagueAuth> SignIn,
    Func<string, string?> Environment,
    TextWriter Out,
    TextWriter Error)
{
    /// <summary>The real settings file, the OS credential store, and the real site.</summary>
    public static CliContext ForThisMachine()
    {
        var settings = new SettingsService();
        return new CliContext(
            settings,
            CredentialStores.ForCurrentOS(settings),
            (username, password) => new AdventurersLeagueAuth(username, password),
            System.Environment.GetEnvironmentVariable,
            Console.Out,
            Console.Error);
    }
}
