using System.Globalization;
using Avalonia.Data.Converters;
using AutoMerge.Core.Models;

namespace AutoMerge.UI.Converters;

public sealed class SessionStateToStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is SessionState state ? state.ToString() : string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
