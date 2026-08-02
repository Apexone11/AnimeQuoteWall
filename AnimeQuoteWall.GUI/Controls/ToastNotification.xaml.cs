using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace AnimeQuoteWall.GUI.Controls;

/// <summary>
/// A non-blocking toast notification control for displaying success, error, and info messages.
/// Automatically dismisses after a specified duration.
/// </summary>
public partial class ToastNotification : System.Windows.Controls.UserControl
{
    /// <summary>
    /// Duration in milliseconds before the toast automatically dismisses.
    /// </summary>
    private const int AutoDismissDuration = 4000;

    /// <summary>
    /// Event raised when the toast is dismissed.
    /// </summary>
    public event EventHandler? Dismissed;

    /// <summary>
    /// Initializes a new instance of the ToastNotification control.
    /// </summary>
    public ToastNotification()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Shows a toast notification with the specified message and type.
    /// </summary>
    /// <param name="message">The message to display</param>
    /// <param name="type">The type of notification (Success, Error, Info, Warning)</param>
    public void Show(string message, ToastType type = ToastType.Info)
    {
        MessageTextBlock.Text = message;

        IconTextBlock.Text = type switch
        {
            ToastType.Success => "OK",
            ToastType.Error => "X",
            ToastType.Warning => "!",
            _ => "i"
        };

        var brushKey = type switch
        {
            ToastType.Success => "SuccessColor",
            ToastType.Error => "DangerColor",
            ToastType.Warning => "WarningColor",
            _ => "PrimaryColor"
        };
        if (System.Windows.Application.Current?.TryFindResource(brushKey) is System.Windows.Media.Brush themedBrush)
            IconTextBlock.Foreground = themedBrush;

        var timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(AutoDismissDuration)
        };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            Dismiss();
        };
        timer.Start();
    }

    /// <summary>
    /// Dismisses the toast notification with a fade-out animation.
    /// </summary>
    public void Dismiss()
    {
        var fadeOut = new DoubleAnimation
        {
            From = 1.0,
            To = 0.0,
            Duration = new Duration(TimeSpan.FromMilliseconds(300))
        };

        fadeOut.Completed += (s, e) =>
        {
            Dismissed?.Invoke(this, EventArgs.Empty);
        };

        BeginAnimation(OpacityProperty, fadeOut);
    }

    /// <summary>
    /// Handles the close button click event.
    /// </summary>
    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Dismiss();
    }
}

/// <summary>
/// Enumeration of toast notification types.
/// </summary>
public enum ToastType
{
    /// <summary>
    /// Information message (default).
    /// </summary>
    Info,

    /// <summary>
    /// Success message.
    /// </summary>
    Success,

    /// <summary>
    /// Error message.
    /// </summary>
    Error,

    /// <summary>
    /// Warning message.
    /// </summary>
    Warning
}

