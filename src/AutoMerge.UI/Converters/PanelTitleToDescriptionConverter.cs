using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace AutoMerge.UI.Converters;

/// <summary>
/// Converts a panel title (Base, Local, Remote) to a description subtitle.
/// </summary>
public sealed class PanelTitleToDescriptionConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string title)
        {
            return title.ToLowerInvariant() switch
            {
                "base" => "Common ancestor",
                "local" => "Your changes (ours)",
                "remote" => "Incoming changes (theirs)",
                "merged" or "merged result" => "Final resolved output",
                _ => string.Empty
            };
        }
        return string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
