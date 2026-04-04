using System;
using System.IO;
using System.Windows;
using Adventure_League_Log_Downloader.Services;

namespace Adventure_League_Log_Downloader;

public partial class DataLocationDialog : Window
{
    public string OutputFolder { get; private set; } = string.Empty;
    public string OutputFileName { get; private set; } = string.Empty;

    public DataLocationDialog(string currentFolder, string currentFileName)
    {
        InitializeComponent();
        OutputFolder = currentFolder ?? string.Empty;
        OutputFileName = string.IsNullOrWhiteSpace(currentFileName) ? "characters.json" : currentFileName;
        FolderTextBox.Text = OutputFolder;
        FileNameTextBox.Text = OutputFileName;
    }

    private void OnBrowseClick(object sender, RoutedEventArgs e)
    {
        using var dlg = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Select folder for character list JSON",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = true,
        };
        if (Directory.Exists(FolderTextBox.Text?.Trim()))
            dlg.SelectedPath = FolderTextBox.Text.Trim();
        if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(dlg.SelectedPath))
            FolderTextBox.Text = dlg.SelectedPath;
    }

    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        var folder = FolderTextBox.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(folder))
            folder = UserSettings.DefaultDataFolder;

        var fileName = FileNameTextBox.Text?.Trim() ?? string.Empty;

        try
        {
            Directory.CreateDirectory(folder);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(this, $"Cannot use that folder: {ex.Message}", "Data location", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            System.Windows.MessageBox.Show(this, "File name is required.", "Data location", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        OutputFolder = folder;
        OutputFileName = fileName;
        DialogResult = true;
        Close();
    }
}
