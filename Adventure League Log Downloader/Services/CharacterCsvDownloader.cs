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
    /// Writes <c>character_{id}.csv</c> for each id into <paramref name="characterDataDirectory"/> (same folder as the characters JSON).
    /// </summary>
    /// <returns>Number of characters for which the HTTP request did not succeed.</returns>
    public async Task<int> DownloadAllAsync(
        IReadOnlyList<string> characterIdsOrdered,
        string characterDataDirectory,
        double delaySeconds,
        IProgress<CharacterScrapeReport>? progress,
        IReadOnlyList<CharacterRecord> charactersForUiSnapshot,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(characterIdsOrdered);

        var client = await _auth.GetAuthenticatedClientAsync();
        var userId = await _auth.GetUserIdAsync();

        Directory.CreateDirectory(characterDataDirectory);

        var total = characterIdsOrdered.Count;
        if (total == 0)
            return 0;

        var failed = 0;

        for (var i = 0; i < characterIdsOrdered.Count; i++)
        {
            ct.ThrowIfCancellationRequested();

            var id = characterIdsOrdered[i];
            var path = Path.Combine(characterDataDirectory, $"character_{id}.csv");
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
    /// Folder that holds the characters JSON and per-character CSVs — the user’s chosen data directory.
    /// Same layout as Python <c>download_all_csv</c>: <c>character_{id}.csv</c> next to the JSON file (no extra subfolder).
    /// </summary>
    public static string GetCharacterDataDirectory(string charactersJsonPath)
    {
        try
        {
            var dir = Path.GetDirectoryName(Path.GetFullPath(charactersJsonPath));
            if (!string.IsNullOrWhiteSpace(dir))
                return dir;
        }
        catch
        {
            // fall through
        }

        return Directory.GetCurrentDirectory();
    }
}
