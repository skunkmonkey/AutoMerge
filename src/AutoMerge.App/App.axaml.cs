using AutoMerge.Application.Events;
using AutoMerge.Core.Models;
using AutoMerge.UI.ViewModels;
using AutoMerge.UI.Views;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;

namespace AutoMerge.App;

public sealed class App : Avalonia.Application
{
    private readonly IServiceProvider _services;
    private readonly MergeInput _mergeInput;

    public App(IServiceProvider services, MergeInput mergeInput)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _mergeInput = mergeInput ?? throw new ArgumentNullException(nameof(mergeInput));
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var eventAggregator = _services.GetRequiredService<IEventAggregator>();
            eventAggregator.Subscribe<SessionCompletedEvent>(evt =>
            {
                var exitCode = evt.Success ? 0 : 1;
                desktop.Shutdown(exitCode);
            });

            var mainWindow = new MainWindow(_services);
            var viewModel = _services.GetRequiredService<MainWindowViewModel>();
            mainWindow.DataContext = viewModel;
            desktop.MainWindow = mainWindow;

            _ = viewModel.InitializeAsync(_mergeInput);
        }

        base.OnFrameworkInitializationCompleted();
    }
}
