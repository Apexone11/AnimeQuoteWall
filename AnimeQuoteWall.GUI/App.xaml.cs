using System;
using System.Windows;

namespace AnimeQuoteWall.GUI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        try
        {
            base.OnStartup(e);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Startup Error: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }
}

