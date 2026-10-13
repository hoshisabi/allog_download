using System;
using Adventure_League_Log_Downloader.Services;

namespace Adventure_League_Log_Downloader.Tests;

public class PlatformShellTests
{
    public static bool IsWindows => OperatingSystem.IsWindows();

    public static bool IsNotWindows => !OperatingSystem.IsWindows();

    [Fact(Skip = "Windows only.", SkipUnless = nameof(IsWindows))]
    public void ForCurrentOS_OnWindows_ReturnsWindowsShell()
    {
        Assert.IsType<WindowsPlatformShell>(PlatformShell.ForCurrentOS());
    }

    [Fact(Skip = "Off Windows only.", SkipUnless = nameof(IsNotWindows))]
    public void ForCurrentOS_OffWindows_IsNotSupportedYet()
    {
        Assert.Throws<PlatformNotSupportedException>(() => PlatformShell.ForCurrentOS());
    }
}
