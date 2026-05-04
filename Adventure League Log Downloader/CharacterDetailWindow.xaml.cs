using System.IO;
using System.Windows;
using Adventure_League_Log_Downloader.Services;

namespace Adventure_League_Log_Downloader;

public partial class CharacterDetailWindow : Window
{
    public CharacterDetailWindow(CharacterRecord character, string? charactersJsonPath)
    {
        InitializeComponent();

        Title = $"Character — {character.Name}";
        HeaderNameText.Text = character.Name;

        IdText.Text = OrDash(character.Id);
        ClassText.Text = OrDash(character.Class);
        LevelText.Text = OrDash(character.Level);
        RaceText.Text = OrDash(character.Race);
        SeasonText.Text = OrDash(character.Season);
        TagText.Text = OrDash(character.Tag);
        LastSessionText.Text = OrDash(character.LastSessionPlayed);

        HasCsvText.Text = character.HasLocalCsv ? "Yes (file on disk)" : "No";

        if (string.IsNullOrWhiteSpace(charactersJsonPath))
        {
            ShowCsvHelp("Set Options → Data location in the main window, then download character CSVs to load sessions here.");
            return;
        }

        var csvPath = CharacterCsvLocator.TryFindCharacterCsvFile(charactersJsonPath, character.Id);
        if (csvPath == null || !File.Exists(csvPath))
        {
            ShowCsvHelp("No character CSV file found for this character. Download CSVs from the main window.");
            return;
        }

        var detail = CharacterCsvDetailReader.TryLoad(csvPath);
        if (!detail.CsvFileFound)
        {
            ShowCsvHelp("Could not open the character CSV file.");
            return;
        }

        if (detail.Sessions.Count == 0)
        {
            ShowCsvHelp("No session rows were found in this CSV (or the file could not be parsed).");
            return;
        }

        CsvSessionsHelpText.Visibility = Visibility.Collapsed;
        SessionsList.ItemsSource = detail.Sessions;
        SessionsList.Visibility = Visibility.Visible;
    }

    private void ShowCsvHelp(string message)
    {
        CsvSessionsHelpText.Text = message;
        CsvSessionsHelpText.Visibility = Visibility.Visible;
        SessionsList.ItemsSource = null;
        SessionsList.Visibility = Visibility.Collapsed;
    }

    private static string OrDash(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "—" : value;

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
