using System.Collections.Concurrent;

namespace AdminShell.Contracts;

/// <summary>
/// Event bus for communication between plugins.
/// </summary>
public interface IEventBus
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default) where TEvent : class;
    IDisposable Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler) where TEvent : class;
}

/// <summary>
/// In-memory implementation of IEventBus using a concurrent dictionary of handlers.
/// </summary>
public class InMemoryEventBus : IEventBus
{
    private readonly ConcurrentDictionary<Type, List<object>> _handlers = new();
    private readonly object _lock = new();

    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default) where TEvent : class
    {
        if (_handlers.TryGetValue(typeof(TEvent), out var handlers))
        {
            var typedHandlers = handlers.Cast<Func<TEvent, CancellationToken, Task>>().ToList();
            var tasks = typedHandlers.Select(h => h(@event, ct));
            return Task.WhenAll(tasks);
        }
        return Task.CompletedTask;
    }

    public IDisposable Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler) where TEvent : class
    {
        lock (_lock)
        {
            var handlers = _handlers.GetOrAdd(typeof(TEvent), _ => new List<object>());
            handlers.Add(handler);
        }

        return new Subscription(() =>
        {
            lock (_lock)
            {
                if (_handlers.TryGetValue(typeof(TEvent), out var handlers))
                {
                    handlers.Remove(handler);
                }
            }
        });
    }

    private class Subscription : IDisposable
    {
        private readonly Action _unsubscribe;
        private bool _disposed;
        public Subscription(Action unsubscribe) => _unsubscribe = unsubscribe;
        public void Dispose()
        {
            if (!_disposed) { _unsubscribe(); _disposed = true; }
        }
    }
}

/// <summary>
/// Event fired when a plugin's status changes (loaded, enabled, disabled, failed).
/// </summary>
public record PluginStatusChangedEvent(string PluginId, string PluginName, string NewStatus);

/// <summary>
/// Event fired when a user is created, updated, or deleted.
/// </summary>
public record UserChangedEvent(string UserId, string Action, string? ChangedBy);

/// <summary>
/// Event fired when the application starts (all plugins loaded and initialized).
/// </summary>
public record ApplicationStartedEvent(DateTime Timestamp, int PluginCount);