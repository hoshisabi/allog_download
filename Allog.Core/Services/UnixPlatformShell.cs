using System;
using System.Diagnostics;
using System.IO;

namespace Adventure_League_Log_Downloader.Services;

/// <summary>
/// Linux and macOS: <c>xdg-open</c> / <c>open</c> for folders and URLs; the freedesktop file manager D-Bus interface or
/// <c>open -R</c> to reveal a file; the XDG user-dirs config (Linux) or <c>~/Downloads</c> (macOS) for Downloads.
/// </summary>
public sealed class UnixPlatformShell : IPlatformShell
{
    /// <summary>How long to wait for the file manager to answer before opening the folder instead.</summary>
    internal const int RevealTimeoutMilliseconds = 3000;

    private readonly bool _isMacOS;
    private readonly string _home;
    private readonly string? _xdgConfigHome;
    private readonly Func<ProcessStartInfo, int> _runAndWait;
    private readonly Action<ProcessStartInfo> _start;

    public UnixPlatformShell()
        : this(
            OperatingSystem.IsMacOS(),
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            Environment.GetEnvironmentVariable("XDG_CONFIG_HOME"),
            RunAndWait,
            info => Process.Start(info)?.Dispose())
    {
    }

    internal UnixPlatformShell(
        bool isMacOS, string home, string? xdgConfigHome, Func<ProcessStartInfo, int> runAndWait, Action<ProcessStartInfo> start)
    {
        _isMacOS = isMacOS;
        _home = home;
        _xdgConfigHome = xdgConfigHome;
        _runAndWait = runAndWait;
        _start = start;
    }

    public string GetDownloadsFolder()
    {
        var fallback = Path.Combine(_home, "Downloads");
        if (_isMacOS)
            return fallback;

        var configHome = string.IsNullOrWhiteSpace(_xdgConfigHome) ? Path.Combine(_home, ".config") : _xdgConfigHome;
        var userDirsPath = Path.Combine(configHome, "user-dirs.dirs");
        try
        {
            if (File.Exists(userDirsPath))
                return ParseXdgDownloadDir(File.ReadAllText(userDirsPath), _home) ?? fallback;
        }
        catch (IOException)
        {
            // Fall through to the conventional location.
        }

        return fallback;
    }

    /// <summary>
    /// Selects the file in the file manager. On Linux, if no file manager answers the freedesktop <c>ShowItems</c> call,
    /// opens the containing folder instead.
    /// </summary>
    public void RevealFile(string path)
    {
        if (_isMacOS)
        {
            _start(BuildMacRevealStartInfo(path));
            return;
        }

        if (_runAndWait(BuildFreedesktopShowItemsStartInfo(path)) != 0)
            OpenFolder(Path.GetDirectoryName(Path.GetFullPath(path)) ?? path);
    }

    public void OpenFolder(string path) => _start(BuildOpenStartInfo(_isMacOS, path));

    public void OpenUrl(string url) => _start(BuildOpenStartInfo(_isMacOS, url));

    /// <summary><c>open</c> (macOS) or <c>xdg-open</c> (Linux) on <paramref name="target"/>.</summary>
    public static ProcessStartInfo BuildOpenStartInfo(bool isMacOS, string target)
    {
        var info = new ProcessStartInfo(isMacOS ? "open" : "xdg-open") { UseShellExecute = false };
        info.ArgumentList.Add(target);
        return info;
    }

    /// <summary><c>open -R</c>: Finder window with <paramref name="path"/> selected.</summary>
    public static ProcessStartInfo BuildMacRevealStartInfo(string path)
    {
        var info = new ProcessStartInfo("open") { UseShellExecute = false };
        info.ArgumentList.Add("-R");
        info.ArgumentList.Add(path);
        return info;
    }

    /// <summary>
    /// <c>org.freedesktop.FileManager1.ShowItems</c> over the session bus (Nautilus, Dolphin, Nemo, Caja, Thunar). D-Bus
    /// starts the file manager if it isn't running. Exits non-zero when nothing answers within the timeout.
    /// </summary>
    public static ProcessStartInfo BuildFreedesktopShowItemsStartInfo(string path)
    {
        var info = new ProcessStartInfo("dbus-send") { UseShellExecute = false };
        foreach (var arg in new[]
                 {
                     "--session", "--print-reply", $"--reply-timeout={RevealTimeoutMilliseconds}",
                     "--dest=org.freedesktop.FileManager1", "--type=method_call", "/org/freedesktop/FileManager1",
                     "org.freedesktop.FileManager1.ShowItems", "array:string:" + ToFileUri(path), "string:"
                 })
            info.ArgumentList.Add(arg);
        info.RedirectStandardOutput = true;
        info.RedirectStandardError = true;
        return info;
    }

    /// <summary>
    /// <c>XDG_DOWNLOAD_DIR</c> from the contents of <c>user-dirs.dirs</c>, or null when it isn't set. Values are
    /// <c>"$HOME/relative"</c> or an absolute path, as the xdg-user-dirs format allows.
    /// </summary>
    public static string? ParseXdgDownloadDir(string userDirsContents, string home)
    {
        foreach (var rawLine in userDirsContents.Split('\n'))
        {
            var line = rawLine.Trim();
            if (!line.StartsWith("XDG_DOWNLOAD_DIR=", StringComparison.Ordinal))
                continue;

            var value = line["XDG_DOWNLOAD_DIR=".Length..].Trim().Trim('"');
            if (value == "$HOME" || value == "$HOME/")
                return home;
            if (value.StartsWith("$HOME/", StringComparison.Ordinal))
                return Path.Combine(home, value["$HOME/".Length..]);
            if (value.StartsWith('/'))
                return value;
            return null;
        }

        return null;
    }

    /// <summary>
    /// <c>file://</c> URI for <paramref name="path"/>, with commas escaped because <c>dbus-send</c> splits array items on them.
    /// </summary>
    internal static string ToFileUri(string path) =>
        new Uri(Path.GetFullPath(path)).AbsoluteUri.Replace(",", "%2C", StringComparison.Ordinal);

    /// <summary>Exit code, or -1 if the program isn't installed, didn't start, or didn't finish in time.</summary>
    private static int RunAndWait(ProcessStartInfo info)
    {
        Process? started;
        try
        {
            started = Process.Start(info);
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return -1;
        }

        using var process = started;
        if (process is null)
            return -1;

        // dbus-send gives up after --reply-timeout; the extra second covers process start-up.
        if (!process.WaitForExit(RevealTimeoutMilliseconds + 1000))
        {
            process.Kill();
            return -1;
        }

        return process.ExitCode;
    }
}
