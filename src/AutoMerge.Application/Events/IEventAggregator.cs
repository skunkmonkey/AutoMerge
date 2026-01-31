using System;

namespace AutoMerge.Application.Events;

public interface IEventAggregator
{
    void Publish<TEvent>(TEvent @event);

    IDisposable Subscribe<TEvent>(Action<TEvent> handler);
}
