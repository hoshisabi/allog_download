using System;
using Adventure_League_Log_Downloader.Services;

namespace Adventure_League_Log_Downloader.Tests;

public class PlatformShellTests
{
    public static bool IsWindows => OperatingSystem.IsWindows();

    public static bool IsLinuxOrMacOS => OperatingSystem.IsLinux() || OperatingSystem.IsMacOS();

    [Fact(Skip = "Windows only.", SkipUnless = nameof(IsWindows))]
    public void ForCurrentOS_OnWindows_ReturnsWindowsShell()
    {
        Assert.IsType<WindowsPlatformShell>(PlatformShell.ForCurrentOS());
    }

    [Fact(Skip = "Linux and macOS only.", SkipUnless = nameof(IsLinuxOrMacOS))]
    public void ForCurrentOS_OnLinuxOrMacOS_ReturnsUnixShell()
    {
        Assert.IsType<UnixPlatformShell>(PlatformShell.ForCurrentOS());
    }
}
