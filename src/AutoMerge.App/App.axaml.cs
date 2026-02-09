using AutoMerge.App.Startup;
using AutoMerge.App.Views;
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
    private bool _isDiffOnly;

    public App()
    {
        // Required for XAML loader
    }

    public void Configure(IServiceProvider services, MergeInput? mergeInput, bool isDiffOnly = false)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _mergeInput = mergeInput;
        _isDiffOnly = isDiffOnly;
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

            // Show splash screen immediately as the initial window
            StartupConsoleLogger.Log("Launching splash screen...");
            var splash = new SplashWindow();
            desktop.MainWindow = splash;
            splash.Show();

            // Kick off async initialization while splash is visible
            Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
            {
                try
                {
                    await InitializeWithSplashAsync(desktop, splash);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Startup error: {ex}");
                    StartupConsoleLogger.Log($"Startup error: {ex.Message}");

                    // Show main window in empty/error state
                    var mainWindow = new MainWindow(_services!);
                    var viewModel = _services!.GetRequiredService<MainWindowViewModel>();
                    mainWindow.DataContext = viewModel;
                    viewModel.ShowEmptyState();
                    TransitionToMainWindow(desktop, splash, mainWindow);
                }
            });
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async Task InitializeWithSplashAsync(
        IClassicDesktopStyleApplicationLifetime desktop,
        SplashWindow splash)
    {
        // Step 1: Create main window and view model (not yet shown)
        ReportProgress(splash, "Preparing main window...");
        var mainWindow = new MainWindow(_services!);
        var viewModel = _services!.GetRequiredService<MainWindowViewModel>();
        mainWindow.DataContext = viewModel;

        if (_mergeInput is not null)
        {
            // Step 2: Load the merge/diff session while splash is visible
            if (_isDiffOnly)
            {
                ReportProgress(splash, "Loading files for diff...");
                await viewModel.InitializeDiffAsync(_mergeInput, msg => ReportProgress(splash, msg));
                StartupConsoleLogger.Log("Diff loaded successfully.");
            }
            else
            {
                ReportProgress(splash, "Loading merge files...");
                await viewModel.InitializeAsync(_mergeInput, msg => ReportProgress(splash, msg));
                StartupConsoleLogger.Log("Merge session loaded successfully.");
            }
        }
        else
        {
            ReportProgress(splash, "Checking AI connection...");
            viewModel.ShowEmptyState();
            // Give the async AI status check a moment to start
            await Task.Delay(200);
            StartupConsoleLogger.Log("Empty state ready.");
        }

        // Step 3: Transition from splash to the main window
        ReportProgress(splash, "Ready.");
        // Brief pause so the user sees "Ready."
        await Task.Delay(200);

        TransitionToMainWindow(desktop, splash, mainWindow);
    }

    /// <summary>
    /// Closes the splash screen, sets the real main window, and shows it.
    /// </summary>
    private static void TransitionToMainWindow(
        IClassicDesktopStyleApplicationLifetime desktop,
        SplashWindow splash,
        MainWindow mainWindow)
    {
        StartupConsoleLogger.Log("Showing main window.");
        desktop.MainWindow = mainWindow;
        mainWindow.Show();
        splash.Close();
    }

    /// <summary>
    /// Logs to console and updates the splash screen status text.
    /// </summary>
    private static void ReportProgress(SplashWindow splash, string message)
    {
        StartupConsoleLogger.Log(message);
        splash.SetStatus(message);
    }
}
