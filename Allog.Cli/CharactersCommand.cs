using System.CommandLine;
using Adventure_League_Log_Downloader.Services;

namespace Allog.Cli;

/// <summary><c>allog characters</c>: the desktop app's Download button, without selecting characters.</summary>
internal static class CharactersCommand
{
    internal sealed record Args(
        FileInfo? Output, string? Csv, double? Delay, string? Username, string? Password, FileInfo? PasswordFile, bool Quiet);

    public static Command Build(Func<CliContext> context)
    {
        var output = new Option<FileInfo?>("--output", "-o")
        {
            Description = "Where to save the character list. Character CSVs go in the same folder. Default: the data location set in the desktop app."
        };
        var csv = new Option<string?>("--csv")
        {
            Description = "Which character CSVs to download: skip, missing (only characters without one), or all. Default: the desktop app's setting."
        };
        csv.AcceptOnlyFromAmong("skip", "missing", "all");
        var delay = new Option<double?>("--delay")
        {
            Description = "Seconds to wait between site requests. Default: the desktop app's setting (0.25)."
        };
        var username = new Option<string?>("--username", "-u")
        {
            Description = $"Site username. Default: {CliCredentials.UsernameVariable}, else the saved login."
        };
        var password = new Option<string?>("--password")
        {
            Description = "Site password. Other users on this machine can see it in the process list; prefer --password-file or "
                          + CliCredentials.PasswordVariable + "."
        };
        var passwordFile = new Option<FileInfo?>("--password-file")
        {
            Description = "File whose first line is the site password (e.g. from systemd-creds)."
        };
        var quiet = new Option<bool>("--quiet", "-q") { Description = "Only print the final summary and errors." };

        var command = new Command("characters", "Sign in, download your character list and character CSVs, and save the list as JSON.")
        {
            output, csv, delay, username, password, passwordFile, quiet
        };
        command.SetAction((parse, ct) => RunAsync(
            new Args(
                parse.GetValue(output), parse.GetValue(csv), parse.GetValue(delay), parse.GetValue(username),
                parse.GetValue(password), parse.GetValue(passwordFile), parse.GetValue(quiet)),
            context(),
            ct));
        return command;
    }

    internal static async Task<int> RunAsync(Args args, CliContext context, CancellationToken ct)
    {
        var settings = await context.Settings.LoadAsync(ct);

        var outputPath = args.Output?.FullName ?? DefaultOutputPath(settings);
        if (outputPath is null)
        {
            await context.Error.WriteLineAsync("No output file: pass --output, or set a data location in the desktop app.");
            return ExitCodes.Usage;
        }

        var delaySeconds = args.Delay ?? settings.DelaySeconds;
        if (delaySeconds < 0)
        {
            await context.Error.WriteLineAsync("--delay can't be negative.");
            return ExitCodes.Usage;
        }

        var mode = args.Csv switch
        {
            "skip" => CharacterCsvMode.Skip,
            "missing" => CharacterCsvMode.OnlyMissing,
            "all" => CharacterCsvMode.All,
            _ => settings.SkipCharacterCsvs ? CharacterCsvMode.Skip
                : settings.DownloadOnlyMissingCharacterCsvs ? CharacterCsvMode.OnlyMissing
                : CharacterCsvMode.All
        };

        var login = CliCredentials.Resolve(
            args.Username, args.Password, args.PasswordFile, context.Environment, context.CredentialStore, out var problem);
        if (login is null)
        {
            await context.Error.WriteLineAsync(problem);
            return ExitCodes.Usage;
        }

        var progress = args.Quiet ? null : new LineProgress(context.Error);
        if (!args.Quiet)
            await context.Error.WriteLineAsync($"Signing in as {login.Username} (login from {login.Source})…");

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            var result = await CharacterListDownload.RunAsync(
                context.SignIn(login.Username, login.Password),
                new CharacterListDownloadOptions(outputPath, delaySeconds, mode),
                progress,
                ct);

            settings.LastWebsiteDownloadUtc = DateTimeOffset.UtcNow;
            await context.Settings.SaveAsync(settings);

            await context.Out.WriteLineAsync($"Saved {result.Characters.Count} character(s) to {outputPath}");
            await context.Out.WriteLineAsync(result.CsvSummary);
            return result.CsvFailed > 0 ? ExitCodes.CompletedWithCsvFailures : ExitCodes.Ok;
        }
        catch (OperationCanceledException)
        {
            await context.Error.WriteLineAsync("Cancelled.");
            return ExitCodes.Cancelled;
        }
        catch (Exception ex)
        {
            await context.Error.WriteLineAsync($"Error: {ex.Message}");
            return ExitCodes.Error;
        }
    }

    /// <summary>The desktop app's data location: <c>OutputFolder</c> / <c>OutputFileName</c> from settings.json.</summary>
    private static string? DefaultOutputPath(UserSettings settings)
    {
        var folder = settings.OutputFolder?.Trim();
        var fileName = settings.OutputFileName?.Trim();
        return string.IsNullOrEmpty(folder) || string.IsNullOrEmpty(fileName) ? null : Path.Combine(folder, fileName);
    }

    /// <summary>Writes each progress message once, as it happens.</summary>
    private sealed class LineProgress(TextWriter writer) : IProgress<CharacterScrapeReport>
    {
        private string? _last;

        public void Report(CharacterScrapeReport value)
        {
            if (string.IsNullOrWhiteSpace(value.Detail) || value.Detail == _last)
                return;
            _last = value.Detail;
            writer.WriteLine("  " + value.Detail);
        }
    }
}
