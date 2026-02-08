using AutoMerge.Logic.Events;
using AutoMerge.Logic.Localization;
using AutoMerge.Logic.Services;
using AutoMerge.Core.Abstractions;
using AutoMerge.Core.Models;

namespace AutoMerge.Logic.UseCases.AcceptResolution;

public sealed class AcceptResolutionHandler
{
    private readonly IFileService _fileService;
    private readonly IConflictParser _conflictParser;
    private readonly MergeSessionManager _sessionManager;
    private readonly AutoSaveService _autoSaveService;
    private readonly IEventAggregator _eventAggregator;

    public AcceptResolutionHandler(
        IFileService fileService,
        IConflictParser conflictParser,
        MergeSessionManager sessionManager,
        AutoSaveService autoSaveService,
        IEventAggregator eventAggregator)
    {
        _fileService = fileService;
        _conflictParser = conflictParser;
        _sessionManager = sessionManager;
        _autoSaveService = autoSaveService;
        _eventAggregator = eventAggregator;
    }

    public async Task<AcceptResolutionResult> ExecuteAsync(
        AcceptResolutionCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        var session = _sessionManager.CurrentSession;
        if (session is null)
        {
            return new AcceptResolutionResult(false, LogicStrings.NoActiveSession);
        }

        if (_conflictParser.HasConflictMarkers(command.FinalContent))
        {
            return new AcceptResolutionResult(false, LogicStrings.ResolvedContentHasConflictMarkers);
        }

        var conflictFile = session.ConflictFile;
        if (conflictFile is null)
        {
            return new AcceptResolutionResult(false, LogicStrings.ConflictFileNotLoaded);
        }

        try
        {
            await _fileService.WriteAsync(
                session.MergeInput.OutputPath,
                command.FinalContent,
                conflictFile.Encoding,
                conflictFile.LineEnding,
                cancellationToken).ConfigureAwait(false);

            _autoSaveService.CleanupDrafts();
            session.SetMergedContent(command.FinalContent);
            session.SetState(SessionState.Saved);
            _eventAggregator.Publish(new SessionCompletedEvent(true));

            return new AcceptResolutionResult(true, null);
        }
        catch (Exception ex)
        {
            session.SetState(SessionState.Ready);
            return new AcceptResolutionResult(false, ex.Message);
        }
    }
}
