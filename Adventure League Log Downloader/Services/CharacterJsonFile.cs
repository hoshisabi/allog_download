using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Adventure_League_Log_Downloader.Services;

/// <summary>
/// Reads character list JSON written by <see cref="CharacterScraper.SaveJsonAsync"/> (dictionary keyed by id).
/// </summary>
public static class CharacterJsonFile
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
    /// Loads characters from disk, sorted by name. Returns null if the file is missing, empty, or not valid JSON for this shape.
    /// </summary>
    public static async Task<List<CharacterRecord>?> TryLoadSortedAsync(string path, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return null;

            await using var fs = File.OpenRead(path);
            var dict = await JsonSerializer.DeserializeAsync<Dictionary<string, CharacterRecord>>(fs, ReadOptions, ct);
            if (dict == null || dict.Count == 0)
                return null;

            return dict.Values
                .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Loads the raw id → character map, or null if missing/invalid.
    /// </summary>
    public static async Task<Dictionary<string, CharacterRecord>?> TryLoadDictionaryAsync(string path, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return null;

            await using var fs = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<Dictionary<string, CharacterRecord>>(fs, ReadOptions, ct);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Writes the character dictionary to JSON (same shape as <see cref="CharacterScraper.SaveJsonAsync"/>).
    /// </summary>
    public static async Task SaveAsync(
        string path,
        IReadOnlyDictionary<string, CharacterRecord> characters,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(characters);

        await using var fs = File.Create(path);
        await JsonSerializer.SerializeAsync(fs, characters, WriteOptions, ct);
    }
}
