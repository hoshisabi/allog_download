namespace Adventure_League_Log_Downloader.Services;

/// <summary>
/// Hands things off to the desktop: the file manager, the default browser, the user's Downloads folder.
/// One implementation per OS; get the current one from <see cref="PlatformShell.ForCurrentOS"/>.
/// </summary>
public interface IPlatformShell
{
    /// <summary>The user's Downloads folder (honoring a relocated or localized folder where the OS records one).</summary>
    string GetDownloadsFolder();

    /// <summary>Opens the file manager on the folder containing <paramref name="path"/>, with the file selected.</summary>
    void RevealFile(string path);

    /// <summary>Opens <paramref name="path"/> in the file manager.</summary>
    void OpenFolder(string path);

    /// <summary>Opens <paramref name="url"/> in the default browser.</summary>
    void OpenUrl(string url);
}
