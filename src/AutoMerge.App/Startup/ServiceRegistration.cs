using AutoMerge.Application.Events;
using AutoMerge.Application.Services;
using AutoMerge.Application.UseCases.AcceptResolution;
using AutoMerge.Application.UseCases.AnalyzeConflict;
using AutoMerge.Application.UseCases.CancelMerge;
using AutoMerge.Application.UseCases.LoadAiModelOptions;
using AutoMerge.Application.UseCases.LoadMergeSession;
using AutoMerge.Application.UseCases.LoadPreferences;
using AutoMerge.Application.UseCases.ProposeResolution;
using AutoMerge.Application.UseCases.RefineResolution;
using AutoMerge.Application.UseCases.SavePreferences;
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
