using Adventure_League_Log_Downloader.Services;

namespace Allog.Cli;

/// <summary>
/// Finds the site login for a command: <c>--username</c> / <c>ALLOG_USERNAME</c>, and <c>--password</c>,
/// <c>--password-file</c>, or <c>ALLOG_PASSWORD</c> (first one set wins); otherwise the login saved in the OS
/// credential store. See SECURITY.md.
/// </summary>
internal static class CliCredentials
{
    public const string UsernameVariable = "ALLOG_USERNAME";
    public const string PasswordVariable = "ALLOG_PASSWORD";

    public sealed record Found(string Username, string Password, string Source);

    /// <returns>The login, or null with <paramref name="problem"/> explaining what's missing.</returns>
    public static Found? Resolve(
        string? usernameOption,
        string? passwordOption,
        FileInfo? passwordFile,
        Func<string, string?> environment,
        ICredentialStore store,
        out string problem)
    {
        problem = string.Empty;
        var username = FirstSet(usernameOption, environment(UsernameVariable));

        string? password;
        string? source;
        if (!string.IsNullOrEmpty(passwordOption))
        {
            (password, source) = (passwordOption, "--password");
        }
        else if (passwordFile is not null)
        {
            if (!passwordFile.Exists)
            {
                problem = $"Password file not found: {passwordFile.FullName}";
                return null;
            }
            (password, source) = (ReadFirstLine(passwordFile.FullName), "--password-file");
        }
        else if (!string.IsNullOrEmpty(environment(PasswordVariable)))
        {
            (password, source) = (environment(PasswordVariable), PasswordVariable);
        }
        else
        {
            (password, source) = (null, null);
        }

        if (password is null)
        {
            var saved = store.Load();
            if (saved is { } s && !string.IsNullOrEmpty(s.Password)
                && (username is null || string.Equals(username, s.Username, StringComparison.Ordinal)))
                return new Found(s.Username, s.Password, $"saved login ({store.Description})");
        }

        if (username is null || string.IsNullOrEmpty(password))
        {
            problem = $"""
                No login found. Give one of:
                  --username NAME with --password-file PATH (or --password, which other users on this machine can see in the process list)
                  {UsernameVariable} and {PasswordVariable} environment variables
                  a login saved by the desktop app (stored in {store.Description})
                """;
            return null;
        }

        return new Found(username, password, source!);
    }

    private static string? FirstSet(params string?[] values) => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

    /// <summary>First line, without the line ending; files from <c>systemd-creds</c> or <c>echo</c> end with one.</summary>
    private static string ReadFirstLine(string path)
    {
        using var reader = new StreamReader(path);
        return reader.ReadLine() ?? string.Empty;
    }
}
