using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace AnimeQuoteWall.GUI.Services;

/// <summary>
/// Registers system-wide global hotkeys via the Win32 RegisterHotKey API and raises events when
/// they are pressed. No external dependency (the app already targets Windows). Currently registers
/// a single show/hide toggle hotkey: Ctrl+Alt+Q.
/// </summary>
public sealed class GlobalHotkeyService : IDisposable
{
    private const int WM_HOTKEY = 0x0312;
    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_NOREPEAT = 0x4000;
    private const uint VK_Q = 0x51;
    private const int HotkeyIdToggle = 0xA001;

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private readonly Window _window;
    private HwndSource? _source;
    private IntPtr _hwnd = IntPtr.Zero;
    private bool _registered;
    private bool _disposed;

    /// <summary>Raised when the show/hide toggle hotkey (Ctrl+Alt+Q) is pressed.</summary>
    public event EventHandler? ToggleWindowRequested;

    /// <summary>Creates the hotkey service for the given main window.</summary>
    public GlobalHotkeyService(Window window)
    {
        _window = window ?? throw new ArgumentNullException(nameof(window));
    }

    /// <summary>Hooks the window message loop and registers the hotkeys. Call after the window exists.</summary>
    public void Initialize()
    {
        var helper = new WindowInteropHelper(_window);
        _hwnd = helper.EnsureHandle();
        _source = HwndSource.FromHwnd(_hwnd);
        _source?.AddHook(WndProc);

        // MOD_NOREPEAT prevents autorepeat from flooding while the keys are held.
        _registered = RegisterHotKey(_hwnd, HotkeyIdToggle, MOD_CONTROL | MOD_ALT | MOD_NOREPEAT, VK_Q);
        if (!_registered)
            System.Diagnostics.Debug.WriteLine("GlobalHotkeyService: RegisterHotKey(Ctrl+Alt+Q) failed (already in use?).");
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == HotkeyIdToggle)
        {
            ToggleWindowRequested?.Invoke(this, EventArgs.Empty);
            handled = true;
        }
        return IntPtr.Zero;
    }

    /// <summary>Unregisters the hotkeys and removes the message hook.</summary>
    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        if (_source != null)
        {
            _source.RemoveHook(WndProc);
            _source = null;
        }
        if (_registered && _hwnd != IntPtr.Zero)
        {
            UnregisterHotKey(_hwnd, HotkeyIdToggle);
            _registered = false;
        }
    }
}
