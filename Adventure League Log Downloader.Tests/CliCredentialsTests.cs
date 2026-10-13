using System;
using System.Collections.Generic;
using System.IO;
using Allog.Cli;

namespace Adventure_League_Log_Downloader.Tests;

public class CliCredentialsTests : IDisposable
{
    private readonly Dictionary<string, string> _environment = [];
    private readonly MemoryCredentialStore _store = new();
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), $"allog_cli_cred_test_{Guid.NewGuid():N}");

    public CliCredentialsTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private CliCredentials.Found? Resolve(string? username = null, string? password = null, FileInfo? passwordFile = null) =>
        CliCredentials.Resolve(username, password, passwordFile, name => _environment.GetValueOrDefault(name), _store, out _);

    private string Problem(string? username = null, string? password = null, FileInfo? passwordFile = null)
    {
        Assert.Null(CliCredentials.Resolve(username, password, passwordFile, name => _environment.GetValueOrDefault(name), _store, out var problem));
        return problem;
    }

    [Fact]
    public void PasswordOption_WinsOverEnvironmentAndSavedLogin()
    {
        _environment["ALLOG_PASSWORD"] = "from-env";
        _store.Saved = ("someone", "from-store");

        Assert.Equal(new CliCredentials.Found("someone", "from-flag", "--password"), Resolve("someone", "from-flag"));
    }

    [Fact]
    public void PasswordFile_UsesFirstLineWithoutLineEnding()
    {
        var file = new FileInfo(Path.Combine(_tempDir, "pw"));
        File.WriteAllText(file.FullName, "from-file\nsecond line\n");
        _environment["ALLOG_PASSWORD"] = "from-env";

        Assert.Equal(new CliCredentials.Found("someone", "from-file", "--password-file"), Resolve("someone", passwordFile: file));
    }

    [Fact]
    public void PasswordFile_Missing_IsAProblem()
    {
        var problem = Problem("someone", passwordFile: new FileInfo(Path.Combine(_tempDir, "nope")));

        Assert.StartsWith("Password file not found:", problem);
    }

    [Fact]
    public void EnvironmentVariables_UsedWhenNoOptions()
    {
        _environment["ALLOG_USERNAME"] = "env-user";
        _environment["ALLOG_PASSWORD"] = "env-pass";

        Assert.Equal(new CliCredentials.Found("env-user", "env-pass", "ALLOG_PASSWORD"), Resolve());
    }

    [Fact]
    public void UsernameOption_WinsOverEnvironment()
    {
        _environment["ALLOG_USERNAME"] = "env-user";
        _environment["ALLOG_PASSWORD"] = "env-pass";

        Assert.Equal("flag-user", Resolve(username: "flag-user")!.Username);
    }

    [Fact]
    public void NoPasswordGiven_UsesSavedLogin()
    {
        _store.Saved = ("saved-user", "saved-pass");

        var found = Resolve();

        Assert.Equal(("saved-user", "saved-pass"), (found!.Username, found.Password));
        Assert.Equal("saved login (test store)", found.Source);
    }

    [Fact]
    public void NoPasswordGiven_SameUsernameAsSaved_UsesSavedLogin()
    {
        _store.Saved = ("saved-user", "saved-pass");

        Assert.Equal("saved-pass", Resolve(username: "saved-user")!.Password);
    }

    [Fact]
    public void NoPasswordGiven_DifferentUsernameFromSaved_IsAProblem()
    {
        _store.Saved = ("saved-user", "saved-pass");

        Assert.StartsWith("No login found.", Problem(username: "someone-else"));
    }

    [Fact]
    public void PasswordWithoutUsername_IsAProblem()
    {
        Assert.StartsWith("No login found.", Problem(password: "pw"));
    }

    [Fact]
    public void Nothing_ProblemListsEveryOption()
    {
        var problem = Problem();

        Assert.Contains("--password-file", problem);
        Assert.Contains("ALLOG_USERNAME", problem);
        Assert.Contains("ALLOG_PASSWORD", problem);
        Assert.Contains("test store", problem);
    }
}
