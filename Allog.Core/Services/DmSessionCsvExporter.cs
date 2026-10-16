using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper;

namespace Adventure_League_Log_Downloader.Services;

/// <summary>
/// Writes DM sessions to a flat CSV (one row per session), the CSV counterpart
/// of <c>dm_sessions.json</c> written by <see cref="DmSessionJsonFile"/>.
/// Magic item names from the detail page are joined with "; " in one column;
/// when no detail items were fetched, the list-page summary text is used instead.
/// </summary>
public static class DmSessionCsvExporter
{
    /// <summary> Written next to <c>dm_sessions.json</c> as <c>dm_sessions.csv</c>. </summary>
    public const string DefaultOutputFileName = "dm_sessions.csv";

    public sealed class Result
    {
        public required string OutputPath { get; init; }
        public int RowCount { get; init; }
    }

    public static async Task<Result> ExportAsync(
        string outputFolder,
        IEnumerable<DmSessionRecord> sessions,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputFolder);
        ArgumentNullException.ThrowIfNull(sessions);

        Directory.CreateDirectory(outputFolder);
        var outputPath = Path.Combine(outputFolder, DefaultOutputFileName);

        // Most recent first, matching the DM Sessions window's default order.
        var sorted = sessions
            .OrderByDescending(s => s.DateDmed, StringComparer.Ordinal)
            .ThenBy(s => s.Id, StringComparer.Ordinal)
            .ToList();

        await using var fs = File.Create(outputPath);
        await using var textWriter = new StreamWriter(fs, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        await using var writer = new CsvWriter(textWriter, CultureInfo.InvariantCulture);

        string[] header =
        [
            "Id", "Date Dmed", "Adventure Title", "Session Num", "DM Reward Choice",
            "Character Name", "Character Id", "Detail Fetched", "Session Length Hours",
            "Player Level", "Location Played", "XP Gained", "GP Gained", "Downtime Gained",
            "Renown Gained", "Secret Missions", "Date Played", "Notes", "Magic Items",
        ];
        foreach (var h in header)
            writer.WriteField(h);
        await writer.NextRecordAsync();

        foreach (var s in sorted)
        {
            cancellationToken.ThrowIfCancellationRequested();

            writer.WriteField(s.Id);
            writer.WriteField(s.DateDmed);
            writer.WriteField(s.AdventureTitle);
            writer.WriteField(s.SessionNum);
            writer.WriteField(s.DmRewardChoice);
            writer.WriteField(s.CharacterName);
            writer.WriteField(s.CharacterId);
            writer.WriteField(s.DetailFetched ? "true" : "false");
            writer.WriteField(s.SessionLengthHours);
            writer.WriteField(s.PlayerLevel);
            writer.WriteField(s.LocationPlayed);
            writer.WriteField(s.XpGained);
            writer.WriteField(s.GpGained);
            writer.WriteField(s.DowntimeGained);
            writer.WriteField(s.RenownGained);
            writer.WriteField(s.NumSecretMissions);
            writer.WriteField(s.DatePlayed);
            writer.WriteField(s.Notes);
            writer.WriteField(MagicItemsText(s));
            await writer.NextRecordAsync();
        }

        await writer.FlushAsync();
        return new Result { OutputPath = outputPath, RowCount = sorted.Count };
    }

    private static string MagicItemsText(DmSessionRecord session)
    {
        if (session.MagicItems is { Count: > 0 } items)
            return string.Join("; ", items.Select(i => i.Name).Where(n => !string.IsNullOrWhiteSpace(n)));
        return session.MagicItemsSummary ?? string.Empty;
    }
}
