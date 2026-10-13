using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Adventure_League_Log_Downloader.Services;

/// <summary>
/// Reads and writes the DM sessions JSON file (dictionary keyed by session id).
/// </summary>
public static class DmSessionJsonFile
{
    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>
    /// Loads the raw id → session map, or null if the file is missing or invalid.
    /// </summary>
    public static async Task<Dictionary<string, DmSessionRecord>?> TryLoadAsync(string path, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return null;

            await using var fs = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<Dictionary<string, DmSessionRecord>>(fs, ReadOptions, ct);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Writes the session dictionary to JSON.
    /// </summary>
    public static async Task SaveAsync(
        string path,
        IReadOnlyDictionary<string, DmSessionRecord> sessions,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(sessions);

        await using var fs = File.Create(path);
        await JsonSerializer.SerializeAsync(fs, sessions, WriteOptions, ct);
    }
}
