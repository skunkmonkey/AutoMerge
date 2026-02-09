using AutoMerge.Logic.Events;
using AutoMerge.Core.Models;

namespace AutoMerge.Logic.Services;

public sealed class MergeSessionManager
{
    private readonly IEventAggregator _eventAggregator;

    public MergeSessionManager(IEventAggregator eventAggregator)
    {
        _eventAggregator = eventAggregator;
    }

    public MergeSession? CurrentSession { get; private set; }

    public MergeSession CreateSession(MergeInput input)
    {
        CurrentSession = new MergeSession(input);
        _eventAggregator.Publish(new SessionLoadedEvent());
        return CurrentSession;
    }

    public void ClearSession()
    {
        CurrentSession = null;
    }
}
