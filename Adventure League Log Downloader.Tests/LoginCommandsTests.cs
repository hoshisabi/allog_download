using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Adventure_League_Log_Downloader.Services;
using Allog.Cli;

namespace Adventure_League_Log_Downloader.Tests;

public class LoginCommandsTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), $"allog_login_test_{Guid.NewGuid():N}");
    private readonly FakeSite _site = new();
    private readonly MemoryCredentialStore _store = new();
    private readonly Dictionary<string, string> _environment = [];
    private readonly Queue<string?> _answers = new();
    private readonly List<(string Label, bool Hidden)> _prompts = [];
    private readonly List<(string Username, string Password)> _signIns = [];
    private readonly StringWriter _out = new();
    private readonly StringWriter _error = new();
    private Exception? _signInFailure;

    public LoginCommandsTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private CliContext Context() => new(
        new MemorySettingsService(new UserSettings()),
        _store,
        (username, password) =>
        {
            _signIns.Add((username, password));
            if (_signInFailure is not null)
                throw _signInFailure;
            return _site.Auth();
        },
        name => _environment.GetValueOrDefault(name),
        _out,
        _error,
        (label, hidden) =>
        {
            _prompts.Add((label, hidden));
            return _answers.Count > 0 ? _answers.Dequeue() : null;
        },
        ErrorIsTerminal: false);

    private Task<int> LoginAsync(LoginOptions.Values options) =>
        LoginCommands.LoginAsync(options, Context(), TestContext.Current.CancellationToken);

    [Fact]
    public async Task Login_PromptsForBoth_HidesPassword_ChecksThenSaves()
    {
        _answers.Enqueue("someone");
        _answers.Enqueue("secret");

        var exit = await LoginAsync(LoginOptions.Values.None);

        Assert.Equal(0, exit);
        Assert.Equal([("Username: ", false), ("Password: ", true)], _prompts);
        Assert.Equal([("someone", "secret")], _signIns);
        Assert.Equal(("someone", "secret"), _store.Saved);
        Assert.Contains("Saved the login for someone in test store.", _out.ToString());
    }

    [Fact]
    public async Task Login_UsernameOption_PromptsOnlyForPassword()
    {
        _answers.Enqueue("secret");

        await LoginAsync(new LoginOptions.Values("someone", null, null, null));

        Assert.Equal([("Password: ", true)], _prompts);
        Assert.Equal(("someone", "secret"), _store.Saved);
    }

    [Fact]
    public async Task Login_FromEnvFile_DoesNotPrompt()
    {
        var envFile = Path.Combine(_tempDir, ".env");
        File.WriteAllText(envFile, "ALLOG_USERNAME=file-user\nALLOG_PASSWORD=file-pass\n");

        var exit = await LoginAsync(new LoginOptions.Values(null, null, null, new FileInfo(envFile)));

        Assert.Equal(0, exit);
        Assert.Empty(_prompts);
        Assert.Equal(("file-user", "file-pass"), _store.Saved);
    }

    [Fact]
    public async Task Login_IgnoresTheLoginAlreadySaved()
    {
        _store.Saved = ("old-user", "old-pass");
        _answers.Enqueue("new-user");
        _answers.Enqueue("new-pass");

        await LoginAsync(LoginOptions.Values.None);

        Assert.Equal([("new-user", "new-pass")], _signIns);
        Assert.Equal(("new-user", "new-pass"), _store.Saved);
    }

    [Fact]
    public async Task Login_SignInFails_SavesNothing()
    {
        _signInFailure = new UnauthorizedAccessException("Login appears to have failed. Please check your credentials.");
        _answers.Enqueue("someone");
        _answers.Enqueue("wrong");

        var exit = await LoginAsync(LoginOptions.Values.None);

        Assert.Equal(1, exit);
        Assert.Null(_store.Saved);
        Assert.Contains("Nothing was saved.", _error.ToString());
    }

    [Fact]
    public async Task Login_NoAnswers_ExitsTwo()
    {
        var exit = await LoginAsync(LoginOptions.Values.None);

        Assert.Equal(2, exit);
        Assert.Empty(_signIns);
        Assert.Null(_store.Saved);
    }

    [Fact]
    public async Task Login_MissingPasswordFile_ExitsTwoWithoutPrompting()
    {
        var exit = await LoginAsync(new LoginOptions.Values("someone", null, new FileInfo(Path.Combine(_tempDir, "nope")), null));

        Assert.Equal(2, exit);
        Assert.Empty(_prompts);
        Assert.StartsWith("Password file not found:", _error.ToString());
    }

    [Fact]
    public void Logout_RemovesSavedLogin()
    {
        _store.Saved = ("someone", "secret");

        Assert.Equal(0, LoginCommands.Logout(Context()));
        Assert.Null(_store.Saved);
        Assert.Contains("Removed the saved login from test store.", _out.ToString());
    }

    [Fact]
    public void Logout_NothingSaved_SaysSo()
    {
        Assert.Equal(0, LoginCommands.Logout(Context()));
        Assert.Contains("No saved login.", _out.ToString());
    }

    [Fact]
    public async Task Login_EnvFileWithOnlyPassword_PromptsOnlyForUsername()
    {
        var envFile = Path.Combine(_tempDir, ".env");
        File.WriteAllText(envFile, "ALLOG_PASSWORD=file-pass\n");
        _answers.Enqueue("typed-user");

        var exit = await LoginAsync(new LoginOptions.Values(null, null, null, new FileInfo(envFile)));

        Assert.Equal(0, exit);
        Assert.Equal([("Username: ", false)], _prompts);
        Assert.Equal(("typed-user", "file-pass"), _store.Saved);
    }

    [Fact]
    public async Task EnvFile_UnknownAllogKey_Warns()
    {
        var envFile = Path.Combine(_tempDir, ".env");
        File.WriteAllText(envFile, "ALLOG_PASSWORD=file-pass\nALLOG_USER=someone\nOTHER_TOOL=x\n");
        _answers.Enqueue("someone");

        await LoginAsync(new LoginOptions.Values(null, null, null, new FileInfo(envFile)));

        Assert.Contains(".env: ALLOG_USER isn't used; the keys are ALLOG_USERNAME and ALLOG_PASSWORD.", _error.ToString());
        Assert.DoesNotContain("OTHER_TOOL", _error.ToString());
    }
}
