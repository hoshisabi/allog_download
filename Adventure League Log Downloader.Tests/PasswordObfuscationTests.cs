using System;
using Adventure_League_Log_Downloader.Services;

namespace Adventure_League_Log_Downloader.Tests;

public class PasswordObfuscationTests
{
    [Theory]
    [InlineData("")]
    [InlineData("secret")]
    [InlineData("pässwörd ☃ with spaces")]
    [InlineData("a password longer than the obfuscation key, so the key repeats at least once")]
    public void Decode_ReversesEncode(string password)
    {
        Assert.Equal(password, PasswordObfuscation.Decode(PasswordObfuscation.Encode(password)));
    }

    [Fact]
    public void Encode_IsNotPlainBase64OfThePassword()
    {
        var encoded = PasswordObfuscation.Encode("secret");

        Assert.StartsWith("v1:", encoded);
        Assert.NotEqual("v1:" + Convert.ToBase64String("secret"u8.ToArray()), encoded);
    }

    [Theory]
    [InlineData("c2VjcmV0")]
    [InlineData("v1:%%%")]
    public void Decode_UnrecognizedValue_ThrowsFormatException(string value)
    {
        Assert.Throws<FormatException>(() => PasswordObfuscation.Decode(value));
    }
}
