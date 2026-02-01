using System.ComponentModel;
using AutoMerge.Application.UseCases.AcceptResolution;
using AutoMerge.Application.UseCases.AnalyzeConflict;
using AutoMerge.Application.UseCases.CancelMerge;
using AutoMerge.Application.UseCases.LoadMergeSession;
using AutoMerge.Application.UseCases.ProposeResolution;
using AutoMerge.Core.Abstractions;
using AutoMerge.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AutoMerge.UI.ViewModels;

public sealed partial class MainWindowViewModel : ViewModelBase
{
    private readonly LoadMergeSessionHandler _loadHandler;
    private readonly AnalyzeConflictHandler _analyzeHandler;
    private readonly ProposeResolutionHandler _proposeHandler;
    private readonly AcceptResolutionHandler _acceptHandler;
    private readonly CancelMergeHandler _cancelHandler;
    private readonly IFileService _fileService;
    private readonly IDiffCalculator _diffCalculator;
    private readonly IAiService _aiService;
    private MergeInput? _lastInput;

    public MainWindowViewModel(
        LoadMergeSessionHandler loadHandler,
        AnalyzeConflictHandler analyzeHandler,
        ProposeResolutionHandler proposeHandler,
        AcceptResolutionHandler acceptHandler,
        CancelMergeHandler cancelHandler,
        IFileService fileService,
        IDiffCalculator diffCalculator,
        IAiService aiService,
        MergedResultViewModel mergedResultViewModel,
        AiChatViewModel aiChatViewModel)
    {
        _loadHandler = loadHandler;
        _analyzeHandler = analyzeHandler;
        _proposeHandler = proposeHandler;
        _acceptHandler = acceptHandler;
        _cancelHandler = cancelHandler;
        _fileService = fileService;
        _diffCalculator = diffCalculator;
        _aiService = aiService;

        BasePaneViewModel = new DiffPaneViewModel { Title = "Base", IsReadOnly = true };
        LocalPaneViewModel = new DiffPaneViewModel { Title = "Local", IsReadOnly = true };
        RemotePaneViewModel = new DiffPaneViewModel { Title = "Remote", IsReadOnly = true };
        MergedResultViewModel = mergedResultViewModel;
        AiChatViewModel = aiChatViewModel;

        AnalyzeCommand = new AsyncRelayCommand(AnalyzeAsync, () => !IsAiBusy && IsAiAvailable && IsSessionLoaded);
        GetAiHelpCommand = new AsyncRelayCommand(ProposeResolutionAsync, () => !IsAiBusy && IsAiAvailable && IsSessionLoaded);
        AcceptCommand = new AsyncRelayCommand(AcceptAsync, () => CanAccept);
        CancelCommand = new RelayCommand(Cancel, () => IsSessionLoaded);
        OpenPreferencesCommand = new RelayCommand(() => { });
        ReconnectAiCommand = new AsyncRelayCommand(CheckAiStatusAsync, () => !IsAiBusy);
        RetryLoadCommand = new AsyncRelayCommand(RetryLoadAsync, () => !IsLoading && _lastInput is not null);

        MergedResultViewModel.PropertyChanged += OnMergedResultChanged;
    }

    public DiffPaneViewModel BasePaneViewModel { get; }
    public DiffPaneViewModel LocalPaneViewModel { get; }
    public DiffPaneViewModel RemotePaneViewModel { get; }
    public MergedResultViewModel MergedResultViewModel { get; }
    public AiChatViewModel AiChatViewModel { get; }

    [ObservableProperty]
    private SessionState _state = SessionState.Created;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isSessionLoaded;

    [ObservableProperty]
    private bool _isAiBusy;

    [ObservableProperty]
    private bool _isAiAvailable = true;

    [ObservableProperty]
    private string? _aiStatusMessage;

    [ObservableProperty]
    private bool _canAccept;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _hasError;

    public IAsyncRelayCommand AnalyzeCommand { get; }
    public IAsyncRelayCommand GetAiHelpCommand { get; }
    public IAsyncRelayCommand AcceptCommand { get; }
    public IRelayCommand CancelCommand { get; }
    public IRelayCommand OpenPreferencesCommand { get; }
    public IAsyncRelayCommand ReconnectAiCommand { get; }
    public IAsyncRelayCommand RetryLoadCommand { get; }

    public async Task InitializeAsync(MergeInput input)
    {
        _lastInput = input;
        IsSessionLoaded = false;
        IsLoading = true;
        ErrorMessage = null;
        HasError = false;
        ClearContent();

        try
        {
            // Check AI status (can run on background thread)
            await CheckAiStatusAsync();

            // Load the merge session
            var result = await _loadHandler.ExecuteAsync(new LoadMergeSessionCommand(input));
            if (!result.Success || result.Session is null)
            {
                ErrorMessage = result.ErrorMessage ?? "Failed to load merge session.";
                IsLoading = false;
                IsSessionLoaded = false;
                return;
            }

            State = result.Session.State;

            // Read all files
            var baseFile = await _fileService.ReadAsync(input.BasePath, CancellationToken.None);
            var localFile = await _fileService.ReadAsync(input.LocalPath, CancellationToken.None);
            var remoteFile = await _fileService.ReadAsync(input.RemotePath, CancellationToken.None);

            // Update UI on the UI thread via property setters (CommunityToolkit.Mvvm handles this)
            BasePaneViewModel.SetContent(baseFile.Content, Array.Empty<LineChange>());
            LocalPaneViewModel.SetContent(localFile.Content, _diffCalculator.CalculateDiff(baseFile.Content, localFile.Content));
            RemotePaneViewModel.SetContent(remoteFile.Content, _diffCalculator.CalculateDiff(baseFile.Content, remoteFile.Content));

            MergedResultViewModel.SetSourceContents(baseFile.Content, localFile.Content, remoteFile.Content, result.Session.CurrentMergedContent);
            UpdateCanAccept();
            IsLoading = false;
            IsSessionLoaded = true;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error loading files: {ex.Message}";
            IsLoading = false;
            IsSessionLoaded = false;
        }
    }

    public void ShowEmptyState()
    {
        _lastInput = null;
        State = SessionState.Created;
        ErrorMessage = null;
        IsLoading = false;
        IsSessionLoaded = false;
        ClearContent();
        UpdateCanAccept();
    }

    private async Task AnalyzeAsync()
    {
        IsAiBusy = true;
        try
        {
            var result = await _analyzeHandler.ExecuteAsync(new AnalyzeConflictCommand());
            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Analysis failed: {ex.Message}";
        }
        finally
        {
            IsAiBusy = false;
            UpdateCanAccept();
        }
    }

    private async Task ProposeResolutionAsync()
    {
        IsAiBusy = true;
        try
        {
            var result = await _proposeHandler.ExecuteAsync(new ProposeResolutionCommand());
            if (result.Success && result.Resolution is not null)
            {
                MergedResultViewModel.Content = result.Resolution.ResolvedContent;
            }
            else if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"AI resolution failed: {ex.Message}";
        }
        finally
        {
            IsAiBusy = false;
            UpdateCanAccept();
        }
    }

    private async Task AcceptAsync()
    {
        try
        {
            var result = await _acceptHandler.ExecuteAsync(new AcceptResolutionCommand(MergedResultViewModel.Content));
            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Accept failed: {ex.Message}";
        }
    }

    private void Cancel()
    {
        _cancelHandler.Execute();
    }

    private void OnMergedResultChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MergedResultViewModel.HasConflictMarkers) ||
            e.PropertyName == nameof(MergedResultViewModel.IsDirty))
        {
            UpdateCanAccept();
        }
    }

    private void UpdateCanAccept()
    {
        CanAccept = IsSessionLoaded && !IsAiBusy && !MergedResultViewModel.HasConflictMarkers;
        AcceptCommand.NotifyCanExecuteChanged();
        AnalyzeCommand.NotifyCanExecuteChanged();
        GetAiHelpCommand.NotifyCanExecuteChanged();
        ReconnectAiCommand.NotifyCanExecuteChanged();
        RetryLoadCommand.NotifyCanExecuteChanged();
        CancelCommand.NotifyCanExecuteChanged();
    }

    private async Task RetryLoadAsync()
    {
        if (_lastInput is null)
        {
            return;
        }

        await InitializeAsync(_lastInput);
    }

    private async Task CheckAiStatusAsync()
    {
        try
        {
            var status = await _aiService.GetStatusAsync(CancellationToken.None);
            IsAiAvailable = status.IsAvailable && status.IsAuthenticated;

            if (IsAiAvailable)
            {
                AiStatusMessage = null;
            }
            else if (!status.IsAvailable)
            {
                AiStatusMessage = status.ErrorMessage ?? "AI service unavailable.";
            }
            else
            {
                AiStatusMessage = "AI authentication required.";
            }
        }
        catch (Exception ex)
        {
            IsAiAvailable = false;
            AiStatusMessage = ex.Message;
        }

        UpdateAiCommandAvailability();
    }

    partial void OnIsAiAvailableChanged(bool value)
    {
        UpdateAiCommandAvailability();
    }

    private void UpdateAiCommandAvailability()
    {
        AnalyzeCommand.NotifyCanExecuteChanged();
        GetAiHelpCommand.NotifyCanExecuteChanged();
        CancelCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsSessionLoadedChanged(bool value)
    {
        UpdateCanAccept();
    }

    private void ClearContent()
    {
        BasePaneViewModel.SetContent(string.Empty, Array.Empty<LineChange>());
        LocalPaneViewModel.SetContent(string.Empty, Array.Empty<LineChange>());
        RemotePaneViewModel.SetContent(string.Empty, Array.Empty<LineChange>());
        MergedResultViewModel.SetSourceContents(string.Empty, string.Empty, string.Empty, string.Empty);
    }

    partial void OnErrorMessageChanged(string? value)
    {
        HasError = !string.IsNullOrWhiteSpace(value);
    }
}
