using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Adventure_League_Log_Downloader.Services;

/// <summary>Explorer, the shell's default handlers, and the shell's known Downloads folder.</summary>
public sealed class WindowsPlatformShell : IPlatformShell
{
    private static readonly Guid FolderIdDownloads = new("374DE290-123F-4565-9164-39C4925E467B");

    /// <summary>
    /// .NET has no <see cref="Environment.SpecialFolder"/> for Downloads, so this asks the shell (which honors a relocated
    /// Downloads folder) and falls back to <c>%UserProfile%\Downloads</c>.
    /// </summary>
    public string GetDownloadsFolder()
    {
        try
        {
            if (SHGetKnownFolderPath(FolderIdDownloads, 0, IntPtr.Zero, out var pathPtr) == 0)
            {
                try
                {
                    var path = Marshal.PtrToStringUni(pathPtr);
                    if (!string.IsNullOrWhiteSpace(path))
                        return path;
                }
                finally
                {
                    Marshal.FreeCoTaskMem(pathPtr);
                }
            }
        }
        catch (Exception)
        {
            // Fall through to the conventional location.
        }

        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
    }

    public void RevealFile(string path) => Process.Start(BuildRevealFileStartInfo(path));

    public void OpenFolder(string path) => Process.Start(BuildShellOpenStartInfo(path));

    public void OpenUrl(string url) => Process.Start(BuildShellOpenStartInfo(url));

    /// <summary>Explorer window with <paramref name="path"/> selected.</summary>
    public static ProcessStartInfo BuildRevealFileStartInfo(string path) => new()
    {
        FileName = "explorer.exe",
        Arguments = $"/select,\"{path}\"",
        UseShellExecute = true
    };

    /// <summary>The shell's default handler for <paramref name="target"/>: Explorer for a folder, the browser for a URL.</summary>
    public static ProcessStartInfo BuildShellOpenStartInfo(string target) => new(target) { UseShellExecute = true };

    [DllImport("shell32.dll")]
    private static extern int SHGetKnownFolderPath(
        [MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint dwFlags, IntPtr hToken, out IntPtr ppszPath);
}
