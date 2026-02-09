namespace AutoMerge.Infrastructure.Configuration;

public static class PlatformPaths
{
    public static string GetConfigDirectory()
    {
        if (OperatingSystem.IsWindows())
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "AutoMerge");
        }

        if (OperatingSystem.IsMacOS())
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "AutoMerge");
        }

        var fallback = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(fallback, "AutoMerge");
    }
}
