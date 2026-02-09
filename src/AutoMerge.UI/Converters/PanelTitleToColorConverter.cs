using Avalonia.Data.Converters;
using Avalonia.Media;
using AutoMerge.UI.Localization;
using System;
using System.Globalization;

namespace AutoMerge.UI.Converters;

/// <summary>
/// Converts a panel title (Base, Local, Remote) to its corresponding header background color.
/// </summary>
public sealed class PanelTitleToColorConverter : IValueConverter
{
    // Panel-specific gradient-like colors for visual distinction
    private static readonly SolidColorBrush BaseBrush = new(Color.Parse("#1E3A5F"));     // Deep blue
    private static readonly SolidColorBrush LocalBrush = new(Color.Parse("#1E5631"));    // Deep green
    private static readonly SolidColorBrush RemoteBrush = new(Color.Parse("#5F1E3A"));   // Deep magenta
    private static readonly SolidColorBrush MergedBrush = new(Color.Parse("#3A1E5F"));   // Deep purple
    private static readonly SolidColorBrush DefaultBrush = new(Color.Parse("#2D2D32")); // Neutral

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string title)
        {
            if (string.Equals(title, UIStrings.PanelTitleBase, StringComparison.CurrentCultureIgnoreCase))
            {
                return BaseBrush;
            }
            if (string.Equals(title, UIStrings.PanelTitleLocal, StringComparison.CurrentCultureIgnoreCase))
            {
                return LocalBrush;
            }
            if (string.Equals(title, UIStrings.PanelTitleRemote, StringComparison.CurrentCultureIgnoreCase))
            {
                return RemoteBrush;
            }
            if (string.Equals(title, UIStrings.PanelTitleMerged, StringComparison.CurrentCultureIgnoreCase) ||
                string.Equals(title, UIStrings.PanelTitleMergedResult, StringComparison.CurrentCultureIgnoreCase))
            {
                return MergedBrush;
            }
        }
        return DefaultBrush;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
