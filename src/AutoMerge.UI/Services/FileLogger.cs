using System.IO;

namespace AutoMerge.UI.Services;

/// <summary>
/// Simple file logger for debugging UI binding issues.
/// </summary>
public static class FileLogger
{
    // Hardcoded absolute path for reliable logging
    private static readonly string LogPath = @"D:\git\AutoMerge\Specs\log.txt";

    private static readonly object Lock = new();

    public static void Log(string message)
    {
        try
        {
            lock (Lock)
            {
                var fullPath = Path.GetFullPath(LogPath);
                var dir = Path.GetDirectoryName(fullPath);
                if (dir != null && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                File.AppendAllText(fullPath, $"[{timestamp}] {message}{Environment.NewLine}");
            }
        }
        catch
        {
            // Ignore logging failures
        }
    }

    public static void Clear()
    {
        try
        {
            lock (Lock)
            {
                var fullPath = Path.GetFullPath(LogPath);
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
            }
        }
        catch
        {
            // Ignore
        }
    }
}
