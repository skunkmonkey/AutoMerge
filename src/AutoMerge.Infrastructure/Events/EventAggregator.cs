using System.Collections.Concurrent;
using AutoMerge.Logic.Events;

namespace AutoMerge.Infrastructure.Events;

public sealed class EventAggregator : IEventAggregator
{
    private readonly ConcurrentDictionary<Type, ConcurrentDictionary<Guid, Action<object>>> _handlers = new();

    public void Publish<TEvent>(TEvent @event)
    {
        if (@event is null)
        {
            return;
        }

        if (!_handlers.TryGetValue(typeof(TEvent), out var handlers))
        {
            return;
        }

        foreach (var handler in handlers.Values)
        {
            handler(@event);
        }
    }

    public IDisposable Subscribe<TEvent>(Action<TEvent> handler)
    {
        if (handler is null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        var handlers = _handlers.GetOrAdd(typeof(TEvent), _ => new ConcurrentDictionary<Guid, Action<object>>());
        var id = Guid.NewGuid();
        handlers[id] = evt => handler((TEvent)evt);

        return new Subscription(() => handlers.TryRemove(id, out _));
    }

    private sealed class Subscription : IDisposable
    {
        private readonly Action _unsubscribe;
        private int _disposed;

        public Subscription(Action unsubscribe)
        {
            _unsubscribe = unsubscribe;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1)
            {
                return;
            }

            _unsubscribe();
        }
    }
}
