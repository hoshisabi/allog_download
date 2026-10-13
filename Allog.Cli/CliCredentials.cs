using Adventure_League_Log_Downloader.Services;

namespace Allog.Cli;

/// <summary>
/// Finds the site login for a command: <c>--username</c> / <c>ALLOG_USERNAME</c>, and <c>--password</c>,
/// <c>--password-file</c>, or <c>ALLOG_PASSWORD</c> (first one set wins); otherwise the login saved in the OS
/// credential store, when one is given. See SECURITY.md.
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
        ICredentialStore? store,
        out string problem)
    {
        var username = FirstSet(usernameOption, environment(UsernameVariable));
        if (!TryGetPassword(passwordOption, passwordFile, environment, out var password, out var source, out problem))
            return null;

        if (password is null && store is not null)
        {
            var saved = store.Load();
            if (saved is { } s && !string.IsNullOrEmpty(s.Password)
                && (username is null || string.Equals(username, s.Username, StringComparison.Ordinal)))
                return new Found(s.Username, s.Password, $"the saved login in {store.Description}");
        }

        if (username is null || string.IsNullOrEmpty(password))
        {
            var saved = store is null ? string.Empty : $"\n  a saved login (allog login, or the desktop app), stored in {store.Description}";
            problem = $"""
                No login found. Give one of:
                  --username NAME with --password-file PATH (or --password, which other users on this machine can see in the process list)
                  --env-file PATH, or the {UsernameVariable} and {PasswordVariable} environment variables
                """ + saved;
            return null;
        }

        return new Found(username, password, source!);
    }

    /// <summary>
    /// The password from <c>--password</c>, <c>--password-file</c>, or <c>ALLOG_PASSWORD</c> (first one set wins); null
    /// when none is set.
    /// </summary>
    /// <returns>False with <paramref name="problem"/> set if the password file is missing.</returns>
    public static bool TryGetPassword(
        string? passwordOption,
        FileInfo? passwordFile,
        Func<string, string?> environment,
        out string? password,
        out string? source,
        out string problem)
    {
        problem = string.Empty;
        (password, source) = (null, null);
        if (!string.IsNullOrEmpty(passwordOption))
        {
            (password, source) = (passwordOption, "--password");
        }
        else if (passwordFile is not null)
        {
            if (!passwordFile.Exists)
            {
                problem = $"Password file not found: {passwordFile.FullName}";
                return false;
            }
            (password, source) = (ReadFirstLine(passwordFile.FullName), "--password-file");
        }
        else if (!string.IsNullOrEmpty(environment(PasswordVariable)))
        {
            (password, source) = (environment(PasswordVariable), PasswordVariable);
        }

        return true;
    }

    public static string? FirstSet(params string?[] values) => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

    /// <summary>First line, without the line ending; files from <c>systemd-creds</c> or <c>echo</c> end with one.</summary>
    private static string ReadFirstLine(string path)
    {
        using var reader = new StreamReader(path);
        return reader.ReadLine() ?? string.Empty;
    }
}
