using System;
using System.Linq;
using System.Windows;
using AnimeQuoteWall.GUI.Controls;
using WpfPanel = System.Windows.Controls.Panel;
using WpfGrid = System.Windows.Controls.Grid;
using WpfHorizontalAlignment = System.Windows.HorizontalAlignment;
using WpfVerticalAlignment = System.Windows.VerticalAlignment;

namespace AnimeQuoteWall.GUI.Services;

/// <summary>
/// Process-wide non-blocking notification service. Pages and dialogs call
/// <see cref="ShowSuccess"/>, <see cref="ShowError"/>, <see cref="ShowWarning"/>, or
/// <see cref="ShowInfo"/> from any thread; the service marshals to the UI thread and
/// stacks toasts at the bottom-right of the active main window.
/// </summary>
public static class ToastService
{
    public static void ShowSuccess(string message) => Show(message, ToastType.Success);
    public static void ShowError(string message) => Show(message, ToastType.Error);
    public static void ShowWarning(string message) => Show(message, ToastType.Warning);
    public static void ShowInfo(string message) => Show(message, ToastType.Info);

    public static void Show(string message, ToastType type)
    {
        var app = System.Windows.Application.Current;
        if (app is null) return;

        if (!app.Dispatcher.CheckAccess())
        {
            app.Dispatcher.Invoke(() => Show(message, type));
            return;
        }

        try
        {
            var window = app.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
                         ?? app.MainWindow
                         ?? app.Windows.OfType<Window>().FirstOrDefault();
            if (window is null) return;

            if (EnsureHost(window) is not WpfPanel host) return;

            var existingCount = host.Children.OfType<ToastNotification>().Count();
            var toast = new ToastNotification
            {
                HorizontalAlignment = WpfHorizontalAlignment.Right,
                VerticalAlignment = WpfVerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, 24, 24 + (72 * existingCount))
            };
            toast.Dismissed += (_, _) => host.Children.Remove(toast);
            host.Children.Add(toast);
            WpfPanel.SetZIndex(toast, 9000);
            toast.Show(message, type);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ToastService.Show: {ex.Message}");
        }
    }

    private static WpfPanel? EnsureHost(Window window)
    {
        if (window.Content is not WpfPanel rootPanel) return null;

        var existing = rootPanel.Children.OfType<WpfGrid>().FirstOrDefault(g => g.Name == "ToastHost");
        if (existing != null) return existing;

        var host = new WpfGrid { Name = "ToastHost", IsHitTestVisible = true };
        WpfPanel.SetZIndex(host, 9000);
        rootPanel.Children.Add(host);
        return host;
    }
}
