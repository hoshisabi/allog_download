using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using Adventure_League_Log_Downloader.Services;

namespace Adventure_League_Log_Downloader;

public partial class AdditionalDataWindow : Window
{
    private const double DelaySeconds = 0.25;

    private readonly ICredentialStore _credentialStore;
    private readonly string _outputFolder;
    private string _accountUsername;
    private string _accountPassword;
    private bool _rememberCredentials;

    private readonly ObservableCollection<LocationRecord> _locations = new();
    private readonly ObservableCollection<PlayerDmRecord> _playerDms = new();
    private readonly ObservableCollection<CampaignListPreviewRow> _campaignRows = new();

    public AdditionalDataWindow(
        ICredentialStore credentialStore,
        string outputFolder,
        string accountUsername,
        string accountPassword,
        bool rememberCredentials)
    {
        InitializeComponent();
        _credentialStore = credentialStore;
        _outputFolder = outputFolder;
        _accountUsername = accountUsername;
        _accountPassword = accountPassword;
        _rememberCredentials = rememberCredentials;

        LocationsGrid.ItemsSource = _locations;
        PlayerDmsGrid.ItemsSource = _playerDms;
        CampaignsGrid.ItemsSource = _campaignRows;

        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_accountUsername))
        {
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
                }
            }
            catch { /* ignore */ }
        }

        await ReloadFromDiskAsync();
    }

    private async Task ReloadFromDiskAsync()
    {
        _locations.Clear();
        _playerDms.Clear();
        _campaignRows.Clear();

        try
        {
            var loc = await AdditionalSiteDataJson.TryLoadLocationsAsync(_outputFolder);
            if (loc?.Locations != null)
            {
                foreach (var x in loc.Locations)
                    _locations.Add(x);
            }

            var dms = await AdditionalSiteDataJson.TryLoadPlayerDmsAsync(_outputFolder);
            if (dms?.PlayerDms != null)
            {
                foreach (var x in dms.PlayerDms)
                    _playerDms.Add(x);
            }

            var camps = await AdditionalSiteDataJson.TryLoadCampaignsAsync(_outputFolder);
            if (camps != null)
            {
                foreach (var r in camps.DmCampaigns)
                {
                    _campaignRows.Add(new CampaignListPreviewRow
                    {
                        Role = "DMing",
                        CampaignName = r.Name,
                        CampaignId = r.Id,
                        Detail = string.IsNullOrEmpty(r.PlayerCount) ? null : $"{r.PlayerCount} players",
                    });
                }

                foreach (var r in camps.Playing)
                {
                    _campaignRows.Add(new CampaignListPreviewRow
                    {
                        Role = "Playing",
                        CampaignName = r.CampaignName,
                        CampaignId = r.CampaignId,
                        Detail = r.CharacterName,
                    });
                }
            }
        }
        catch
        {
            // ignore corrupt files
        }

        try
        {
            var label = new DirectoryInfo(_outputFolder).Name;
            StatusText.Text = $"Preview from disk ({label}).";
        }
        catch
        {
            StatusText.Text = "Preview from disk.";
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

        _accountUsername = dlg.Username;
        _accountPassword = dlg.Password;
        _rememberCredentials = dlg.RememberCredentials;
        return true;
    }

    private async void OnDownloadLocationsClick(object sender, RoutedEventArgs e)
    {
        if (!EnsureAccountCredentials())
            return;

        SetBusy(true);
        StatusText.Text = "Downloading locations…";
        try
        {
            var auth = new AdventurersLeagueAuth(_accountUsername, _accountPassword);
            var scraper = new AdditionalSiteDataScraper(auth);
            await scraper.DownloadLocationsAsync(_outputFolder, DelaySeconds);
            await ReloadFromDiskAsync();
            StatusText.Text = "Done";
            System.Windows.MessageBox.Show(this, $"Saved {AdditionalSiteDataJson.LocationsPath(_outputFolder)}", "Locations",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            StatusText.Text = "Error";
            System.Windows.MessageBox.Show(this, ex.Message, "Locations", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void OnDownloadPlayerDmsClick(object sender, RoutedEventArgs e)
    {
        if (!EnsureAccountCredentials())
            return;

        SetBusy(true);
        StatusText.Text = "Downloading player DMs…";
        try
        {
            var auth = new AdventurersLeagueAuth(_accountUsername, _accountPassword);
            var scraper = new AdditionalSiteDataScraper(auth);
            await scraper.DownloadPlayerDmsAsync(_outputFolder, DelaySeconds);
            await ReloadFromDiskAsync();
            StatusText.Text = "Done";
            System.Windows.MessageBox.Show(this, $"Saved {AdditionalSiteDataJson.PlayerDmsPath(_outputFolder)}", "Player DMs",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            StatusText.Text = "Error";
            System.Windows.MessageBox.Show(this, ex.Message, "Player DMs", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void OnDownloadCampaignsClick(object sender, RoutedEventArgs e)
    {
        if (!EnsureAccountCredentials())
            return;

        SetBusy(true);
        StatusText.Text = "Downloading campaigns (this may take a while)…";
        try
        {
            var auth = new AdventurersLeagueAuth(_accountUsername, _accountPassword);
            var scraper = new AdditionalSiteDataScraper(auth);
            await scraper.DownloadCampaignsAsync(_outputFolder, DelaySeconds);
            await ReloadFromDiskAsync();
            StatusText.Text = "Done";
            System.Windows.MessageBox.Show(this, $"Saved {AdditionalSiteDataJson.CampaignsPath(_outputFolder)}", "Campaigns",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            StatusText.Text = "Error";
            System.Windows.MessageBox.Show(this, ex.Message, "Campaigns", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetBusy(bool busy)
    {
        DownloadLocationsButton.IsEnabled = !busy;
        DownloadPlayerDmsButton.IsEnabled = !busy;
        DownloadCampaignsButton.IsEnabled = !busy;
    }
}
