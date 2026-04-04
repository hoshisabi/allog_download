using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Adventure_League_Log_Downloader.Services;

/// <summary>
/// Downloads per-character CSV exports from AdventurersLeagueLog.com (same URLs as the Python <c>csv_download</c> script).
/// </summary>
public sealed class CharacterCsvDownloader
{
    private readonly IAdventurersLeagueAuth _auth;

    public CharacterCsvDownloader(IAdventurersLeagueAuth auth)
    {
        _auth = auth;
    }

    /// <summary>
    /// Writes <c>character_{id}.csv</c> for each id into <paramref name="csvDirectory"/> using the configured network delay between requests.
    /// </summary>
    /// <returns>Number of characters for which the HTTP request did not succeed.</returns>
    public async Task<int> DownloadAllAsync(
        IReadOnlyList<string> characterIdsOrdered,
        string csvDirectory,
        double delaySeconds,
        IProgress<CharacterScrapeReport>? progress,
        IReadOnlyList<CharacterRecord> charactersForUiSnapshot,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(characterIdsOrdered);

        var client = await _auth.GetAuthenticatedClientAsync();
        var userId = await _auth.GetUserIdAsync();

        Directory.CreateDirectory(csvDirectory);

        var total = characterIdsOrdered.Count;
        if (total == 0)
            return 0;

        var failed = 0;

        for (var i = 0; i < characterIdsOrdered.Count; i++)
        {
            ct.ThrowIfCancellationRequested();

            var id = characterIdsOrdered[i];
            var path = Path.Combine(csvDirectory, $"character_{id}.csv");
            var relativeUrl = $"/users/{userId}/characters/{Uri.EscapeDataString(id)}.csv";

            progress?.Report(new CharacterScrapeReport
            {
                Phase = CharacterScrapePhase.DownloadingCsvs,
                CharacterCount = charactersForUiSnapshot.Count,
                Characters = charactersForUiSnapshot,
                Detail = $"Downloading character CSVs: {i + 1} of {total}…"
            });

            using var req = new HttpRequestMessage(HttpMethod.Get, relativeUrl);
            req.Headers.Accept.Clear();
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/csv"));
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));

            using var resp = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!resp.IsSuccessStatusCode)
            {
                failed++;
                progress?.Report(new CharacterScrapeReport
                {
                    Phase = CharacterScrapePhase.DownloadingCsvs,
                    CharacterCount = charactersForUiSnapshot.Count,
                    Characters = charactersForUiSnapshot,
                    Detail = $"CSV for character {id}: HTTP {(int)resp.StatusCode} (skipped)."
                });
            }
            else
            {
                await using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
                await resp.Content.CopyToAsync(fs, ct);
            }

            if (delaySeconds > 0 && i < characterIdsOrdered.Count - 1)
            {
                var delayMs = (int)Math.Round(delaySeconds * 1000);
                await Task.Delay(delayMs, ct);
            }
        }

        return failed;
    }

    /// <summary>
    /// Default folder for CSV files: a <c>csv</c> directory next to the characters JSON file.
    /// </summary>
    public static string GetDefaultCsvDirectory(string charactersJsonPath)
    {
        var dir = Path.GetDirectoryName(charactersJsonPath);
        if (string.IsNullOrWhiteSpace(dir))
            dir = Directory.GetCurrentDirectory();
        return Path.Combine(dir, "csv");
    }
}
