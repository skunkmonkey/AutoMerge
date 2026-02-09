using System;

namespace AutoMerge.App.Startup;

/// <summary>
/// Writes timestamped startup progress messages to the console so users can
/// follow loading progress when watching the terminal that launched AutoMerge.
/// </summary>
internal static class StartupConsoleLogger
{
    /// <summary>
    /// Logs a startup progress message to the console.
    /// </summary>
    public static void Log(string message)
    {
        Console.WriteLine($"[AutoMerge] {message}");
    }
}
