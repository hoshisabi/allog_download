using System.Text;
using Adventure_League_Log_Downloader.Services;

namespace Allog.Cli;

/// <summary>Everything a command touches outside itself, so tests can run commands against a fake site.</summary>
/// <param name="Prompt">Asks the user for a value (label, hide typing); null when there's no one to ask.</param>
/// <param name="ErrorIsTerminal">Whether <paramref name="Error"/> is a terminal, so progress can update one line in place.</param>
internal sealed record CliContext(
    ISettingsService Settings,
    ICredentialStore CredentialStore,
    Func<string, string, IAdventurersLeagueAuth> SignIn,
    Func<string, string?> Environment,
    TextWriter Out,
    TextWriter Error,
    Func<string, bool, string?> Prompt,
    bool ErrorIsTerminal)
{
    /// <summary>The real settings file, the OS credential store, the real site, and the console.</summary>
    public static CliContext ForThisMachine()
    {
        var settings = new SettingsService();
        return new CliContext(
            settings,
            CredentialStores.ForCurrentOS(settings),
            (username, password) => new AdventurersLeagueAuth(username, password),
            System.Environment.GetEnvironmentVariable,
            Console.Out,
            Console.Error,
            ConsolePrompt,
            !Console.IsErrorRedirected);
    }

    /// <summary>
    /// Reads a line from the console. Hidden input shows nothing as you type. With input piped in
    /// (<c>echo pw | allog login</c>) it reads the next line; at end of input it returns null.
    /// </summary>
    private static string? ConsolePrompt(string label, bool hidden)
    {
        Console.Error.Write(label);
        if (!hidden || Console.IsInputRedirected)
            return Console.ReadLine();

        var typed = new StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter)
                break;
            if (key.Key == ConsoleKey.Backspace)
            {
                if (typed.Length > 0)
                    typed.Length--;
            }
            else if (!char.IsControl(key.KeyChar))
            {
                typed.Append(key.KeyChar);
            }
        }

        Console.Error.WriteLine();
        return typed.ToString();
    }
}
