using System.IO;

namespace Adventure_League_Log_Downloader.Services;

/// <summary>
/// Browser-style download naming: <c>name.ext</c>, then <c>name (1).ext</c>, <c>name (2).ext</c>, … for the first unused name.
/// </summary>
public static class UniqueFilePath
{
    public static string Next(string folder, string baseName, string extension)
    {
        var path = Path.Combine(folder, baseName + extension);
        for (var n = 1; File.Exists(path); n++)
            path = Path.Combine(folder, $"{baseName} ({n}){extension}");
        return path;
    }
}
