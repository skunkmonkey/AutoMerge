using AutoMerge.Logic.Events;
using AutoMerge.Logic.Localization;
using AutoMerge.Logic.Services;
using AutoMerge.Core.Abstractions;
using AutoMerge.Core.Models;

namespace AutoMerge.Logic.UseCases.AnalyzeConflict;

public sealed class AnalyzeConflictHandler
{
    private readonly IAiService _aiService;
    private readonly MergeSessionManager _sessionManager;
    private readonly IEventAggregator _eventAggregator;

    public AnalyzeConflictHandler(
        IAiService aiService,
        MergeSessionManager sessionManager,
        IEventAggregator eventAggregator)
    {
        _aiService = aiService;
        _sessionManager = sessionManager;
        _eventAggregator = eventAggregator;
    }

    public async Task<AnalyzeConflictResult> ExecuteAsync(
        AnalyzeConflictCommand command,
        CancellationToken cancellationToken = default)
    {
        var session = _sessionManager.CurrentSession;
        if (session is null)
        {
            return new AnalyzeConflictResult(false, null, LogicStrings.NoActiveSession);
        }

        session.SetState(SessionState.Analyzing);
        _eventAggregator.Publish(new AnalysisStartedEvent());

        try
        {
            var analysis = await _aiService.AnalyzeConflictAsync(session, null, cancellationToken).ConfigureAwait(false);
            session.SetAnalysis(analysis);
            session.SetState(SessionState.Ready);
            _eventAggregator.Publish(new AnalysisCompletedEvent());
            return new AnalyzeConflictResult(true, analysis, null);
        }
        catch (Exception ex)
        {
            session.SetState(SessionState.Ready);
            _eventAggregator.Publish(new AnalysisCompletedEvent());
            return new AnalyzeConflictResult(false, null, ex.Message);
        }
    }
}
