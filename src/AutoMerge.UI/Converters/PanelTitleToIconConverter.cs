using Avalonia.Data.Converters;
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
            return title.ToLowerInvariant() switch
            {
                "base" => "📋",      // Clipboard - common ancestor
                "local" => "📝",     // Memo - your local changes
                "remote" => "📥",    // Inbox - incoming changes
                "merged" or "merged result" => "✨",  // Sparkles - the result
                _ => "📄"            // Generic document
            };
        }
        return "📄";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
