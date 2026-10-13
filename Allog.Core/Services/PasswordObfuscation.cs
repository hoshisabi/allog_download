using System;
using System.Text;

namespace Adventure_League_Log_Downloader.Services;

/// <summary>
/// Obfuscation, not encryption (see SECURITY.md): keeps a stored password from being readable at a glance or decoded
/// by pasting it into a Base64 decoder. Anyone with the file and this code can recover it.
/// </summary>
public static class PasswordObfuscation
{
    private const string Prefix = "v1:";
    private static readonly byte[] Key = "AllogDownloader-obfuscation-v1"u8.ToArray();

    public static string Encode(string password)
    {
        var bytes = Encoding.UTF8.GetBytes(password);
        Xor(bytes);
        return Prefix + Convert.ToBase64String(bytes);
    }

    /// <exception cref="FormatException">Not a value produced by <see cref="Encode"/>.</exception>
    public static string Decode(string obfuscated)
    {
        if (!obfuscated.StartsWith(Prefix, StringComparison.Ordinal))
            throw new FormatException("Unrecognized obfuscated password format.");

        var bytes = Convert.FromBase64String(obfuscated[Prefix.Length..]);
        Xor(bytes);
        return Encoding.UTF8.GetString(bytes);
    }

    private static void Xor(byte[] bytes)
    {
        for (var i = 0; i < bytes.Length; i++)
            bytes[i] ^= Key[i % Key.Length];
    }
}
