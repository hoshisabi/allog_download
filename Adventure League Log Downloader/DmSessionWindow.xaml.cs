using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Adventure_League_Log_Downloader.Services;

namespace Adventure_League_Log_Downloader;

public partial class DmSessionWindow : Window
{
    private readonly ICredentialStore _credentialStore;
    private readonly ISettingsService _settingsService;
    private readonly string _outputFolder;
    private readonly ObservableCollection<DmSessionRecord> _sessionPreview = new();

    private string _accountUsername;
    private string _accountPassword;
    private bool _rememberCredentials;

    public DmSessionWindow(
        ICredentialStore credentialStore,
        ISettingsService settingsService,
        string outputFolder,
        string accountUsername,
        string accountPassword,
        bool rememberCredentials)
    {
        InitializeComponent();
        _credentialStore = credentialStore;
        _settingsService = settingsService;
        _outputFolder = outputFolder;
        _accountUsername = accountUsername;
        _accountPassword = accountPassword;
        _rememberCredentials = rememberCredentials;

        SessionsDataGrid.ItemsSource = _sessionPreview;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        var settings = await _settingsService.LoadAsync();
        SkipDmSessionDetailsCheckBox.IsChecked = settings.SkipDmSessionDetails;
        OnlyMissingDmSessionDetailsCheckBox.IsChecked = settings.DownloadOnlyMissingDmSessionDetails;
        ApplyDmSessionDetailOptionEnabledState();

        // Load credentials from store if none were passed in
        if (string.IsNullOrWhiteSpace(_accountUsername))
        {
            try
            {
                var creds = _credentialStore.Load();
                if (creds != null
                    && !string.IsNullOrWhiteSpace(creds.Value.Username)
                    && !string.IsNullOrWhiteSpace(creds.Value.Password))
                {
                    _accountUsername    = creds.Value.Username;
                    _accountPassword    = creds.Value.Password;
                    _rememberCredentials = true;
                }
            }
            catch { /* ignore */ }
        }

        await LoadPreviewFromFileAsync();
    }

    private void OnClearSessionSelectionClick(object sender, RoutedEventArgs e) =>
        SessionsDataGrid.UnselectAll();

    private async void OnSkipDmSessionDetailsChanged(object sender, RoutedEventArgs e)
    {
        ApplyDmSessionDetailOptionEnabledState();
        await PersistDmSessionOptionsToSettingsAsync();
    }

    private async void OnOnlyMissingDmSessionDetailsChanged(object sender, RoutedEventArgs e) =>
        await PersistDmSessionOptionsToSettingsAsync();

    private void ApplyDmSessionDetailOptionEnabledState()
    {
        OnlyMissingDmSessionDetailsCheckBox.IsEnabled = SkipDmSessionDetailsCheckBox.IsChecked != true;
    }

    private async Task PersistDmSessionOptionsToSettingsAsync()
    {
        var s = await _settingsService.LoadAsync();
        s.SkipDmSessionDetails = SkipDmSessionDetailsCheckBox.IsChecked == true;
        s.DownloadOnlyMissingDmSessionDetails = OnlyMissingDmSessionDetailsCheckBox.IsChecked == true;
        await _settingsService.SaveAsync(s);
    }

    private async void OnDownloadClick(object sender, RoutedEventArgs e)
    {
        if (!EnsureAccountCredentials())
        {
            StatusText.Text = "Ready";
            return;
        }

        if (string.IsNullOrWhiteSpace(_outputFolder))
        {
            System.Windows.MessageBox.Show(this,
                "Output folder is not set. Set it in the main window under Options → Data location.",
                "DM Session Downloader",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var outputPath = Path.Combine(_outputFolder, "dm_sessions.json");

        var selectedSessions = SessionsDataGrid.SelectedItems
            .OfType<DmSessionRecord>()
            .DistinctBy(s => s.Id)
            .ToList();
        if (selectedSessions.Count > 0)
        {
            DownloadButton.IsEnabled = false;
            try
            {
                StatusText.Text = $"Fetching detail for {selectedSessions.Count} session(s)…";
                await PersistDmSessionOptionsToSettingsAsync();
                var settings = await _settingsService.LoadAsync();
                var auth = new AdventurersLeagueAuth(_accountUsername, _accountPassword);
                var scraper = new DmSessionScraper(auth);
                var (ok, failed) = await scraper.FetchSessionDetailsBatchAsync(
                    outputPath, selectedSessions, settings.DelaySeconds);
                await LoadPreviewFromFileAsync();
                StatusText.Text = "Done";

                if (ok == 0 && failed > 0)
                {
                    System.Windows.MessageBox.Show(this,
                        "No session details could be fetched (check your connection or session).",
                        "Download",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
                else
                {
                    var body = failed == 0
                        ? $"Saved detail for {ok} session(s)."
                        : $"Saved detail for {ok} session(s); {failed} failed.";
                    System.Windows.MessageBox.Show(this, body, "Download", MessageBoxButton.OK,
                        failed == 0 ? MessageBoxImage.Information : MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = "Error";
                System.Windows.MessageBox.Show(this, ex.Message, "Download", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                DownloadButton.IsEnabled = true;
            }

            return;
        }

        DownloadButton.IsEnabled = false;
        _sessionPreview.Clear();
        SessionsSummary.Text = "Starting…";
        StatusText.Text = "Starting…";

        IProgress<DmSessionScrapeReport> progress = new System.Progress<DmSessionScrapeReport>(ApplyReport);

        try
        {
            Directory.CreateDirectory(_outputFolder);

            await PersistDmSessionOptionsToSettingsAsync();
            var settings = await _settingsService.LoadAsync();
            var auth = new AdventurersLeagueAuth(_accountUsername, _accountPassword);
            var scraper = new DmSessionScraper(auth);
            var skipDetails = SkipDmSessionDetailsCheckBox.IsChecked == true;
            var onlyMissing = OnlyMissingDmSessionDetailsCheckBox.IsChecked == true;
            var results = await scraper.ScrapeAsync(
                outputPath,
                settings.DelaySeconds,
                progress,
                skipDetailPages: skipDetails,
                onlyFetchMissingDetailPages: onlyMissing);

            var sorted = results.Values
                .OrderByDescending(s => s.DateDmed)
                .ToList();

            _sessionPreview.Clear();
            foreach (var s in sorted)
                _sessionPreview.Add(s);

            var detailCount = sorted.Count(s => s.DetailFetched);
            SessionsSummary.Text =
                $"{sorted.Count} DM session(s) · {detailCount} with full details · saved to {Path.GetFileName(outputPath)}";
            StatusText.Text = "Done";
        }
        catch (Exception ex)
        {
            SessionsSummary.Text = $"Error: {ex.Message}";
            StatusText.Text = "Error";
            System.Windows.MessageBox.Show(this, ex.Message, "Download Failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            DownloadButton.IsEnabled = true;
        }
    }

    private bool EnsureAccountCredentials()
    {
        if (!string.IsNullOrWhiteSpace(_accountUsername) && !string.IsNullOrWhiteSpace(_accountPassword))
            return true;

        var dlg = new AccountDialog(_credentialStore, _accountUsername, _accountPassword, _rememberCredentials)
        {
            Owner = this
        };
        if (dlg.ShowDialog() != true)
            return false;

        _accountUsername    = dlg.Username;
        _accountPassword    = dlg.Password;
        _rememberCredentials = dlg.RememberCredentials;
        return true;
    }

    private async Task LoadPreviewFromFileAsync()
    {
        var path = Path.Combine(_outputFolder, "dm_sessions.json");
        var dict = await DmSessionJsonFile.TryLoadAsync(path);
        _sessionPreview.Clear();

        if (dict == null || dict.Count == 0)
        {
            SessionsSummary.Text =
                "No DM sessions file yet. Download (no rows selected) to load from the site, or use the main window’s DM Sessions button.";
            return;
        }

        var sorted = dict.Values.OrderByDescending(s => s.DateDmed).ToList();
        foreach (var s in sorted)
            _sessionPreview.Add(s);

        ClearDataGridSortState();
        SessionsDataGrid.Items.Refresh();

        var detailCount = sorted.Count(s => s.DetailFetched);
        SessionsSummary.Text =
            $"{sorted.Count} DM session(s) · {detailCount} with full details · from {Path.GetFileName(path)}";
    }

    private void SessionsDataGrid_OnSorting(object sender, DataGridSortingEventArgs e)
    {
        if (CollectionViewSource.GetDefaultView(_sessionPreview) is not ListCollectionView view)
            return;

        if (e.Column.SortMemberPath is not ("DateDmed" or "SessionNum" or "DetailFetched"))
        {
            view.CustomSort = null;
            return;
        }

        e.Handled = true;

        var newDir = e.Column.SortDirection != ListSortDirection.Ascending
            ? ListSortDirection.Ascending
            : ListSortDirection.Descending;
        e.Column.SortDirection = newDir;

        view.CustomSort = e.Column.SortMemberPath switch
        {
            "DateDmed" => new DmSessionDateDmedComparer(newDir),
            "SessionNum" => new DmSessionSessionNumComparer(newDir),
            "DetailFetched" => new DmSessionDetailFetchedComparer(newDir),
            _ => null
        };
    }

    private void ClearDataGridSortState()
    {
        if (CollectionViewSource.GetDefaultView(_sessionPreview) is ListCollectionView lcv)
            lcv.CustomSort = null;
        foreach (var col in SessionsDataGrid.Columns)
            col.SortDirection = null;
    }

    private void ApplyReport(DmSessionScrapeReport report)
    {
        var phase = report.Phase switch
        {
            DmSessionScrapePhase.DiscoveringPages => "Discovering pages",
            DmSessionScrapePhase.ScrapingList     => "Loading session list",
            DmSessionScrapePhase.FetchingDetails  => "Fetching details",
            DmSessionScrapePhase.Saving           => "Saving",
            DmSessionScrapePhase.Complete         => "Complete",
            DmSessionScrapePhase.Error            => "Error",
            _                                     => "Working…"
        };

        var detailPart = report.DetailsFetched.HasValue && report.DetailsTotal.HasValue
            ? $" · Detail {report.DetailsFetched}/{report.DetailsTotal}"
            : string.Empty;

        var pagePart = report.CurrentPage.HasValue
            ? report.TotalPages.HasValue
                ? $" · Page {report.CurrentPage}/{report.TotalPages}"
                : $" · Page {report.CurrentPage}"
            : string.Empty;

        SessionsSummary.Text =
            $"{report.SessionCount} session(s) · {phase}{pagePart}{detailPart}" +
            (string.IsNullOrWhiteSpace(report.Detail) ? string.Empty : $"\n{report.Detail}");

        StatusText.Text = report.Detail ?? phase;
    }
}
