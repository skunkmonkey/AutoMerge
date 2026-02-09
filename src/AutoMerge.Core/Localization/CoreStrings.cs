using System.Globalization;
using System.Resources;
using AutoMerge.Resources;

namespace AutoMerge.Core.Localization;

internal static class CoreStrings
{
    private static readonly ResourceManager ResourceManager = new("AutoMerge.Resources.Resources.Core.Strings", typeof(ResourceAssembly).Assembly);

    public static string PathMustNotBeNullOrEmpty => GetString(nameof(PathMustNotBeNullOrEmpty));

    private static string GetString(string name)
    {
        return ResourceManager.GetString(name, CultureInfo.CurrentUICulture) ?? name;
    }
}
