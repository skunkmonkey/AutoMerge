using Avalonia.Data.Converters;
using Avalonia.Layout;
using AutoMerge.Core.Models;
using System;
using System.Globalization;

namespace AutoMerge.UI.Converters;

/// <summary>
/// Converts a ChatRole to horizontal alignment for message bubbles.
/// User messages align right, Assistant messages align left.
/// </summary>
public sealed class ChatRoleToAlignmentConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ChatRole role)
        {
            return role == ChatRole.User ? HorizontalAlignment.Right : HorizontalAlignment.Left;
        }
        return HorizontalAlignment.Left;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
