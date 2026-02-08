using Avalonia.Data.Converters;
using System;
using System.Globalization;
using AutoMerge.UI.Localization;

namespace AutoMerge.UI.Converters;

/// <summary>
/// Converts a boolean AI availability status to a human-readable string.
/// </summary>
public sealed class BoolToAiStatusConverter : IValueConverter
{
    public static readonly BoolToAiStatusConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isAvailable)
        {
            return isAvailable ? UIStrings.AiConnected : UIStrings.AiDisconnected;
        }
        return UIStrings.AiStatusUnknown;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
