namespace Allog.Cli;

/// <summary>
/// Reads <c>KEY=VALUE</c> lines as written for shells and dotenv tools: blank lines and <c>#</c> comments skipped, an
/// optional <c>export </c> prefix, and values optionally wrapped in matching single or double quotes.
/// </summary>
internal static class EnvFile
{
    public static Dictionary<string, string> Parse(string contents)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var rawLine in contents.Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
                continue;
            if (line.StartsWith("export ", StringComparison.Ordinal))
                line = line["export ".Length..].TrimStart();

            var equals = line.IndexOf('=');
            if (equals <= 0)
                continue;

            var key = line[..equals].Trim();
            var value = line[(equals + 1)..].Trim();
            if (value.Length >= 2 && (value[0] == '"' || value[0] == '\'') && value[^1] == value[0])
                value = value[1..^1];
            values[key] = value;
        }

        return values;
    }
}
