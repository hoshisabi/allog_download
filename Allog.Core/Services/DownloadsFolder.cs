using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Adventure_League_Log_Downloader.Services;

/// <summary>
/// The user's Downloads folder. .NET has no <see cref="Environment.SpecialFolder"/> for it, so this asks the shell
/// (which honors a relocated Downloads folder) and falls back to <c>%UserProfile%\Downloads</c>.
/// </summary>
public static class DownloadsFolder
{
    private static readonly Guid FolderIdDownloads = new("374DE290-123F-4565-9164-39C4925E467B");

    public static string GetPath()
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

    [DllImport("shell32.dll")]
    private static extern int SHGetKnownFolderPath(
        [MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint dwFlags, IntPtr hToken, out IntPtr ppszPath);
}
