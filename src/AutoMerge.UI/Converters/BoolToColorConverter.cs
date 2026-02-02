using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace AutoMerge.UI.Converters;

/// <summary>
/// Converts a boolean to a color - green for true (AI available), red for false (AI unavailable).
/// </summary>
public sealed class BoolToColorConverter : IValueConverter
{
    public static readonly BoolToColorConverter Instance = new();

    private static readonly SolidColorBrush SuccessBrush = new(Color.Parse("#4CAF50"));
    private static readonly SolidColorBrush ErrorBrush = new(Color.Parse("#F44336"));

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? SuccessBrush : ErrorBrush;
        }
        return ErrorBrush;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
