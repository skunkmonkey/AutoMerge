using System.Globalization;
using System.Resources;
using AutoMerge.Resources;

namespace AutoMerge.App.Localization;

internal static class AppStrings
{
    private static readonly ResourceManager ResourceManager = new("AutoMerge.Resources.Resources.App.Strings", typeof(ResourceAssembly).Assembly);

    public static string CliHelpText => GetString(nameof(CliHelpText));

    public static string CliVersionFormat => GetString(nameof(CliVersionFormat));

    private static string GetString(string name)
    {
        return ResourceManager.GetString(name, CultureInfo.CurrentUICulture) ?? name;
    }
}
