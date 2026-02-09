using AutoMerge.Logic.Events;
using AutoMerge.Logic.Services;
using AutoMerge.Logic.UseCases.AcceptResolution;
using AutoMerge.Logic.UseCases.AnalyzeConflict;
using AutoMerge.Logic.UseCases.CancelMerge;
using AutoMerge.Logic.UseCases.LoadAiModelOptions;
using AutoMerge.Logic.UseCases.LoadMergeSession;
using AutoMerge.Logic.UseCases.LoadPreferences;
using AutoMerge.Logic.UseCases.ProposeResolution;
using AutoMerge.Logic.UseCases.RefineResolution;
using AutoMerge.Logic.UseCases.SavePreferences;
using AutoMerge.Core.Abstractions;
using AutoMerge.Core.Services;
using AutoMerge.Infrastructure.AI;
using AutoMerge.Infrastructure.Configuration;
using AutoMerge.Infrastructure.Diff;
using AutoMerge.Infrastructure.Events;
using AutoMerge.Infrastructure.FileSystem;
using AutoMerge.UI.Services;
using AutoMerge.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace AutoMerge.App.Startup;

public static class ServiceRegistration
{
    public static IServiceCollection AddAutoMergeServices(this IServiceCollection services)
    {
        services.AddSingleton<IEventAggregator, EventAggregator>();

        services.AddSingleton<IFileService, FileService>();
        services.AddSingleton<IConflictParser, ConflictMarkerParser>();
        services.AddSingleton<IConfigurationService, ConfigurationService>();
        services.AddSingleton<IDiffCalculator, DiffPlexCalculator>();

        services.AddSingleton<ThemeService>();
        services.AddSingleton<DialogService>();
        services.AddSingleton<KeyboardShortcutService>();

        services.AddScoped<MergeSessionManager>();
        services.AddScoped<AutoSaveService>();

        services.AddTransient<LoadMergeSessionHandler>();
        services.AddTransient<AnalyzeConflictHandler>();
        services.AddTransient<ProposeResolutionHandler>();
        services.AddTransient<RefineResolutionHandler>();
        services.AddTransient<AcceptResolutionHandler>();
        services.AddTransient<CancelMergeHandler>();
        services.AddTransient<LoadAiModelOptionsHandler>();
        services.AddTransient<LoadPreferencesHandler>();
        services.AddTransient<SavePreferencesHandler>();

        services.AddTransient<DiffPaneViewModel>();
        services.AddTransient<MergedResultViewModel>();
        services.AddTransient<AiChatViewModel>();
        services.AddTransient<MergeInputDialogViewModel>();
        services.AddTransient<PreferencesViewModel>();
        services.AddTransient<MainWindowViewModel>();

        // Register the real Copilot AI service
        // Requires GitHub Copilot CLI to be installed and authenticated (run 'copilot auth login')
        services.AddSingleton<IAiService, CopilotAiService>();

        return services;
    }
}
