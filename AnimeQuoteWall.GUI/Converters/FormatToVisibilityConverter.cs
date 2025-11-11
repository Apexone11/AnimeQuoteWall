using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace AnimeQuoteWall.GUI.Converters;

/// <summary>
/// Converts format string (MP4, WEBM, MOV) to Visibility for video play icon.
/// </summary>
public class FormatToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string format)
        {
            var videoFormats = new[] { "MP4", "WEBM", "MOV" };
            return videoFormats.Contains(format.ToUpperInvariant()) ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

