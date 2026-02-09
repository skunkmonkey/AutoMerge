using System;
using System.Globalization;
using Avalonia.Data.Converters;
using AutoMerge.Core.Models;
using AutoMerge.UI.Localization;

namespace AutoMerge.UI.Converters;

public sealed class SessionStateToStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not SessionState state)
        {
            return string.Empty;
        }

        return state switch
        {
            SessionState.Created => UIStrings.SessionStateCreated,
            SessionState.Loading => UIStrings.SessionStateLoading,
            SessionState.Ready => UIStrings.SessionStateReady,
            SessionState.Analyzing => UIStrings.SessionStateAnalyzing,
            SessionState.ResolutionProposed => UIStrings.SessionStateResolutionProposed,
            SessionState.Refining => UIStrings.SessionStateRefining,
            SessionState.UserEditing => UIStrings.SessionStateUserEditing,
            SessionState.Validated => UIStrings.SessionStateValidated,
            SessionState.Saved => UIStrings.SessionStateSaved,
            SessionState.Cancelled => UIStrings.SessionStateCancelled,
            _ => string.Empty
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
