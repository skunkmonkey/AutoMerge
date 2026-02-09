using Avalonia.Data.Converters;
using AutoMerge.UI.Localization;
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
            if (string.Equals(title, UIStrings.PanelTitleBase, StringComparison.CurrentCultureIgnoreCase))
            {
                return UIStrings.PanelDescriptionBase;
            }
            if (string.Equals(title, UIStrings.PanelTitleLocal, StringComparison.CurrentCultureIgnoreCase))
            {
                return UIStrings.PanelDescriptionLocal;
            }
            if (string.Equals(title, UIStrings.PanelTitleRemote, StringComparison.CurrentCultureIgnoreCase))
            {
                return UIStrings.PanelDescriptionRemote;
            }
            if (string.Equals(title, UIStrings.PanelTitleMerged, StringComparison.CurrentCultureIgnoreCase) ||
                string.Equals(title, UIStrings.PanelTitleMergedResult, StringComparison.CurrentCultureIgnoreCase))
            {
                return UIStrings.PanelDescriptionMerged;
            }
        }
        return string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
