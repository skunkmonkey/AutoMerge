using Avalonia;
using Avalonia.Styling;
using AutoMerge.Core.Models;

namespace AutoMerge.UI.Services;

public sealed class ThemeService
{
    public void ApplySystemTheme()
    {
        ApplyTheme(Theme.System);
    }

    public void ApplyTheme(Theme theme)
    {
        if (Avalonia.Application.Current is null)
        {
            return;
        }

        Avalonia.Application.Current.RequestedThemeVariant = theme switch
        {
            Theme.Dark => ThemeVariant.Dark,
            Theme.Light => ThemeVariant.Light,
            _ => ThemeVariant.Default
        };
    }
}
