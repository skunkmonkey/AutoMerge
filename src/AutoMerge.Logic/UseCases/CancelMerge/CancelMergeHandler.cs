using AutoMerge.Logic.Events;
using AutoMerge.Logic.Services;
using AutoMerge.Core.Models;

namespace AutoMerge.Logic.UseCases.CancelMerge;

public sealed class CancelMergeHandler
{
    private readonly MergeSessionManager _sessionManager;
    private readonly AutoSaveService _autoSaveService;
    private readonly IEventAggregator _eventAggregator;

    public CancelMergeHandler(
        MergeSessionManager sessionManager,
        AutoSaveService autoSaveService,
        IEventAggregator eventAggregator)
    {
        _sessionManager = sessionManager;
        _autoSaveService = autoSaveService;
        _eventAggregator = eventAggregator;
    }

    public void Execute()
    {
        var session = _sessionManager.CurrentSession;
        if (session is null)
        {
            return;
        }

        session.SetState(SessionState.Cancelled);
        _autoSaveService.CleanupDrafts();
        _eventAggregator.Publish(new SessionCompletedEvent(false));
    }
}
