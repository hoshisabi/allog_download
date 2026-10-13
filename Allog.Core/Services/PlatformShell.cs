using System;

namespace Adventure_League_Log_Downloader.Services;

public static class PlatformShell
{
    /// <summary>The <see cref="IPlatformShell"/> for the OS this process is running on.</summary>
    /// <exception cref="PlatformNotSupportedException">No implementation for this OS yet (Linux / macOS: TASKS.md A6).</exception>
    public static IPlatformShell ForCurrentOS()
    {
        if (OperatingSystem.IsWindows())
            return new WindowsPlatformShell();

        throw new PlatformNotSupportedException("Opening files and links isn't supported on this OS yet.");
    }
}
