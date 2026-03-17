using System;
using System.Globalization;
using System.Windows;

namespace Adventure_League_Log_Downloader;

public partial class OptionsDialog : Window
{
    public double DelaySeconds { get; private set; }

    public OptionsDialog(double currentDelaySeconds)
    {
        InitializeComponent();
        DelaySeconds = currentDelaySeconds;
        DelayTextBox.Text = DelaySeconds.ToString("0.##", CultureInfo.InvariantCulture);
    }

    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        var text = DelayTextBox.Text?.Trim() ?? string.Empty;
        if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var delay) || delay < 0)
        {
            System.Windows.MessageBox.Show(this, "Delay must be a non-negative number (seconds).", "Options", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DelaySeconds = delay;
        DialogResult = true;
        Close();
    }
}
