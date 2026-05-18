using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using AnimeQuoteWall.GUI.Services;

namespace AnimeQuoteWall.GUI.Dialogs;

public partial class AboutDialog : Window
{
    private const string GitHubRepoUrl = "https://github.com/Apexone11/AnimeQuoteWall";

    public AboutDialog()
    {
        InitializeComponent();
        VersionLine.Text = $"Version {GetAppVersion()}  |  .NET 8  |  Windows x64";
    }

    private static string GetAppVersion()
    {
        try
        {
            var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            var info = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                       ?? assembly.GetName().Version?.ToString();
            if (!string.IsNullOrEmpty(info))
            {
                var plus = info.IndexOf('+');
                if (plus > 0) info = info[..plus];
                return info;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"AboutDialog.GetAppVersion: {ex.Message}");
        }
        return "unknown";
    }

    private void OpenGitHub_Click(object sender, RoutedEventArgs e) => ShellLauncher.OpenUrl(GitHubRepoUrl);
    private void OpenIssues_Click(object sender, RoutedEventArgs e) => ShellLauncher.OpenUrl(GitHubRepoUrl + "/issues");
    private void OpenReleases_Click(object sender, RoutedEventArgs e) => ShellLauncher.OpenUrl(GitHubRepoUrl + "/releases");

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
