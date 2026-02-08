using System;

namespace AutoMerge.Logic.Events;

public interface IEventAggregator
{
    void Publish<TEvent>(TEvent @event);

    IDisposable Subscribe<TEvent>(Action<TEvent> handler);
}
