using System;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using AnimeQuoteWall.Core.Configuration;

namespace AnimeQuoteWall.GUI.Services;

/// <summary>
/// Provides a Windows notification-area (system tray) icon for AnimeQuoteWall using the built-in
/// WinForms <see cref="NotifyIcon"/>. The project already enables WinForms, so this needs no extra
/// NuGet dependency. Supports quick Open/Exit actions, double-click to restore, and optional
/// minimize-to-tray (driven by <see cref="AppConfiguration.MinimizeToTray"/>).
/// </summary>
public sealed class TrayIconService : IDisposable
{
    private readonly Window _window;
    private NotifyIcon? _notifyIcon;
    private ContextMenuStrip? _menu;
    private Icon? _icon;
    private bool _ownsIcon;
    private bool _disposed;

    /// <summary>Creates the tray service for the given main window.</summary>
    public TrayIconService(Window window)
    {
        _window = window ?? throw new ArgumentNullException(nameof(window));
    }

    /// <summary>Creates the tray icon and begins listening for window minimize events.</summary>
    public void Initialize()
    {
        _menu = new ContextMenuStrip();
        _menu.Items.Add("Open AnimeQuoteWall", null, (_, _) => ShowWindow());
        _menu.Items.Add("Hide to tray", null, (_, _) => _window.Hide());
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add("Exit", null, (_, _) => System.Windows.Application.Current.Shutdown());

        _icon = LoadAppIcon();
        _notifyIcon = new NotifyIcon
        {
            Text = "AnimeQuoteWall",
            Visible = true,
            ContextMenuStrip = _menu,
            Icon = _icon
        };
        _notifyIcon.DoubleClick += (_, _) => ShowWindow();

        _window.StateChanged += OnWindowStateChanged;
    }

    /// <summary>
    /// Shows a balloon tip in the tray (used for non-intrusive "wallpaper changed" feedback when
    /// the window is hidden). Safe to call before Initialize (it simply no-ops).
    /// </summary>
    public void ShowBalloon(string title, string message)
    {
        try
        {
            _notifyIcon?.ShowBalloonTip(3000, title, message, ToolTipIcon.Info);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"TrayIconService.ShowBalloon: {ex.Message}");
        }
    }

    private Icon LoadAppIcon()
    {
        try
        {
            var exe = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(exe))
            {
                var extracted = Icon.ExtractAssociatedIcon(exe);
                if (extracted != null)
                {
                    _ownsIcon = true; // we created this instance, so we may dispose it
                    return extracted;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"TrayIconService.LoadAppIcon: {ex.Message}");
        }
        // SystemIcons.Application is a shared, framework-owned handle; it must NOT be disposed.
        _ownsIcon = false;
        return SystemIcons.Application;
    }

    private void OnWindowStateChanged(object? sender, EventArgs e)
    {
        // Hide from the taskbar when minimized; the tray icon remains the way back in.
        if (_window.WindowState == WindowState.Minimized && AppConfiguration.MinimizeToTray)
            _window.Hide();
    }

    private void ShowWindow()
    {
        _window.Show();
        if (_window.WindowState == WindowState.Minimized)
            _window.WindowState = WindowState.Normal;
        _window.Activate();
        // Briefly toggle Topmost to bring the restored window to the foreground reliably.
        _window.Topmost = true;
        _window.Topmost = false;
    }

    /// <summary>Removes the tray icon and detaches event handlers.</summary>
    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        _window.StateChanged -= OnWindowStateChanged;
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }
        // NotifyIcon does not dispose an externally-assigned ContextMenuStrip, so do it here.
        _menu?.Dispose();
        _menu = null;
        // Only dispose the icon if we created it; never dispose the shared SystemIcons handle.
        if (_ownsIcon)
            _icon?.Dispose();
        _icon = null;
    }
}
