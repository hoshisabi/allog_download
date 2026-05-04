using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Navigation;

namespace Adventure_League_Log_Downloader;

public partial class AboutDialog : Window
{
    public AboutDialog()
    {
        InitializeComponent();
        VersionBuildText.Text = BuildAboutVersionLines();
    }

    private static string BuildAboutVersionLines()
    {
        var asm = typeof(AboutDialog).Assembly;
        var informational = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion?.Trim();
        var nameVer = asm.GetName().Version;

        var displayVersion = string.IsNullOrEmpty(informational)
            ? nameVer?.ToString(3) ?? "unknown"
            : informational;

        var fileVerLine = TryGetFileVersionLine();
        var verLine = $"Version {displayVersion}";
        return string.IsNullOrEmpty(fileVerLine) ? verLine : $"{verLine}{Environment.NewLine}{fileVerLine}";
    }

    /// <summary>
    /// Product/file version from the running executable when available.
    /// Uses <see cref="Environment.ProcessPath"/> only — <see cref="Assembly.Location"/> is empty under single-file publish.
    /// </summary>
    private static string? TryGetFileVersionLine()
    {
        try
        {
            var path = Environment.ProcessPath;
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return null;

            var info = FileVersionInfo.GetVersionInfo(path);
            var product = info.ProductVersion?.Trim();
            var file = info.FileVersion?.Trim();
            if (!string.IsNullOrEmpty(product) && product == file)
                return $"Build {product}";
            if (!string.IsNullOrEmpty(product) && !string.IsNullOrEmpty(file))
                return $"Build product {product} · file {file}";
            if (!string.IsNullOrEmpty(product))
                return $"Build {product}";
            if (!string.IsNullOrEmpty(file))
                return $"Build {file}";
        }
        catch
        {
            // ignore; version line above is enough
        }

        return null;
    }

    private void OnLinkNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }

    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
