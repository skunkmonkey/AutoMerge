using System.ComponentModel;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using AutoMerge.Logic.UseCases.AcceptResolution;
using AutoMerge.Logic.UseCases.CancelMerge;
using AutoMerge.Logic.UseCases.LoadMergeSession;
using AutoMerge.Logic.UseCases.ProposeResolution;
using AutoMerge.Core.Abstractions;
using AutoMerge.Core.Models;
using AutoMerge.UI.Localization;
using AutoMerge.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AutoMerge.UI.ViewModels;

public sealed partial class MainWindowViewModel : ViewModelBase
{
    private readonly LoadMergeSessionHandler _loadHandler;
    private readonly ProposeResolutionHandler _proposeHandler;
    private readonly AcceptResolutionHandler _acceptHandler;
    private readonly CancelMergeHandler _cancelHandler;
    private readonly IFileService _fileService;
    private readonly IDiffCalculator _diffCalculator;
    private readonly IAiService _aiService;
    private MergeInput? _lastInput;

    public MainWindowViewModel(
        LoadMergeSessionHandler loadHandler,
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
        _proposeHandler = proposeHandler;
        _acceptHandler = acceptHandler;
        _cancelHandler = cancelHandler;
        _fileService = fileService;
        _diffCalculator = diffCalculator;
        _aiService = aiService;

        BasePaneViewModel = new DiffPaneViewModel { Title = UIStrings.PanelTitleBase, IsReadOnly = true };
        LocalPaneViewModel = new DiffPaneViewModel { Title = UIStrings.PanelTitleLocal, IsReadOnly = true };
        RemotePaneViewModel = new DiffPaneViewModel { Title = UIStrings.PanelTitleRemote, IsReadOnly = true };
        MergedResultViewModel = mergedResultViewModel;
        AiChatViewModel = aiChatViewModel;

        GetAiHelpCommand = new AsyncRelayCommand(ProposeResolutionAsync, () => !IsAiBusy && IsAiAvailable && IsSessionLoaded && !HasAiResolved);
        AcceptCommand = new AsyncRelayCommand(AcceptAsync, () => CanAccept);
        CancelCommand = new RelayCommand(Cancel, () => IsSessionLoaded);
        OpenPreferencesCommand = new RelayCommand(() => { });
        ReconnectAiCommand = new AsyncRelayCommand(CheckAiStatusAsync, () => !IsAiBusy);
        RetryLoadCommand = new AsyncRelayCommand(RetryLoadAsync, () => !IsLoading && _lastInput is not null);
        DismissSummaryCommand = new RelayCommand(() => ShowResolutionSummary = false);
        CopyDiffToolParamsCommand = new AsyncRelayCommand(() => CopyToClipboardAsync(UIStrings.MainWindowDiffToolParametersValue));
        CopyMergeToolParamsCommand = new AsyncRelayCommand(() => CopyToClipboardAsync(UIStrings.MainWindowMergeToolParametersValue));
        CloseDiffCommand = new RelayCommand(CloseDiff);

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

    /// <summary>
    /// The background AI auto-resolve task started after initialization.
    /// Exposed so tests can await completion; production code does not await this.
    /// </summary>
    internal Task? AutoResolveTask { get; private set; }

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isSessionLoaded;

    /// <summary>
    /// True when the application is in diff-only mode (comparing two files, no merge output).
    /// Hides the base pane, merged result, AI chat, and merge-specific toolbar actions.
    /// </summary>
    [ObservableProperty]
    private bool _isDiffMode;

    [ObservableProperty]
    private bool _isAiBusy;

    /// <summary>
    /// Whether AI has already attempted to resolve conflicts in this session.
    /// Once set, the Resolve With AI button is disabled to prevent redundant calls.
    /// </summary>
    [ObservableProperty]
    private bool _hasAiResolved;

    [ObservableProperty]
    private bool _isAiAvailable;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _busyMessage = string.Empty;

    [ObservableProperty]
    private string? _aiStatusMessage;

    [ObservableProperty]
    private string _aiModelName = UserPreferences.Default.AiModel;

    [ObservableProperty]
    private string _aiDetailedStatus = UIStrings.AiDetailedStatusChecking;

    [ObservableProperty]
    private bool _isAiSetupNeeded;

    [ObservableProperty]
    private string? _aiSetupInstructions;

    /// <summary>
    /// Set to true when the AI status check reveals that Copilot is not available
    /// and the setup dialog should be shown to the user. The View resets this after
    /// displaying the dialog.
    /// </summary>
    [ObservableProperty]
    private bool _showAiSetupDialog;

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
    /// Number of conflicts resolved by AI (tracked after AI actions).
    /// </summary>
    [ObservableProperty]
    private int _aiResolvedCount;

    /// <summary>
    /// User can dismiss the summary banner.
    /// </summary>
    public IRelayCommand DismissSummaryCommand { get; }

    public IAsyncRelayCommand GetAiHelpCommand { get; }
    public IAsyncRelayCommand AcceptCommand { get; }
    public IRelayCommand CancelCommand { get; }
    public IRelayCommand OpenPreferencesCommand { get; }
    public IAsyncRelayCommand ReconnectAiCommand { get; }
    public IAsyncRelayCommand RetryLoadCommand { get; }
    public IAsyncRelayCommand CopyDiffToolParamsCommand { get; }
    public IAsyncRelayCommand CopyMergeToolParamsCommand { get; }
    public IRelayCommand CloseDiffCommand { get; }

    public Task InitializeAsync(MergeInput input) => InitializeAsync(input, null);

    public async Task InitializeAsync(MergeInput input, Action<string>? onProgress)
    {
        _lastInput = input;
        IsSessionLoaded = false;
        IsLoading = true;
        ErrorMessage = null;
        HasError = false;
        ClearContent();
        AiResolvedCount = 0;
        HasAiResolved = false;
        _lastKnownRemainingConflicts = 0;
        TotalOriginalConflicts = 0;
        ShowResolutionSummary = false;
        ResolutionSummaryHeadline = string.Empty;
        ResolutionSummaryDetail = string.Empty;
        AllConflictsResolved = false;

        try
        {
            // Check AI status (can run on background thread)
            onProgress?.Invoke("Checking AI connection...");
            await CheckAiStatusAsync();

            // Load the merge session
            onProgress?.Invoke("Loading merge session...");
            var result = await _loadHandler.ExecuteAsync(new LoadMergeSessionCommand(input));
            if (!result.Success || result.Session is null)
            {
                ErrorMessage = result.ErrorMessage ?? UIStrings.LoadMergeFailed;
                IsLoading = false;
                IsSessionLoaded = false;
                return;
            }

            State = result.Session.State;

            // Read all files
            onProgress?.Invoke("Reading input files...");
            var baseFile = await _fileService.ReadAsync(input.BasePath, CancellationToken.None);
            var localFile = await _fileService.ReadAsync(input.LocalPath, CancellationToken.None);
            var remoteFile = await _fileService.ReadAsync(input.RemotePath, CancellationToken.None);

            // Calculate diffs and populate panes
            onProgress?.Invoke("Computing diffs...");
            BasePaneViewModel.SetContent(baseFile.Content, Array.Empty<LineChange>());
            LocalPaneViewModel.SetContent(localFile.Content, _diffCalculator.CalculateDiff(baseFile.Content, localFile.Content));
            RemotePaneViewModel.SetContent(remoteFile.Content, _diffCalculator.CalculateDiff(baseFile.Content, remoteFile.Content));

            onProgress?.Invoke("Resolving trivial conflicts...");
            MergedResultViewModel.SetSourceContents(baseFile.Content, localFile.Content, remoteFile.Content, result.Session.CurrentMergedContent);
            _lastKnownRemainingConflicts = MergedResultViewModel.TotalConflictCount;
            UpdateCanAccept();
            IsLoading = false;
            IsSessionLoaded = true;
            UpdateResolutionSummary();

            onProgress?.Invoke("Ready.");

            // Fire-and-forget so the splash can transition to the main window
            // immediately. Errors are handled inside TryAutoResolveWithAiAsync.
            AutoResolveTask = TryAutoResolveWithAiAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = string.Format(CultureInfo.CurrentUICulture, UIStrings.ErrorLoadingFilesFormat, ex.Message);
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
        AiResolvedCount = 0;
        HasAiResolved = false;
        _lastKnownRemainingConflicts = 0;
        TotalOriginalConflicts = 0;
        ShowResolutionSummary = false;
        ResolutionSummaryHeadline = string.Empty;
        ResolutionSummaryDetail = string.Empty;
        AllConflictsResolved = false;
        UpdateCanAccept();

        // Check AI status so the welcome screen shows the real connection state
        _ = CheckAiStatusAsync();
    }

    /// <summary>
    /// Initializes the application in diff-only mode, showing a side-by-side comparison
    /// of the local and remote files without merge/AI functionality.
    /// </summary>
    public Task InitializeDiffAsync(MergeInput input) => InitializeDiffAsync(input, null);

    public async Task InitializeDiffAsync(MergeInput input, Action<string>? onProgress)
    {
        _lastInput = input;
        IsSessionLoaded = false;
        IsLoading = true;
        IsDiffMode = true;
        ErrorMessage = null;
        HasError = false;
        ClearContent();

        try
        {
            onProgress?.Invoke("Reading files for diff...");
            var localFile = await _fileService.ReadAsync(input.LocalPath, CancellationToken.None);
            var remoteFile = await _fileService.ReadAsync(input.RemotePath, CancellationToken.None);

            onProgress?.Invoke("Computing diffs...");
            LocalPaneViewModel.SetContent(localFile.Content, Array.Empty<LineChange>());
            RemotePaneViewModel.SetContent(remoteFile.Content, _diffCalculator.CalculateDiff(localFile.Content, remoteFile.Content));

            IsLoading = false;
            IsSessionLoaded = true;
            State = SessionState.Ready;
            onProgress?.Invoke("Ready.");
        }
        catch (Exception ex)
        {
            ErrorMessage = string.Format(CultureInfo.CurrentUICulture, UIStrings.ErrorLoadingFilesFormat, ex.Message);
            IsLoading = false;
            IsSessionLoaded = false;
        }
    }

    private void CloseDiff()
    {
        State = SessionState.Saved;
    }

    private async Task ProposeResolutionAsync()
    {
        var beforeRemaining = MergedResultViewModel.TotalConflictCount;
        IsAiBusy = true;

        try
        {
            var localIntentBuilder = new System.Text.StringBuilder();
            var remoteIntentBuilder = new System.Text.StringBuilder();

            var command = new ProposeResolutionCommand(
                Preferences: null,
                OnLocalIntentChunk: chunk => localIntentBuilder.Append(chunk),
                OnRemoteIntentChunk: chunk => remoteIntentBuilder.Append(chunk),
                OnBusyMessageChanged: (busyMessage, _) =>
                {
                    BusyMessage = busyMessage;
                });

            // Set initial busy message
            BusyMessage = UIStrings.BusyMessageResearchingIntents;

            var result = await _proposeHandler.ExecuteAsync(command);

            if (result.Success && result.Resolution is not null)
            {
                // Output Local intent to AI chat
                if (!string.IsNullOrWhiteSpace(result.LocalIntent))
                {
                    AiChatViewModel.Messages.Add(new ChatMessage(
                        ChatRole.Assistant,
                        string.Format(CultureInfo.CurrentUICulture, UIStrings.AiChatLocalIntentFormat, result.LocalIntent),
                        DateTimeOffset.UtcNow));
                }

                // Output Remote intent to AI chat
                if (!string.IsNullOrWhiteSpace(result.RemoteIntent))
                {
                    AiChatViewModel.Messages.Add(new ChatMessage(
                        ChatRole.Assistant,
                        string.Format(CultureInfo.CurrentUICulture, UIStrings.AiChatRemoteIntentFormat, result.RemoteIntent),
                        DateTimeOffset.UtcNow));
                }

                // Output resolution explanation to AI chat
                AiChatViewModel.Messages.Add(new ChatMessage(
                    ChatRole.Assistant,
                    string.Format(CultureInfo.CurrentUICulture, UIStrings.AiChatResolutionFormat, result.Resolution.Explanation),
                    DateTimeOffset.UtcNow));

                MergedResultViewModel.Content = result.Resolution.ResolvedContent;
                var afterRemaining = MergedResultViewModel.TotalConflictCount;
                UpdateAiResolvedCount(beforeRemaining, afterRemaining);
                HasAiResolved = true;
                UpdateResolutionSummary();
            }
            else if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = string.Format(CultureInfo.CurrentUICulture, UIStrings.AiResolutionFailedFormat, ex.Message);
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
            ErrorMessage = string.Format(CultureInfo.CurrentUICulture, UIStrings.AcceptFailedFormat, ex.Message);
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
            e.PropertyName == nameof(MergedResultViewModel.IsDirty) ||
            e.PropertyName == nameof(MergedResultViewModel.AllConflictsApproved))
        {
            UpdateCanAccept();
        }

        if (e.PropertyName == nameof(MergedResultViewModel.TotalConflictCount) ||
            e.PropertyName == nameof(MergedResultViewModel.AutoResolvedCount) ||
            e.PropertyName == nameof(MergedResultViewModel.UnapprovedCount))
        {
            UpdateResolutionSummary();
        }

        // Sync source pane scrolling when conflict navigation occurs
        if (e.PropertyName == nameof(MergedResultViewModel.ScrollToLine) && MergedResultViewModel.ScrollToLine > 0)
        {
            ScrollSourcePanesToConflict();
        }
    }

    private void UpdateCanAccept()
    {
        CanAccept = IsSessionLoaded && !IsAiBusy && !MergedResultViewModel.HasConflictMarkers && MergedResultViewModel.AllConflictsApproved;
        AcceptCommand.NotifyCanExecuteChanged();
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
                AiDetailedStatus = string.Format(CultureInfo.CurrentUICulture, UIStrings.AiConnectedWithModelFormat, AiModelName);
                IsAiSetupNeeded = false;
                AiSetupInstructions = null;
            }
            else if (!status.IsAvailable)
            {
                AiStatusMessage = status.ErrorMessage ?? UIStrings.AiServiceUnavailable;
                AiDetailedStatus = UIStrings.AiNotConnected;
                IsAiSetupNeeded = true;
                AiSetupInstructions = status.ErrorMessage?.Contains("CLI not found", StringComparison.OrdinalIgnoreCase) == true
                    ? UIStrings.AiSetupInstructionsCliMissing
                    : UIStrings.AiSetupInstructionsCliNotFound;
                ShowAiSetupDialog = true;
            }
            else
            {
                AiStatusMessage = UIStrings.AiAuthenticationRequired;
                AiDetailedStatus = UIStrings.AiAuthenticationRequiredShort;
                IsAiSetupNeeded = true;
                AiSetupInstructions = UIStrings.AiSetupInstructionsAuth;
                ShowAiSetupDialog = true;
            }
        }
        catch (Exception ex)
        {
            IsAiAvailable = false;
            AiStatusMessage = ex.Message;
            AiDetailedStatus = UIStrings.AiConnectionError;
            IsAiSetupNeeded = true;
            AiSetupInstructions = UIStrings.AiUnexpectedError;
            ShowAiSetupDialog = true;
        }

        UpdateAiCommandAvailability();
    }

    partial void OnIsAiAvailableChanged(bool value)
    {
        UpdateAiCommandAvailability();
    }

    partial void OnIsLoadingChanged(bool value)
    {
        UpdateBusyState();
    }

    partial void OnIsAiBusyChanged(bool value)
    {
        UpdateBusyState();
    }

    private void UpdateAiCommandAvailability()
    {
        GetAiHelpCommand.NotifyCanExecuteChanged();
        CancelCommand.NotifyCanExecuteChanged();
    }

    partial void OnHasAiResolvedChanged(bool value)
    {
        UpdateAiCommandAvailability();
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

    private static async Task CopyToClipboardAsync(string text)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
            desktop.MainWindow is not null)
        {
            var clipboard = TopLevel.GetTopLevel(desktop.MainWindow)?.Clipboard;
            if (clipboard is not null)
            {
                await clipboard.SetTextAsync(text);
            }
        }
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
        var total = TotalOriginalConflicts > 0 ? TotalOriginalConflicts : autoResolved + remaining;

        if (TotalOriginalConflicts == 0 && total > 0)
        {
            TotalOriginalConflicts = total;
        }

        if (total == 0)
        {
            // No conflicts at all — nothing to show
            ShowResolutionSummary = false;
            return;
        }

        AllConflictsResolved = remaining == 0;

        var aiResolved = Math.Max(0, AiResolvedCount);
        var resolvedByAiText = string.Format(CultureInfo.CurrentUICulture, UIStrings.ResolutionSummaryResolvedByAiFormat, aiResolved);
        var autoResolvedText = autoResolved > 0
            ? string.Format(CultureInfo.CurrentUICulture, UIStrings.ResolutionSummaryAutoResolvedFormat, autoResolved)
            : string.Empty;

        if (AllConflictsResolved)
        {
            ResolutionSummaryHeadline = string.Format(CultureInfo.CurrentUICulture, UIStrings.ResolutionSummaryHeadlineAllResolvedFormat, resolvedByAiText, autoResolvedText);
            ResolutionSummaryDetail = UIStrings.ResolutionSummaryDetailAllResolved;
        }
        else
        {
            ResolutionSummaryHeadline = string.Format(CultureInfo.CurrentUICulture, UIStrings.ResolutionSummaryHeadlineRemainingFormat, resolvedByAiText, autoResolvedText, remaining);
            var detailFormat = remaining == 1
                ? UIStrings.ResolutionSummaryDetailRemainingSingularFormat
                : UIStrings.ResolutionSummaryDetailRemainingPluralFormat;
            ResolutionSummaryDetail = string.Format(CultureInfo.CurrentUICulture, detailFormat, remaining);
        }

        ShowResolutionSummary = true;
    }

    private int _lastKnownRemainingConflicts;

    private void UpdateAiResolvedCount(int beforeRemaining, int afterRemaining)
    {
        if (beforeRemaining <= 0)
        {
            _lastKnownRemainingConflicts = afterRemaining;
            return;
        }

        var resolvedDelta = Math.Max(0, beforeRemaining - afterRemaining);
        if (resolvedDelta > 0)
        {
            AiResolvedCount += resolvedDelta;
            var maxAiResolved = Math.Max(0, TotalOriginalConflicts - MergedResultViewModel.AutoResolvedCount);
            if (AiResolvedCount > maxAiResolved)
            {
                AiResolvedCount = maxAiResolved;
            }
        }

        _lastKnownRemainingConflicts = afterRemaining;
    }

    private void UpdateBusyState()
    {
        IsBusy = IsLoading || IsAiBusy;

        if (IsLoading)
        {
            BusyMessage = UIStrings.BusyMessageLoadingMergeFiles;
        }
        else if (IsAiBusy)
        {
            BusyMessage = UIStrings.BusyMessageAiProcessing;
        }
        else
        {
            BusyMessage = string.Empty;
        }
    }

    private async Task TryAutoResolveWithAiAsync()
    {
        if (!IsAiAvailable || !IsSessionLoaded)
        {
            return;
        }

        var remainingConflicts = MergedResultViewModel.TotalConflictCount;
        if (remainingConflicts <= 0)
        {
            return;
        }

        // Automatically invoke AI resolution when a diff is loaded.
        await ProposeResolutionAsync();
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
    /// Uses original conflict regions so navigation works even after AI has
    /// removed conflict markers from the merged content.
    /// </summary>
    private void ScrollSourcePanesToConflict()
    {
        var index = MergedResultViewModel.CurrentConflictIndex - 1;
        if (index < 0 || index >= MergedResultViewModel.ApprovalItems.Count)
        {
            return;
        }

        var approvalItem = MergedResultViewModel.ApprovalItems[index];
        var originalConflicts = MergedResultViewModel.OriginalConflictRegions;

        if (approvalItem.Index < 0 || approvalItem.Index >= originalConflicts.Count)
        {
            // Fallback: scroll all source panes to the same line as the merged pane
            var scrollLine = MergedResultViewModel.ScrollToLine;
            if (scrollLine > 0)
            {
                ScrollAllSourcePanesToLine(scrollLine);
            }
            return;
        }

        var region = originalConflicts[approvalItem.Index];

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
    /// Scrolls all three source panes to the given line number (approximate fallback).
    /// </summary>
    private void ScrollAllSourcePanesToLine(int line)
    {
        BasePaneViewModel.ScrollToLine = 0;
        BasePaneViewModel.ScrollToLine = line;
        LocalPaneViewModel.ScrollToLine = 0;
        LocalPaneViewModel.ScrollToLine = line;
        RemotePaneViewModel.ScrollToLine = 0;
        RemotePaneViewModel.ScrollToLine = line;
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
