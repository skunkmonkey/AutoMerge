using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using AutoMerge.Core.Models;

namespace AutoMerge.UI.Converters;

public sealed class LineChangeTypeToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is LineChangeType changeType
            ? changeType switch
            {
                LineChangeType.Added => Brushes.LightGreen,
                LineChangeType.Removed => Brushes.IndianRed,
                LineChangeType.Modified => Brushes.Khaki,
                _ => Brushes.Transparent
            }
            : Brushes.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
