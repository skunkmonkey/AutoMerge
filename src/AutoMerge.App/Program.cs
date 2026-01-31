using AutoMerge.App.Startup;
using AutoMerge.Core.Models;
using Avalonia;
using Microsoft.Extensions.DependencyInjection;

namespace AutoMerge.App;

internal static class Program
{
    public static int Main(string[] args)
    {
        var parseResult = CliParser.Parse(args);
        if (parseResult.ShouldExit)
        {
            return parseResult.ExitCode;
        }

        var mergeInput = parseResult.MergeInput ?? throw new InvalidOperationException("Merge input is required.");
        var services = new ServiceCollection()
            .AddAutoMergeServices()
            .BuildServiceProvider();

        return BuildAvaloniaApp(services, mergeInput)
            .StartWithClassicDesktopLifetime(args);
    }

    private static AppBuilder BuildAvaloniaApp(IServiceProvider services, MergeInput mergeInput)
    {
        return AppBuilder
            .Configure(() => new App(services, mergeInput))
            .UsePlatformDetect()
            .LogToTrace();
    }
}
