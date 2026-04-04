using System.Windows;
using Adventure_League_Log_Downloader.Services;

namespace Adventure_League_Log_Downloader;

public partial class AccountDialog : Window
{
    private readonly ICredentialStore _credentialStore;

    public string Username { get; private set; } = string.Empty;
    public string Password { get; private set; } = string.Empty;
    public bool RememberCredentials { get; private set; }

    public AccountDialog(
        ICredentialStore credentialStore,
        string? currentUsername,
        string? currentPassword,
        bool rememberCredentials)
    {
        InitializeComponent();
        _credentialStore = credentialStore;

        UsernameTextBox.Text = currentUsername ?? string.Empty;
        PasswordBox.Password = currentPassword ?? string.Empty;
        RememberCredentialsCheckBox.IsChecked = rememberCredentials;

        RememberCredentialsCheckBox.IsEnabled = _credentialStore.IsAvailable;
        RememberCredentialsCheckBox.ToolTip = _credentialStore.IsAvailable
            ? null
            : "Secure credential storage is not available on this system.";
    }

    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        var username = UsernameTextBox.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(username))
        {
            System.Windows.MessageBox.Show(this, "Username is required.", "Account", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var password = PasswordBox.Password ?? string.Empty;
        if (string.IsNullOrWhiteSpace(password))
        {
            System.Windows.MessageBox.Show(this, "Password is required.", "Account", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Username = username;
        Password = password;
        RememberCredentials = RememberCredentialsCheckBox.IsChecked == true;

        try
        {
            if (RememberCredentials && _credentialStore.IsAvailable)
                _credentialStore.Save(Username, Password);
            else
                _credentialStore.Delete();
        }
        catch
        {
            System.Windows.MessageBox.Show(this,
                "Could not update saved credentials. Your sign-in will work for this session only.",
                "Account",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        DialogResult = true;
        Close();
    }
}
