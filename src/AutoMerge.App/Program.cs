using AutoMerge.App.Startup;
using AutoMerge.Core.Models;
using Avalonia;
using Microsoft.Extensions.DependencyInjection;

namespace AutoMerge.App;

internal static class Program
{
    public static int Main(string[] args)
    {
        StartupConsoleLogger.Log("Parsing command-line arguments...");
        var parseResult = CliParser.Parse(args);
        if (parseResult.ShouldExit)
        {
            return parseResult.ExitCode;
        }

        var mergeInput = parseResult.MergeInput;
        var isDiffOnly = parseResult.IsDiffOnly;

        StartupConsoleLogger.Log("Configuring services...");
        var services = new ServiceCollection()
            .AddAutoMergeServices()
            .BuildServiceProvider();
        StartupConsoleLogger.Log("Services configured.");

        return BuildAvaloniaApp(services, mergeInput, isDiffOnly)
            .StartWithClassicDesktopLifetime(args);
    }

    private static AppBuilder BuildAvaloniaApp(IServiceProvider services, MergeInput? mergeInput, bool isDiffOnly)
    {
        return AppBuilder
            .Configure(() =>
            {
                var app = new App();
                app.Configure(services, mergeInput, isDiffOnly);
                return app;
            })
            .UsePlatformDetect()
            .LogToTrace();
    }
}
