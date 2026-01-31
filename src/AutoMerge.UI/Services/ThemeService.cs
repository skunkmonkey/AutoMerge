using Avalonia;
using Avalonia.Themes.Fluent;
using Avalonia.Styling;
using AutoMerge.Core.Models;

namespace AutoMerge.UI.Services;

public sealed class ThemeService
{
    public void ApplyTheme(ThemeVariant variant)
    {
        if (Avalonia.Application.Current is null)
        {
            return;
        }

        EnsureFluentTheme();
        Avalonia.Application.Current.RequestedThemeVariant = variant;
    }

    public void ApplySystemTheme()
    {
        ApplyTheme(ThemeVariant.Default);
    }

    public void ApplyTheme(Theme theme)
    {
        switch (theme)
        {
            case Theme.Light:
                ApplyTheme(ThemeVariant.Light);
                break;
            case Theme.Dark:
                ApplyTheme(ThemeVariant.Dark);
                break;
            default:
                ApplySystemTheme();
                break;
        }
    }

    public ThemeVariant GetSystemThemeVariant()
    {
        if (Avalonia.Application.Current?.PlatformSettings is { } platformSettings)
        {
            var platformVariant = platformSettings.GetColorValues().ThemeVariant;

            return platformVariant switch
            {
                Avalonia.Platform.PlatformThemeVariant.Light => ThemeVariant.Light,
                Avalonia.Platform.PlatformThemeVariant.Dark => ThemeVariant.Dark,
                _ => ThemeVariant.Default
            };
        }

        return ThemeVariant.Default;
    }

    private static void EnsureFluentTheme()
    {
        if (Avalonia.Application.Current is null)
        {
            return;
        }

        foreach (var style in Avalonia.Application.Current.Styles)
        {
            if (style is FluentTheme)
            {
                return;
            }
        }

        Avalonia.Application.Current.Styles.Insert(0, new FluentTheme());
    }
}
