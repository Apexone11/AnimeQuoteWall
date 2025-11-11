using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace AnimeQuoteWall.GUI.Converters;

/// <summary>
/// Converts a file path string to a BitmapImage, handling errors gracefully.
/// </summary>
public class ImagePathConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string path || string.IsNullOrWhiteSpace(path))
            return null;

        try
        {
            if (!File.Exists(path))
                return null;

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad; // Cache for performance
            bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache; // Always load fresh
            bitmap.DecodePixelWidth = 256; // Limit decode size for thumbnails (faster loading, less memory)
            bitmap.DecodePixelHeight = 144; // Maintain aspect ratio approximately
            bitmap.UriSource = new Uri(path, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            // Return null on any error (file doesn't exist, invalid format, etc.)
            return null;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

