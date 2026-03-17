using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
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

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        _settings = await _settingsService.LoadAsync();

        // populate UI
        OutputFolderTextBox.Text = _settings.OutputFolder;
        OutputFileTextBox.Text = _settings.OutputFileName;

        StatusText.Text = $"Settings loaded from {_settingsService.SettingsPath}";

        // Configure Remember Credentials checkbox based on availability
        RememberCredentialsCheckBox.IsEnabled = _credentialStore.IsAvailable;
        RememberCredentialsCheckBox.ToolTip = _credentialStore.IsAvailable
            ? "Store credentials securely in Windows Credential Manager."
            : "Secure credential storage is not available on this system.";

        // Attempt to load saved credentials
        try
        {
            var creds = _credentialStore.Load();
            if (creds != null)
            {
                UsernameTextBox.Text = creds.Value.Username;
                PasswordBox.Password = creds.Value.Password;
                RememberCredentialsCheckBox.IsChecked = true;
                StatusText.Text = "Credentials loaded from secure store.";
            }
        }
        catch
        {
            // ignore load errors; do not surface secrets
        }
    }

    private void OnBrowseFolderClick(object sender, RoutedEventArgs e)
    {
        using var dlg = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Select output folder",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = true,
        };
        var result = dlg.ShowDialog();
        if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(dlg.SelectedPath))
        {
            OutputFolderTextBox.Text = dlg.SelectedPath;
        }
    }

    private async void OnSaveDefaultsClick(object sender, RoutedEventArgs e)
    {
        if (!TryReadUiIntoSettings(out var msg))
        {
            System.Windows.MessageBox.Show(this, msg, "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await _settingsService.SaveAsync(_settings);
        StatusText.Text = "Defaults saved.";
    }

    private void OnExitClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnAboutClick(object sender, RoutedEventArgs e)
    {
        System.Windows.MessageBox.Show(this,
            "Adventurers League Log Downloader\nWPF front-end for scraping and exporting logs.",
            "About",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
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

    private async void OnRunClick(object sender, RoutedEventArgs e)
    {
        if (!TryReadUiIntoSettings(out var msg))
        {
            System.Windows.MessageBox.Show(this, msg, "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Persist latest non-sensitive settings
        await _settingsService.SaveAsync(_settings);

        var username = UsernameTextBox.Text?.Trim();
        var password = PasswordBox.Password; // not persisted

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            System.Windows.MessageBox.Show(this, "Username and password are required to log in.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var outputPath = Path.Combine(_settings.OutputFolder, _settings.OutputFileName);

        RunButton.IsEnabled = false;
        SaveDefaultsButton.IsEnabled = false;
        StatusText.Text = "Logging in…";

        try
        {
            var auth = new AdventurersLeagueAuth(username!, password);
            var scraper = new CharacterScraper(auth);

            StatusText.Text = "Determining pages…";
            var maxPage = await scraper.GetMaxPageAsync();

            StatusText.Text = $"Scraping {maxPage} page(s)…";
            _ = await scraper.ScrapeAsync(_settings.DelaySeconds);

            StatusText.Text = "Saving JSON…";
            await scraper.SaveJsonAsync(outputPath);

            // Save or delete credentials based on checkbox
            try
            {
                if (RememberCredentialsCheckBox.IsChecked == true && _credentialStore.IsAvailable)
                {
                    _credentialStore.Save(username!, password);
                }
                else
                {
                    _credentialStore.Delete();
                }
            }
            catch
            {
                // Non-fatal: ignore errors from credential store operations
            }

            StatusText.Text = "Done";
            System.Windows.MessageBox.Show(this, $"Saved to:\n{outputPath}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            StatusText.Text = "Error";
            System.Windows.MessageBox.Show(this, ex.Message, "Run Failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            RunButton.IsEnabled = true;
            SaveDefaultsButton.IsEnabled = true;
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

    private bool TryReadUiIntoSettings(out string validationMessage)
    {
        validationMessage = string.Empty;

        var folder = OutputFolderTextBox.Text?.Trim() ?? string.Empty;
        var fileName = OutputFileTextBox.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(folder))
        {
            validationMessage = "Output folder is required.";
            return false;
        }

        try
        {
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
        }
        catch (Exception ex)
        {
            validationMessage = $"Cannot access or create output folder: {ex.Message}";
            return false;
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            validationMessage = "Output file name is required.";
            return false;
        }

        _settings.OutputFolder = folder;
        _settings.OutputFileName = fileName;

        return true;
    }
}