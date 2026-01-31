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

        AnalyzeCommand = new AsyncRelayCommand(AnalyzeAsync, () => !IsAiBusy && IsAiAvailable);
        GetAiHelpCommand = new AsyncRelayCommand(ProposeResolutionAsync, () => !IsAiBusy && IsAiAvailable);
        AcceptCommand = new AsyncRelayCommand(AcceptAsync, () => CanAccept);
        CancelCommand = new RelayCommand(Cancel);
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
        IsLoading = true;
        ErrorMessage = null;

        await CheckAiStatusAsync().ConfigureAwait(false);

        var result = await _loadHandler.ExecuteAsync(new LoadMergeSessionCommand(input)).ConfigureAwait(false);
        if (!result.Success || result.Session is null)
        {
            ErrorMessage = result.ErrorMessage;
            IsLoading = false;
            return;
        }

        State = result.Session.State;
        var baseFile = await _fileService.ReadAsync(input.BasePath, CancellationToken.None).ConfigureAwait(false);
        var localFile = await _fileService.ReadAsync(input.LocalPath, CancellationToken.None).ConfigureAwait(false);
        var remoteFile = await _fileService.ReadAsync(input.RemotePath, CancellationToken.None).ConfigureAwait(false);

        BasePaneViewModel.SetContent(baseFile.Content, Array.Empty<LineChange>());
        LocalPaneViewModel.SetContent(localFile.Content, _diffCalculator.CalculateDiff(baseFile.Content, localFile.Content));
        RemotePaneViewModel.SetContent(remoteFile.Content, _diffCalculator.CalculateDiff(baseFile.Content, remoteFile.Content));

        MergedResultViewModel.SetSourceContents(baseFile.Content, localFile.Content, remoteFile.Content, result.Session.CurrentMergedContent);
        UpdateCanAccept();
        IsLoading = false;
    }

    private async Task AnalyzeAsync()
    {
        IsAiBusy = true;
        var result = await _analyzeHandler.ExecuteAsync(new AnalyzeConflictCommand()).ConfigureAwait(false);
        if (!result.Success)
        {
            ErrorMessage = result.ErrorMessage;
        }
        IsAiBusy = false;
        UpdateCanAccept();
    }

    private async Task ProposeResolutionAsync()
    {
        IsAiBusy = true;
        var result = await _proposeHandler.ExecuteAsync(new ProposeResolutionCommand()).ConfigureAwait(false);
        if (result.Success && result.Resolution is not null)
        {
            MergedResultViewModel.Content = result.Resolution.ResolvedContent;
        }
        else if (!result.Success)
        {
            ErrorMessage = result.ErrorMessage;
        }
        IsAiBusy = false;
        UpdateCanAccept();
    }

    private async Task AcceptAsync()
    {
        var result = await _acceptHandler.ExecuteAsync(new AcceptResolutionCommand(MergedResultViewModel.Content)).ConfigureAwait(false);
        if (!result.Success)
        {
            ErrorMessage = result.ErrorMessage;
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
        CanAccept = !IsAiBusy && !MergedResultViewModel.HasConflictMarkers;
        AcceptCommand.NotifyCanExecuteChanged();
        AnalyzeCommand.NotifyCanExecuteChanged();
        GetAiHelpCommand.NotifyCanExecuteChanged();
        ReconnectAiCommand.NotifyCanExecuteChanged();
        RetryLoadCommand.NotifyCanExecuteChanged();
    }

    private async Task RetryLoadAsync()
    {
        if (_lastInput is null)
        {
            return;
        }

        await InitializeAsync(_lastInput).ConfigureAwait(false);
    }

    private async Task CheckAiStatusAsync()
    {
        try
        {
            var status = await _aiService.GetStatusAsync(CancellationToken.None).ConfigureAwait(false);
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
    }

    partial void OnErrorMessageChanged(string? value)
    {
        HasError = !string.IsNullOrWhiteSpace(value);
    }
}
