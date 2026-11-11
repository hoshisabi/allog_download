using System.CommandLine;

namespace Allog.Cli;

/// <summary><c>allog login</c> and <c>allog logout</c>: save or remove the site login in the OS credential store.</summary>
internal static class LoginCommands
{
    public static Command BuildLogin(Func<CliContext> context)
    {
        var login = new LoginOptions();
        var command = new Command(
            "login",
            "Check a site login and save it, so later commands don't need one. Prompts for anything not given by an option, "
            + "--env-file, or environment variable.");
        login.AddTo(command);
        command.SetAction((parse, ct) => LoginAsync(login.Read(parse), context(), ct));
        return command;
    }

    public static Command BuildLogout(Func<CliContext> context)
    {
        var command = new Command("logout", "Remove the saved site login.");
        command.SetAction((_, _) => Task.FromResult(Logout(context())));
        return command;
    }

    internal static async Task<int> LoginAsync(LoginOptions.Values options, CliContext context, CancellationToken ct)
    {
        if (!options.TryGetEnvironment(context.Environment, context.Error, out var environment, out var problem))
        {
            await context.Error.WriteLineAsync(problem);
            return ExitCodes.Usage;
        }

        if (!CliCredentials.TryGetPassword(options.Password, options.PasswordFile, environment, out var password, out _, out problem))
        {
            await context.Error.WriteLineAsync(problem);
            return ExitCodes.Usage;
        }

        var username = CliCredentials.FirstSet(options.Username, environment(CliCredentials.UsernameVariable))
                       ?? context.Prompt("Username: ", false);
        if (!string.IsNullOrWhiteSpace(username) && string.IsNullOrEmpty(password))
            password = context.Prompt("Password: ", true);
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
        {
            await context.Error.WriteLineAsync("A username and password are needed to log in.");
            return ExitCodes.Usage;
        }

        ct.ThrowIfCancellationRequested();
        await context.Error.WriteLineAsync($"Checking the login for {username}…");
        try
        {
            await context.SignIn(username, password).GetUserIdAsync();
        }
        catch (OperationCanceledException)
        {
            await context.Error.WriteLineAsync("Cancelled.");
            return ExitCodes.Cancelled;
        }
        catch (Exception ex)
        {
            await context.Error.WriteLineAsync($"Error: {ex.Message} Nothing was saved.");
            return ExitCodes.Error;
        }

        context.CredentialStore.Save(username, password);
        await context.Out.WriteLineAsync($"Saved the login for {username} in {context.CredentialStore.Description}.");
        return ExitCodes.Ok;
    }

    internal static int Logout(CliContext context)
    {
        if (context.CredentialStore.Load() is null)
        {
            context.Out.WriteLine("No saved login.");
            return ExitCodes.Ok;
        }

        context.CredentialStore.Delete();
        context.Out.WriteLine($"Removed the saved login from {context.CredentialStore.Description}.");
        return ExitCodes.Ok;
    }

}
