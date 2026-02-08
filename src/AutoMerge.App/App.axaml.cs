using AutoMerge.Logic.Events;
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
    private IServiceProvider? _services;
    private MergeInput? _mergeInput;

    public App()
    {
        // Required for XAML loader
    }

    public void Configure(IServiceProvider services, MergeInput? mergeInput)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _mergeInput = mergeInput;
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (_services is null)
        {
            throw new InvalidOperationException("App.Configure() must be called before framework initialization.");
        }

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

            if (_mergeInput is not null)
            {
                // Use Dispatcher to ensure initialization runs on UI thread and errors are handled
                Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
                {
                    try
                    {
                        await viewModel.InitializeAsync(_mergeInput);
                    }
                    catch (Exception ex)
                    {
                        viewModel.ShowEmptyState();
                        // Log or display error - the ViewModel's ErrorMessage property should handle this
                        System.Diagnostics.Debug.WriteLine($"Initialization error: {ex}");
                    }
                });
            }
            else
            {
                viewModel.ShowEmptyState();
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}
