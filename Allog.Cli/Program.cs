using System.CommandLine;
using Allog.Cli;

var root = new RootCommand($"""
    Adventurers League Log Downloader, command-line version. Uses the same settings and saved login as the desktop app.

    Exit codes: {ExitCodes.Ok} ok, {ExitCodes.Error} error, {ExitCodes.Usage} missing login or bad arguments, {ExitCodes.CompletedWithCsvFailures} finished but some character CSVs failed, {ExitCodes.Cancelled} cancelled.
    """);
root.Subcommands.Add(CharactersCommand.Build(CliContext.ForThisMachine));
root.Subcommands.Add(LoginCommands.BuildLogin(CliContext.ForThisMachine));
root.Subcommands.Add(LoginCommands.BuildLogout(CliContext.ForThisMachine));

var parse = root.Parse(args);
var exitCode = await parse.InvokeAsync();

// System.CommandLine prints parse errors and returns 1; report them as usage errors like the rest of the CLI.
return parse.Errors.Count > 0 ? ExitCodes.Usage : exitCode;
