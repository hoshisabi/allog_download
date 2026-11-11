using System.CommandLine;
using Adventure_League_Log_Downloader.Services;

namespace Allog.Cli;

/// <summary><c>allog characters</c>: the desktop app's Download button, without selecting characters.</summary>
internal static class CharactersCommand
{
    internal sealed record Args(FileInfo? Output, string? Csv, double? Delay, LoginOptions.Values Login, bool Quiet);

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
        var login = new LoginOptions();
        var quiet = new Option<bool>("--quiet", "-q") { Description = "Only print the final summary and errors." };

        var command = new Command("characters", "Sign in, download your character list and character CSVs, and save the list as JSON.")
        {
            output, csv, delay
        };
        login.AddTo(command);
        command.Options.Add(quiet);
        command.SetAction((parse, ct) => RunAsync(
            new Args(parse.GetValue(output), parse.GetValue(csv), parse.GetValue(delay), login.Read(parse), parse.GetValue(quiet)),
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

        var login = args.Login.Resolve(context, useSavedLogin: true, out var problem);
        if (login is null)
        {
            await context.Error.WriteLineAsync(problem);
            return ExitCodes.Usage;
        }

        var progress = args.Quiet ? null : new StatusLines(context.Error, context.ErrorIsTerminal);
        if (!args.Quiet)
            await context.Error.WriteLineAsync($"Signing in as {login.Username}; login from {login.Source}…");

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            var result = await CharacterListDownload.RunAsync(
                context.SignIn(login.Username, login.Password),
                new CharacterListDownloadOptions(outputPath, delaySeconds, mode),
                progress,
                ct);

            progress?.Finish();
            settings.LastWebsiteDownloadUtc = DateTimeOffset.UtcNow;
            await context.Settings.SaveAsync(settings);

            await context.Out.WriteLineAsync($"Saved {result.Characters.Count} character(s) to {outputPath}");
            await context.Out.WriteLineAsync(result.CsvSummary);
            return result.CsvFailed > 0 ? ExitCodes.CompletedWithCsvFailures : ExitCodes.Ok;
        }
        catch (OperationCanceledException)
        {
            progress?.Finish();
            await context.Error.WriteLineAsync("Cancelled.");
            return ExitCodes.Cancelled;
        }
        catch (Exception ex)
        {
            progress?.Finish();
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
}
