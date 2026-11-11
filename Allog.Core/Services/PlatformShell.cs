using System;

namespace Adventure_League_Log_Downloader.Services;

public static class PlatformShell
{
    /// <summary>The <see cref="IPlatformShell"/> for the OS this process is running on.</summary>
    /// <exception cref="PlatformNotSupportedException">Not Windows, Linux, or macOS.</exception>
    public static IPlatformShell ForCurrentOS()
    {
        if (OperatingSystem.IsWindows())
            return new WindowsPlatformShell();
        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            return new UnixPlatformShell();

        throw new PlatformNotSupportedException("Opening files and links isn't supported on this OS.");
    }
}
