using Avalonia.Data.Converters;
using Avalonia.Media;
using AutoMerge.Core.Models;
using System;
using System.Globalization;

namespace AutoMerge.UI.Converters;

/// <summary>
/// Converts a ChatRole to a background color for message bubbles.
/// User messages have a blue tint, Assistant messages have a neutral background.
/// </summary>
public sealed class ChatRoleToBackgroundConverter : IValueConverter
{
    private static readonly SolidColorBrush UserBrush = new(Color.Parse("#1E3A5F"));       // Blue tint for user
    private static readonly SolidColorBrush AssistantBrush = new(Color.Parse("#2D2D32")); // Neutral for AI

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ChatRole role)
        {
            return role == ChatRole.User ? UserBrush : AssistantBrush;
        }
        return AssistantBrush;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
