using System.ComponentModel;
using AutoMerge.Application.UseCases.AcceptResolution;
using AutoMerge.Application.UseCases.AnalyzeConflict;
using AutoMerge.Application.UseCases.CancelMerge;
using AutoMerge.Application.UseCases.LoadMergeSession;
using AutoMerge.Application.UseCases.ProposeResolution;
using AutoMerge.Core.Abstractions;
using AutoMerge.Core.Models;
using AutoMerge.UI.Services;
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
        DismissSummaryCommand = new RelayCommand(() => ShowResolutionSummary = false);

        MergedResultViewModel.PropertyChanged += OnMergedResultChanged;

        // Wire cross-panel scroll synchronisation
        BasePaneViewModel.PropertyChanged += OnPaneScrollChanged;
        LocalPaneViewModel.PropertyChanged += OnPaneScrollChanged;
        RemotePaneViewModel.PropertyChanged += OnPaneScrollChanged;
        MergedResultViewModel.PropertyChanged += OnPaneScrollChanged;
    }

    private bool _isSyncingScroll;

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
    private string _aiModelName = UserPreferences.Default.AiModel;

    [ObservableProperty]
    private string _aiDetailedStatus = "Checking AI connection...";

    [ObservableProperty]
    private bool _isAiSetupNeeded;

    [ObservableProperty]
    private string? _aiSetupInstructions;

    [ObservableProperty]
    private bool _canAccept;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _hasError;

    /// <summary>
    /// Whether to show the resolution summary banner (visible after load when there are conflicts or auto-resolutions).
    /// </summary>
    [ObservableProperty]
    private bool _showResolutionSummary;

    /// <summary>
    /// Total number of conflicts originally detected in the merged file (before auto-resolve).
    /// </summary>
    [ObservableProperty]
    private int _totalOriginalConflicts;

    /// <summary>
    /// Summary headline, e.g., "4 / 5 conflicts were automatically resolved"
    /// </summary>
    [ObservableProperty]
    private string _resolutionSummaryHeadline = string.Empty;

    /// <summary>
    /// Detail message with guidance for the user on resolving remaining conflicts.
    /// </summary>
    [ObservableProperty]
    private string _resolutionSummaryDetail = string.Empty;

    /// <summary>
    /// True if all conflicts were auto-resolved and none remain.
    /// </summary>
    [ObservableProperty]
    private bool _allConflictsResolved;

    /// <summary>
    /// User can dismiss the summary banner.
    /// </summary>
    public IRelayCommand DismissSummaryCommand { get; }

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
            UpdateResolutionSummary();
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
            if (result.Success)
            {
                State = SessionState.Saved;
            }
            else
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
        State = SessionState.Cancelled;
        _cancelHandler.Execute();
    }

    private void OnMergedResultChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MergedResultViewModel.HasConflictMarkers) ||
            e.PropertyName == nameof(MergedResultViewModel.IsDirty))
        {
            UpdateCanAccept();
        }

        // Sync source pane scrolling when conflict navigation occurs
        if (e.PropertyName == nameof(MergedResultViewModel.ScrollToLine) && MergedResultViewModel.ScrollToLine > 0)
        {
            ScrollSourcePanesToConflict();
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
                AiModelName = status.ActiveModel ?? UserPreferences.Default.AiModel;
                AiDetailedStatus = $"Connected · {AiModelName}";
                IsAiSetupNeeded = false;
                AiSetupInstructions = null;
            }
            else if (!status.IsAvailable)
            {
                AiStatusMessage = status.ErrorMessage ?? "AI service unavailable.";
                AiDetailedStatus = "Not connected";
                IsAiSetupNeeded = true;
                AiSetupInstructions = status.ErrorMessage?.Contains("CLI not found", StringComparison.OrdinalIgnoreCase) == true
                    ? "Step 1: Install GitHub Copilot CLI from https://github.com/github/copilot-cli\nStep 2: Run 'copilot auth login' in your terminal\nStep 3: Click 'Retry Connection' below"
                    : "Step 1: Ensure GitHub Copilot CLI is installed and in PATH\nStep 2: Run 'copilot auth login' in your terminal\nStep 3: Click 'Retry Connection' below";
            }
            else
            {
                AiStatusMessage = "AI authentication required.";
                AiDetailedStatus = "Authentication required";
                IsAiSetupNeeded = true;
                AiSetupInstructions = "Step 1: Open a terminal and run 'copilot auth login'\nStep 2: Complete the GitHub authentication flow\nStep 3: Click 'Retry Connection' below";
            }
        }
        catch (Exception ex)
        {
            IsAiAvailable = false;
            AiStatusMessage = ex.Message;
            AiDetailedStatus = "Connection error";
            IsAiSetupNeeded = true;
            AiSetupInstructions = "An unexpected error occurred while connecting to GitHub Copilot.\nCheck that the Copilot CLI is installed and try again.";
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

    /// <summary>
    /// Computes the resolution summary banner after files are loaded and auto-resolve runs.
    /// Shows how many conflicts were auto-resolved, how many remain, a color legend, and guidance.
    /// </summary>
    private void UpdateResolutionSummary()
    {
        var autoResolved = MergedResultViewModel.AutoResolvedCount;
        var remaining = MergedResultViewModel.TotalConflictCount;
        var total = autoResolved + remaining;

        TotalOriginalConflicts = total;

        if (total == 0)
        {
            // No conflicts at all — nothing to show
            ShowResolutionSummary = false;
            return;
        }

        AllConflictsResolved = remaining == 0;

        if (AllConflictsResolved)
        {
            ResolutionSummaryHeadline = "✅ All conflicts were resolved by AI. Please verify the result.";
            ResolutionSummaryDetail = "Every conflict had a clear resolution and was merged automatically. Review the result below carefully and click Accept when you're satisfied.";
        }
        else
        {
            ResolutionSummaryHeadline = $"⚠ {autoResolved}/{total} conflicts were resolved by AI";
            ResolutionSummaryDetail = $"{remaining} conflict{(remaining == 1 ? " requires" : "s require")} manual resolution. Navigate conflicts with the ◀ ▶ buttons. Edit the merged result directly, or click \"Get AI Help\" for an AI-suggested resolution.";
        }

        ShowResolutionSummary = true;
    }

    #region Cross-panel scroll synchronisation

    private void OnPaneScrollChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_isSyncingScroll)
            return;

        if (e.PropertyName is not (nameof(DiffPaneViewModel.ScrollOffsetX)
                                or nameof(DiffPaneViewModel.ScrollOffsetY)
                                or nameof(MergedResultViewModel.ScrollOffsetX)
                                or nameof(MergedResultViewModel.ScrollOffsetY)))
            return;

        _isSyncingScroll = true;
        try
        {
            double x, y;
            string senderName;

            if (sender is DiffPaneViewModel diffVm)
            {
                x = diffVm.ScrollOffsetX;
                y = diffVm.ScrollOffsetY;
                senderName = diffVm.Title;
            }
            else if (sender is MergedResultViewModel mergedVm)
            {
                x = mergedVm.ScrollOffsetX;
                y = mergedVm.ScrollOffsetY;
                senderName = "Merged";
            }
            else
            {
                return;
            }

            // Propagate to all panels except the sender
            if (!ReferenceEquals(sender, BasePaneViewModel))
            {
                BasePaneViewModel.ScrollOffsetX = x;
                BasePaneViewModel.ScrollOffsetY = y;
            }
            if (!ReferenceEquals(sender, LocalPaneViewModel))
            {
                LocalPaneViewModel.ScrollOffsetX = x;
                LocalPaneViewModel.ScrollOffsetY = y;
            }
            if (!ReferenceEquals(sender, RemotePaneViewModel))
            {
                RemotePaneViewModel.ScrollOffsetX = x;
                RemotePaneViewModel.ScrollOffsetY = y;
            }
            if (!ReferenceEquals(sender, MergedResultViewModel))
            {
                MergedResultViewModel.ScrollOffsetX = x;
                MergedResultViewModel.ScrollOffsetY = y;
            }
        }
        finally
        {
            _isSyncingScroll = false;
        }
    }

    #endregion

    #region Source pane scroll sync

    /// <summary>
    /// When user navigates to a conflict in the merged view, scroll the source
    /// panes (local, base, remote) to show the corresponding code region.
    /// </summary>
    private void ScrollSourcePanesToConflict()
    {
        var regions = MergedResultViewModel.ConflictRegions;
        var index = MergedResultViewModel.CurrentConflictIndex - 1;
        if (index < 0 || index >= regions.Count)
        {
            return;
        }

        var region = regions[index];

        if (region.LocalContent is not null)
        {
            var line = FindContentLine(LocalPaneViewModel.Content, region.LocalContent);
            if (line > 0)
            {
                LocalPaneViewModel.ScrollToLine = 0;
                LocalPaneViewModel.ScrollToLine = line;
            }
        }

        if (region.BaseContent is not null)
        {
            var line = FindContentLine(BasePaneViewModel.Content, region.BaseContent);
            if (line > 0)
            {
                BasePaneViewModel.ScrollToLine = 0;
                BasePaneViewModel.ScrollToLine = line;
            }
        }
        else if (region.LocalContent is not null)
        {
            // No base content in conflict (2-way diff); approximate with local content
            var line = FindContentLine(BasePaneViewModel.Content, region.LocalContent);
            if (line > 0)
            {
                BasePaneViewModel.ScrollToLine = 0;
                BasePaneViewModel.ScrollToLine = line;
            }
        }

        if (region.RemoteContent is not null)
        {
            var line = FindContentLine(RemotePaneViewModel.Content, region.RemoteContent);
            if (line > 0)
            {
                RemotePaneViewModel.ScrollToLine = 0;
                RemotePaneViewModel.ScrollToLine = line;
            }
        }
    }

    /// <summary>
    /// Finds the 1-indexed line number where the given content starts in a file.
    /// Returns 0 if not found.
    /// </summary>
    private static int FindContentLine(string fileContent, string searchContent)
    {
        if (string.IsNullOrEmpty(fileContent) || string.IsNullOrEmpty(searchContent))
        {
            return 0;
        }

        var normalizedFile = fileContent.Replace("\r\n", "\n");
        var normalizedSearch = searchContent.Replace("\r\n", "\n").Trim();

        if (string.IsNullOrEmpty(normalizedSearch))
        {
            return 0;
        }

        var index = normalizedFile.IndexOf(normalizedSearch, StringComparison.Ordinal);
        if (index < 0)
        {
            return 0;
        }

        // Count newlines before the match to determine line number
        var lineNumber = 1;
        for (var i = 0; i < index; i++)
        {
            if (normalizedFile[i] == '\n')
            {
                lineNumber++;
            }
        }

        return lineNumber;
    }

    #endregion
}
