using Avalonia.Data.Converters;
using AutoMerge.UI.Localization;
using System;
using System.Globalization;

namespace AutoMerge.UI.Converters;

/// <summary>
/// Converts a panel title (Base, Local, Remote) to an appropriate emoji icon.
/// </summary>
public sealed class PanelTitleToIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string title)
        {
            if (string.Equals(title, UIStrings.PanelTitleBase, StringComparison.CurrentCultureIgnoreCase))
            {
                return "📋"; // Clipboard - common ancestor
            }
            if (string.Equals(title, UIStrings.PanelTitleLocal, StringComparison.CurrentCultureIgnoreCase))
            {
                return "📝"; // Memo - your local changes
            }
            if (string.Equals(title, UIStrings.PanelTitleRemote, StringComparison.CurrentCultureIgnoreCase))
            {
                return "📥"; // Inbox - incoming changes
            }
            if (string.Equals(title, UIStrings.PanelTitleMerged, StringComparison.CurrentCultureIgnoreCase) ||
                string.Equals(title, UIStrings.PanelTitleMergedResult, StringComparison.CurrentCultureIgnoreCase))
            {
                return "✨"; // Sparkles - the result
            }
        }
        return "📄"; // Generic document
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
