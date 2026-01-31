using System.Globalization;
using Avalonia.Data.Converters;

namespace AutoMerge.UI.Converters;

public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool flag && flag;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool flag && flag;
    }
}
