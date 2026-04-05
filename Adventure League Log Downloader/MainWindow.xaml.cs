using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Adventure_League_Log_Downloader.Services;

namespace Adventure_League_Log_Downloader;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly ISettingsService _settingsService = new SettingsService();
    private UserSettings _settings = UserSettings.CreateDefaults();
    private readonly ICredentialStore _credentialStore = new WindowsCredentialStore();
    private readonly ObservableCollection<CharacterRecord> _characterPreview = new();

    private string _accountUsername = string.Empty;
    private string _accountPassword = string.Empty;
    private bool _rememberCredentials;

    private DmSessionWindow? _dmSessionWindow;
    private AdditionalDataWindow? _additionalDataWindow;

    public MainWindow()
    {
        InitializeComponent();
        CharactersDataGrid.ItemsSource = _characterPreview;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        _settings = await _settingsService.LoadAsync();
        _settings.OutputFolder ??= string.Empty;
        _settings.OutputFileName ??= string.Empty;

        StatusText.Text = $"Settings: {_settingsService.SettingsPath}";

        RefreshLastWebsiteDownloadDisplay();

        SkipCharacterCsvsCheckBox.IsChecked = _settings.SkipCharacterCsvs;
        OnlyMissingCharacterCsvsCheckBox.IsChecked = _settings.DownloadOnlyMissingCharacterCsvs;
        ApplyCharacterCsvOptionEnabledState();

        await ReloadPreviewFromSavedFileAsync();

        try
        {
            var creds = _credentialStore.Load();
            if (creds != null
                && !string.IsNullOrWhiteSpace(creds.Value.Username)
                && !string.IsNullOrWhiteSpace(creds.Value.Password))
            {
                _accountUsername = creds.Value.Username;
                _accountPassword = creds.Value.Password;
                _rememberCredentials = true;
                StatusText.Text = "Credentials loaded from secure store.";
            }
        }
        catch
        {
            // ignore load errors; do not surface secrets
        }
    }

    private void OnAccountClick(object sender, RoutedEventArgs e)
    {
        if (!ShowAccountDialog())
            return;

        StatusText.Text = _rememberCredentials
            ? "Account updated; credentials saved securely."
            : "Account updated (not saved to Credential Manager).";
    }

    private async void OnSaveDefaultsClick(object sender, RoutedEventArgs e)
    {
        CopyCharacterCsvOptionsFromUiToSettings();
        await _settingsService.SaveAsync(_settings);
        StatusText.Text = "Defaults saved.";
    }

    private void OnExitClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnClearCharacterSelectionClick(object sender, RoutedEventArgs e) =>
        CharactersDataGrid.UnselectAll();

    private void OnAdditionalDataWindowClick(object sender, RoutedEventArgs e)
    {
        if (_additionalDataWindow != null && _additionalDataWindow.IsLoaded)
        {
            _additionalDataWindow.Activate();
            return;
        }

        var folder = _settings.OutputFolder?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(folder))
        {
            System.Windows.MessageBox.Show(this,
                "Set a data folder first (Options → Data location).",
                "More data",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        _additionalDataWindow = new AdditionalDataWindow(
            _credentialStore, folder,
            _accountUsername, _accountPassword, _rememberCredentials)
        {
            Owner = this
        };
        _additionalDataWindow.Show();
    }

    private void OnDmSessionWindowClick(object sender, RoutedEventArgs e)
    {
        if (_dmSessionWindow != null && _dmSessionWindow.IsLoaded)
        {
            _dmSessionWindow.Activate();
            return;
        }

        var folder = _settings.OutputFolder?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(folder))
        {
            System.Windows.MessageBox.Show(this,
                "Set a data folder first (Options → Data location).",
                "DM Session Downloader",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        _dmSessionWindow = new DmSessionWindow(
            _credentialStore, _settingsService, folder,
            _accountUsername, _accountPassword, _rememberCredentials)
        {
            Owner = this
        };
        _dmSessionWindow.Show();
    }

    private void OnAboutClick(object sender, RoutedEventArgs e)
    {
        System.Windows.MessageBox.Show(this,
            "Adventurers League Log Downloader\nWPF front-end for scraping and exporting logs.",
            "About",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void OnOpenDataFilesInExplorerClick(object sender, RoutedEventArgs e)
    {
        var folder = _settings.OutputFolder?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(folder))
        {
            System.Windows.MessageBox.Show(this,
                "Set a data folder first (Options → Data location).",
                "Open Files in Explorer",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        try
        {
            Directory.CreateDirectory(folder);

            var fileName = _settings.OutputFileName?.Trim() ?? string.Empty;
            var jsonPath = string.IsNullOrWhiteSpace(fileName)
                ? null
                : Path.Combine(folder, fileName);

            if (!string.IsNullOrWhiteSpace(jsonPath) && File.Exists(jsonPath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{jsonPath}\"",
                    UseShellExecute = true
                });
            }
            else
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = folder,
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(this, ex.Message, "Open Files in Explorer", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnOpenSettingsFolderClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var folder = Path.GetDirectoryName(_settingsService.SettingsPath);
            if (!string.IsNullOrWhiteSpace(folder))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = folder,
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(this, ex.Message, "Open Settings Folder", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void OnDownloadClick(object sender, RoutedEventArgs e)
    {
        if (!TryEnsureOutputPath(out var outputPath, out var msg))
        {
            System.Windows.MessageBox.Show(this, msg, "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var selectedCharacters = CharactersDataGrid.SelectedItems
            .OfType<CharacterRecord>()
            .DistinctBy(c => c.Id)
            .ToList();
        if (selectedCharacters.Count > 0)
        {
            if (!EnsureAccountCredentials())
            {
                StatusText.Text = "Ready";
                return;
            }

            DownloadButton.IsEnabled = false;
            try
            {
                await DownloadSelectedCharacterCsvsAsync(selectedCharacters, outputPath);
            }
            finally
            {
                DownloadButton.IsEnabled = true;
            }

            return;
        }

        CopyCharacterCsvOptionsFromUiToSettings();
        await _settingsService.SaveAsync(_settings);

        if (!EnsureAccountCredentials())
        {
            StatusText.Text = "Ready";
            return;
        }

        var username = _accountUsername;
        var password = _accountPassword;

        DownloadButton.IsEnabled = false;
        _characterPreview.Clear();
        CharactersPreviewSummary.Text = "Logging in…";
        StatusText.Text = "Logging in…";

        IProgress<CharacterScrapeReport> progress = new System.Progress<CharacterScrapeReport>(ApplyScrapeReport);

        try
        {
            var auth = new AdventurersLeagueAuth(username, password);
            var scraper = new CharacterScraper(auth);

            var results = await scraper.ScrapeAsync(_settings.DelaySeconds, progress);

            var sorted = results.Values.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase).ToList();
            var dataDir = CharacterCsvDownloader.GetCharacterDataDirectory(outputPath);
            var skipCsvs = SkipCharacterCsvsCheckBox.IsChecked == true;
            var onlyMissing = !skipCsvs && OnlyMissingCharacterCsvsCheckBox.IsChecked == true;
            List<string> idsForCsv;
            if (skipCsvs)
                idsForCsv = [];
            else if (onlyMissing)
                idsForCsv = sorted.Where(c => !CharacterCsvLocator.LocalCsvExists(outputPath, c.Id)).Select(c => c.Id).ToList();
            else
                idsForCsv = sorted.Select(c => c.Id).ToList();

            var csvFailed = 0;
            if (idsForCsv.Count > 0)
            {
                StatusText.Text = "Downloading CSV files…";
                var csvDownloader = new CharacterCsvDownloader(auth);
                csvFailed = await csvDownloader.DownloadAllAsync(idsForCsv, dataDir, _settings.DelaySeconds, progress, sorted);
            }

            var previousById = await CharacterJsonFile.TryLoadDictionaryAsync(outputPath);
            foreach (var c in sorted)
            {
                CharacterLogCsvReader.ApplyLatestSessionFromCsvIfPresent(c, outputPath);
                if (!string.IsNullOrWhiteSpace(c.LastSessionPlayed))
                    continue;
                if (previousById != null && previousById.TryGetValue(c.Id, out var prev) && !string.IsNullOrWhiteSpace(prev.LastSessionPlayed))
                    c.LastSessionPlayed = prev.LastSessionPlayed;
            }

            CharacterCsvLocator.RefreshLocalCsvFlags(outputPath, sorted);

            progress.Report(new CharacterScrapeReport
            {
                Phase = CharacterScrapePhase.Saving,
                CharacterCount = sorted.Count,
                Characters = sorted,
                Detail = "Saving character list…"
            });
            StatusText.Text = "Saving character list…";

            var dictToSave = sorted.ToDictionary(c => c.Id, c => c);
            await CharacterJsonFile.SaveAsync(outputPath, dictToSave);

            _settings.LastWebsiteDownloadUtc = DateTimeOffset.UtcNow;
            await _settingsService.SaveAsync(_settings);
            RefreshLastWebsiteDownloadDisplay();

            var csvOk = idsForCsv.Count - csvFailed;
            var csvDetail = skipCsvs
                ? "CSV downloads skipped (list only)."
                : idsForCsv.Count == 0
                    ? onlyMissing
                        ? "All character CSV files were already present; no CSV downloads."
                        : "No characters to download CSVs for."
                    : csvFailed == 0
                        ? $"Downloaded {csvOk} character CSV file(s) next to your character list."
                        : $"Downloaded {csvOk} CSV file(s); {csvFailed} failed (see status text). CSVs are next to your character list.";

            progress.Report(new CharacterScrapeReport
            {
                Phase = CharacterScrapePhase.Complete,
                CharacterCount = sorted.Count,
                Characters = sorted,
                Detail = $"Saved {Path.GetFileName(outputPath)}. {csvDetail}"
            });

            StatusText.Text = "Done";
            var successBody = skipCsvs || idsForCsv.Count == 0
                ? $"Character list saved to:\n{outputPath}\n\n{csvDetail}"
                : $"Character list saved to:\n{outputPath}\n\nCharacter CSVs ({csvOk} ok{(csvFailed > 0 ? $", {csvFailed} failed" : "")}):\n{dataDir}";
            System.Windows.MessageBox.Show(this, successBody, "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            StatusText.Text = "Error";
            ApplyScrapeReport(new CharacterScrapeReport
            {
                Phase = CharacterScrapePhase.Error,
                CharacterCount = _characterPreview.Count,
                Characters = _characterPreview.ToList(),
                Detail = ex.Message
            });
            System.Windows.MessageBox.Show(this, ex.Message, "Download Failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            DownloadButton.IsEnabled = true;
        }
    }

    private async void OnDelayOptionsClick(object sender, RoutedEventArgs e)
    {
        var dlg = new OptionsDialog(_settings.DelaySeconds);
        dlg.Owner = this;
        var result = dlg.ShowDialog();
        if (result == true)
        {
            _settings.DelaySeconds = dlg.DelaySeconds;
            await _settingsService.SaveAsync(_settings);
            StatusText.Text = $"Delay set to {_settings.DelaySeconds:0.##} s";
        }
    }

    private async void OnDataLocationClick(object sender, RoutedEventArgs e)
    {
        var dlg = new DataLocationDialog(_settings.OutputFolder, _settings.OutputFileName)
        {
            Owner = this
        };
        if (dlg.ShowDialog() != true)
            return;

        _settings.OutputFolder = dlg.OutputFolder;
        _settings.OutputFileName = dlg.OutputFileName;
        await _settingsService.SaveAsync(_settings);
        await ReloadPreviewFromSavedFileAsync();
        StatusText.Text = "Data location updated.";
    }

    /// <summary>
    /// Ensures username and password are present, prompting with <see cref="AccountDialog"/> if needed.
    /// </summary>
    private bool EnsureAccountCredentials()
    {
        if (!string.IsNullOrWhiteSpace(_accountUsername) && !string.IsNullOrWhiteSpace(_accountPassword))
            return true;

        return ShowAccountDialog();
    }

    private bool ShowAccountDialog()
    {
        var dlg = new AccountDialog(_credentialStore, _accountUsername, _accountPassword, _rememberCredentials)
        {
            Owner = this
        };
        if (dlg.ShowDialog() != true)
            return false;

        _accountUsername = dlg.Username;
        _accountPassword = dlg.Password;
        _rememberCredentials = dlg.RememberCredentials;
        return true;
    }

    private bool TryEnsureOutputPath(out string outputPath, out string validationMessage)
    {
        outputPath = string.Empty;
        validationMessage = string.Empty;

        var folder = _settings.OutputFolder?.Trim() ?? string.Empty;
        var fileName = _settings.OutputFileName?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(folder))
        {
            validationMessage = "Data folder is not set. Use Options → Data location.";
            return false;
        }

        try
        {
            Directory.CreateDirectory(folder);
        }
        catch (Exception ex)
        {
            validationMessage = $"Cannot access or create data folder: {ex.Message}";
            return false;
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            validationMessage = "Characters file name is not set. Use Options → Data location.";
            return false;
        }

        outputPath = Path.Combine(folder, fileName);
        return true;
    }

    private async Task ReloadPreviewFromSavedFileAsync()
    {
        if (string.IsNullOrWhiteSpace(_settings.OutputFolder) || string.IsNullOrWhiteSpace(_settings.OutputFileName))
        {
            _characterPreview.Clear();
            CharactersPreviewSummary.Text =
                "Use Download to load your character list. Change the save folder or file under Options → Data location if needed.";
            return;
        }

        var path = Path.Combine(_settings.OutputFolder.Trim(), _settings.OutputFileName.Trim());
        var list = await CharacterJsonFile.TryLoadSortedAsync(path);
        _characterPreview.Clear();
        if (list is not { Count: > 0 })
        {
            CharactersPreviewSummary.Text =
                "Use Download to load your character list. Change the save folder or file under Options → Data location if needed.";
            return;
        }

        foreach (var c in list)
        {
            CharacterLogCsvReader.ApplyLatestSessionFromCsvIfPresent(c, path);
            _characterPreview.Add(c);
        }

        CharacterCsvLocator.RefreshLocalCsvFlags(path, _characterPreview);

        ClearDataGridSortState();
        CharactersDataGrid.Items.Refresh();

        CharactersPreviewSummary.Text =
            $"Showing {list.Count} character(s) from {Path.GetFileName(path)} ({path}).";
    }

    private void RefreshLastWebsiteDownloadDisplay()
    {
        LastWebsiteDownloadText.Text = _settings.LastWebsiteDownloadUtc is { } utc
            ? $"Last downloaded from site: {utc.ToLocalTime():g}"
            : "Last downloaded from site: never";
    }

    private void ApplyScrapeReport(CharacterScrapeReport report)
    {
        _characterPreview.Clear();
        foreach (var c in report.Characters)
            _characterPreview.Add(c);

        var phaseLabel = report.Phase switch
        {
            CharacterScrapePhase.DiscoveringPages => "Discovering pages",
            CharacterScrapePhase.Scraping => "Loading character list",
            CharacterScrapePhase.Saving => "Saving",
            CharacterScrapePhase.DownloadingCsvs => "Downloading CSVs",
            CharacterScrapePhase.Complete => "Complete",
            CharacterScrapePhase.Error => "Error",
            _ => "Ready"
        };

        var pagePart = report.CurrentPage is { } p
            ? report.TotalPages is { } tp
                ? $" · Page {p} of {tp}"
                : $" · Page {p}"
            : "";

        var head = $"{report.CharacterCount} characters · {phaseLabel}{pagePart}";
        CharactersPreviewSummary.Text = string.IsNullOrWhiteSpace(report.Detail)
            ? head
            : $"{head}{Environment.NewLine}{report.Detail}";

        if (report.Phase is CharacterScrapePhase.Scraping or CharacterScrapePhase.DiscoveringPages
            or CharacterScrapePhase.Saving or CharacterScrapePhase.DownloadingCsvs)
        {
            StatusText.Text = report.Detail ?? head;
        }
        else if (report.Phase == CharacterScrapePhase.Complete)
        {
            StatusText.Text = "Done";
        }
        else if (report.Phase == CharacterScrapePhase.Error)
        {
            StatusText.Text = "Error";
        }

        // Ensure "Last session" and other bound fields refresh when records are mutated in place.
        CharactersDataGrid.Items.Refresh();
    }

    private void OnSkipCharacterCsvsChanged(object sender, RoutedEventArgs e) =>
        ApplyCharacterCsvOptionEnabledState();

    private void ApplyCharacterCsvOptionEnabledState()
    {
        OnlyMissingCharacterCsvsCheckBox.IsEnabled = SkipCharacterCsvsCheckBox.IsChecked != true;
    }

    private void CopyCharacterCsvOptionsFromUiToSettings()
    {
        _settings.SkipCharacterCsvs = SkipCharacterCsvsCheckBox.IsChecked == true;
        _settings.DownloadOnlyMissingCharacterCsvs = OnlyMissingCharacterCsvsCheckBox.IsChecked == true;
    }

    private void CharactersDataGrid_OnSorting(object sender, DataGridSortingEventArgs e)
    {
        if (CollectionViewSource.GetDefaultView(_characterPreview) is not ListCollectionView view)
            return;

        if (e.Column.SortMemberPath is not ("Level" or "LastSessionPlayed"))
        {
            view.CustomSort = null;
            return;
        }

        e.Handled = true;

        var newDir = e.Column.SortDirection != ListSortDirection.Ascending
            ? ListSortDirection.Ascending
            : ListSortDirection.Descending;
        e.Column.SortDirection = newDir;

        view.CustomSort = e.Column.SortMemberPath == "Level"
            ? new CharacterRecordLevelComparer(newDir)
            : new CharacterRecordLastSessionComparer(newDir);
    }

    private void ClearDataGridSortState()
    {
        if (CollectionViewSource.GetDefaultView(_characterPreview) is ListCollectionView lcv)
            lcv.CustomSort = null;
        foreach (var col in CharactersDataGrid.Columns)
            col.SortDirection = null;
    }

    private async Task DownloadSelectedCharacterCsvsAsync(IReadOnlyList<CharacterRecord> selected, string outputPath)
    {
        try
        {
            var auth = new AdventurersLeagueAuth(_accountUsername, _accountPassword);
            var downloader = new CharacterCsvDownloader(auth);
            var dataDir = CharacterCsvDownloader.GetCharacterDataDirectory(outputPath);
            var delayMs = (int)Math.Round(_settings.DelaySeconds * 1000);
            var failed = 0;
            var total = selected.Count;

            for (var i = 0; i < total; i++)
            {
                var c = selected[i];
                StatusText.Text = $"Downloading CSV {i + 1} of {total}: {c.Name}…";

                var ok = await downloader.DownloadOneAsync(c.Id, dataDir);
                if (!ok)
                {
                    failed++;
                    continue;
                }

                CharacterLogCsvReader.ApplyLatestSessionFromCsvIfPresent(c, outputPath);

                if (delayMs > 0 && i < total - 1)
                    await Task.Delay(delayMs);
            }

            CharacterCsvLocator.RefreshLocalCsvFlags(outputPath, _characterPreview);
            await CharacterJsonFile.SaveAsync(outputPath, _characterPreview.ToDictionary(x => x.Id, x => x));

            CharactersDataGrid.Items.Refresh();
            StatusText.Text = "Done";

            var okCount = total - failed;
            var body = failed == 0
                ? $"Downloaded CSV for {okCount} character(s) into:\n{dataDir}"
                : $"Downloaded {okCount} CSV file(s); {failed} failed (check status / connection).\n\nFolder:\n{dataDir}";
            System.Windows.MessageBox.Show(this, body, "Download", MessageBoxButton.OK,
                failed == 0 ? MessageBoxImage.Information : MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            StatusText.Text = "Error";
            System.Windows.MessageBox.Show(this, ex.Message, "Download", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}