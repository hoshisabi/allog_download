using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Adventure_League_Log_Downloader.Services;

/// <summary>Which character CSVs a <see cref="CharacterListDownload"/> fetches after the list.</summary>
public enum CharacterCsvMode
{
    /// <summary>List only; no CSV downloads.</summary>
    Skip,

    /// <summary>Only characters with no <c>character_{id}.csv</c> next to the list yet.</summary>
    OnlyMissing,

    /// <summary>Every character, overwriting existing CSVs.</summary>
    All
}

/// <param name="CharactersJsonPath">Where to save the character list; CSVs go in the same folder.</param>
/// <param name="DelaySeconds">Pause between site requests.</param>
public sealed record CharacterListDownloadOptions(string CharactersJsonPath, double DelaySeconds, CharacterCsvMode CsvMode);

public sealed record CharacterListDownloadResult(
    IReadOnlyList<CharacterRecord> Characters,
    string CharactersJsonPath,
    string DataDirectory,
    CharacterCsvMode CsvMode,
    int CsvRequested,
    int CsvFailed)
{
    public int CsvSucceeded => CsvRequested - CsvFailed;

    /// <summary>One sentence on what happened with the CSVs, for status text and summaries.</summary>
    public string CsvSummary => CsvMode == CharacterCsvMode.Skip
        ? "CSV downloads skipped (list only)."
        : CsvRequested == 0
            ? CsvMode == CharacterCsvMode.OnlyMissing
                ? "All character CSV files were already present; no CSV downloads."
                : "No characters to download CSVs for."
            : CsvFailed == 0
                ? $"Downloaded {CsvSucceeded} character CSV file(s) next to your character list."
                : $"Downloaded {CsvSucceeded} CSV file(s); {CsvFailed} failed (see status text). CSVs are next to your character list.";
}

/// <summary>
/// Signs in, scrapes the character list, downloads character CSVs per <see cref="CharacterListDownloadOptions.CsvMode"/>,
/// fills in each character's last session date (from its CSV, else from the previously saved list), and saves the list.
/// </summary>
public static class CharacterListDownload
{
    public static async Task<CharacterListDownloadResult> RunAsync(
        IAdventurersLeagueAuth auth,
        CharacterListDownloadOptions options,
        IProgress<CharacterScrapeReport>? progress = null,
        CancellationToken ct = default)
    {
        var outputPath = options.CharactersJsonPath;
        var scraper = new CharacterScraper(auth);

        var results = await scraper.ScrapeAsync(options.DelaySeconds, progress, ct);

        var sorted = results.Values.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase).ToList();
        var dataDir = CharacterCsvDownloader.GetCharacterDataDirectory(outputPath);
        var idsForCsv = options.CsvMode switch
        {
            CharacterCsvMode.Skip => [],
            CharacterCsvMode.OnlyMissing => sorted.Where(c => !CharacterCsvLocator.LocalCsvExists(outputPath, c.Id)).Select(c => c.Id).ToList(),
            _ => sorted.Select(c => c.Id).ToList()
        };

        var csvFailed = 0;
        if (idsForCsv.Count > 0)
        {
            var csvDownloader = new CharacterCsvDownloader(auth);
            csvFailed = await csvDownloader.DownloadAllAsync(idsForCsv, dataDir, options.DelaySeconds, progress, sorted, ct);
        }

        var previousById = await CharacterJsonFile.TryLoadDictionaryAsync(outputPath, ct);
        foreach (var c in sorted)
        {
            CharacterLogCsvReader.ApplyLatestSessionFromCsvIfPresent(c, outputPath);
            if (!string.IsNullOrWhiteSpace(c.LastSessionPlayed))
                continue;
            if (previousById != null && previousById.TryGetValue(c.Id, out var prev) && !string.IsNullOrWhiteSpace(prev.LastSessionPlayed))
                c.LastSessionPlayed = prev.LastSessionPlayed;
        }

        CharacterCsvLocator.RefreshLocalCsvFlags(outputPath, sorted);

        progress?.Report(new CharacterScrapeReport
        {
            Phase = CharacterScrapePhase.Saving,
            CharacterCount = sorted.Count,
            Characters = sorted,
            Detail = "Saving character list…"
        });

        await CharacterJsonFile.SaveAsync(outputPath, sorted.ToDictionary(c => c.Id, c => c), ct);

        var result = new CharacterListDownloadResult(sorted, outputPath, dataDir, options.CsvMode, idsForCsv.Count, csvFailed);

        progress?.Report(new CharacterScrapeReport
        {
            Phase = CharacterScrapePhase.Complete,
            CharacterCount = sorted.Count,
            Characters = sorted,
            Detail = $"Saved {Path.GetFileName(outputPath)}. {result.CsvSummary}"
        });

        return result;
    }
}
