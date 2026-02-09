using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace AutoMerge.UI.Converters;

/// <summary>
/// Converts a boolean AI availability to a background color for the AI status card.
/// Connected = subtle green tint, Disconnected = subtle amber/warning tint.
/// </summary>
public sealed class BoolToSetupCardColorConverter : IValueConverter
{
    public static readonly BoolToSetupCardColorConverter Instance = new();

    private static readonly Color ConnectedColor = Color.Parse("#104CAF50");  // Very subtle green
    private static readonly Color DisconnectedColor = Color.Parse("#18FF9800"); // Very subtle amber

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isAvailable)
        {
            return isAvailable ? ConnectedColor : DisconnectedColor;
        }
        return DisconnectedColor;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
