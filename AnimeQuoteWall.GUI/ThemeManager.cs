using System;
using System.Linq;
using System.Windows;
using AnimeQuoteWall.Core.Configuration;
using Microsoft.Win32;

namespace AnimeQuoteWall.GUI;

public static class ThemeManager
{
    private static bool _watching;

    public static void ApplyTheme()
    {
        try
        {
            var isDark = AppConfiguration.GetEffectiveIsDark();
            var dictUri = new Uri(isDark
                ? "Resources/Themes/Theme.Dark.xaml"
                : "Resources/Themes/Theme.Light.xaml", UriKind.Relative);

            var themeDict = new ResourceDictionary { Source = dictUri };
            var app = System.Windows.Application.Current;
            if (app == null) return;

            var existing = app.Resources.MergedDictionaries
                .FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("Resources/Themes/Theme."));

            if (existing != null)
            {
                var idx = app.Resources.MergedDictionaries.IndexOf(existing);
                app.Resources.MergedDictionaries[idx] = themeDict;
            }
            else
            {
                app.Resources.MergedDictionaries.Insert(0, themeDict);
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to load theme: {ex.Message}\n\n{ex.StackTrace}", "Theme Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    public static void StartSystemThemeWatch()
    {
        if (_watching) return;
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
        _watching = true;

        if (System.Windows.Application.Current != null)
        {
            System.Windows.Application.Current.Exit += (_, __) => StopSystemThemeWatch();
        }
    }

    public static void StopSystemThemeWatch()
    {
        if (!_watching) return;
        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        _watching = false;
    }

    private static void OnUserPreferenceChanged(object? sender, UserPreferenceChangedEventArgs e)
    {
        if (!string.Equals(AppConfiguration.ThemeMode, "System", StringComparison.OrdinalIgnoreCase))
            return;

        // Re-apply theme on any user preference change (covers theme toggles)
        System.Windows.Application.Current?.Dispatcher.Invoke(() => ApplyTheme());
    }
}


