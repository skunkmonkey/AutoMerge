using System.Globalization;
using System.Resources;
using AutoMerge.Resources;

namespace AutoMerge.Logic.Localization;

internal static class LogicStrings
{
    private static readonly ResourceManager ResourceManager = new("AutoMerge.Resources.Resources.Logic.Strings", typeof(ResourceAssembly).Assembly);

    public static string NoActiveSession => GetString(nameof(NoActiveSession));
    public static string ResolvedContentHasConflictMarkers => GetString(nameof(ResolvedContentHasConflictMarkers));
    public static string ConflictFileNotLoaded => GetString(nameof(ConflictFileNotLoaded));
    public static string MergeInputRequired => GetString(nameof(MergeInputRequired));
    public static string MissingRequiredFileFormat => GetString(nameof(MissingRequiredFileFormat));
    public static string BinaryFileNotSupportedFormat => GetString(nameof(BinaryFileNotSupportedFormat));
    public static string BusyResearchLocalTitle => GetString(nameof(BusyResearchLocalTitle));
    public static string BusyResearchLocalMessage => GetString(nameof(BusyResearchLocalMessage));
    public static string BusyResearchRemoteTitle => GetString(nameof(BusyResearchRemoteTitle));
    public static string BusyResearchRemoteMessage => GetString(nameof(BusyResearchRemoteMessage));
    public static string BusyResearchBothTitle => GetString(nameof(BusyResearchBothTitle));
    public static string BusyResearchBothMessage => GetString(nameof(BusyResearchBothMessage));
    public static string BusyResolvingTitle => GetString(nameof(BusyResolvingTitle));
    public static string BusyResolvingMessage => GetString(nameof(BusyResolvingMessage));

    private static string GetString(string name)
    {
        return ResourceManager.GetString(name, CultureInfo.CurrentUICulture) ?? name;
    }
}
