using System.CommandLine;
using Adventure_League_Log_Downloader.Services;

namespace Allog.Cli;

/// <summary>The login options shared by commands that sign in, and how they combine (see <see cref="CliCredentials"/>).</summary>
internal sealed class LoginOptions
{
    public Option<string?> Username { get; } = new("--username", "-u")
    {
        Description = $"Site username. Default: {CliCredentials.UsernameVariable}, else the saved login."
    };

    public Option<string?> Password { get; } = new("--password")
    {
        Description = "Site password. Other users on this machine can see it in the process list; prefer --password-file, "
                      + $"--env-file, or {CliCredentials.PasswordVariable}."
    };

    public Option<FileInfo?> PasswordFile { get; } = new("--password-file")
    {
        Description = "File whose first line is the site password (e.g. from systemd-creds)."
    };

    public Option<FileInfo?> EnvFile { get; } = new("--env-file")
    {
        Description = $"File of KEY=VALUE lines; {CliCredentials.UsernameVariable} and {CliCredentials.PasswordVariable} "
                      + "from it take precedence over the same environment variables."
    };

    public void AddTo(Command command)
    {
        command.Options.Add(Username);
        command.Options.Add(Password);
        command.Options.Add(PasswordFile);
        command.Options.Add(EnvFile);
    }

    public Values Read(ParseResult parse) =>
        new(parse.GetValue(Username), parse.GetValue(Password), parse.GetValue(PasswordFile), parse.GetValue(EnvFile));

    internal sealed record Values(string? Username, string? Password, FileInfo? PasswordFile, FileInfo? EnvFile)
    {
        public static Values None { get; } = new(null, null, null, null);

        /// <summary>The environment with <see cref="EnvFile"/>'s values layered on top.</summary>
        /// <returns>False with <paramref name="problem"/> set if the env file is missing.</returns>
        /// <remarks>Warns on <paramref name="warnings"/> about <c>ALLOG_</c> keys it doesn't use, e.g. <c>ALLOG_USER</c>.</remarks>
        public bool TryGetEnvironment(
            Func<string, string?> processEnvironment, TextWriter warnings, out Func<string, string?> environment, out string problem)
        {
            problem = string.Empty;
            environment = processEnvironment;
            if (EnvFile is null)
                return true;

            if (!EnvFile.Exists)
            {
                problem = $"Env file not found: {EnvFile.FullName}";
                return false;
            }

            var fromFile = Cli.EnvFile.Parse(File.ReadAllText(EnvFile.FullName));
            foreach (var key in fromFile.Keys.Where(k => k.StartsWith("ALLOG_", StringComparison.Ordinal)
                                                         && k is not CliCredentials.UsernameVariable and not CliCredentials.PasswordVariable))
                warnings.WriteLine($"{EnvFile.Name}: {key} isn't used; the keys are {CliCredentials.UsernameVariable} and {CliCredentials.PasswordVariable}.");
            environment = name => fromFile.TryGetValue(name, out var value) ? value : processEnvironment(name);
            return true;
        }

        public CliCredentials.Found? Resolve(CliContext context, bool useSavedLogin, out string problem)
        {
            if (!TryGetEnvironment(context.Environment, context.Error, out var environment, out problem))
                return null;
            return CliCredentials.Resolve(
                Username, Password, PasswordFile, environment, useSavedLogin ? context.CredentialStore : null, out problem);
        }
    }
}
