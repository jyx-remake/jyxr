namespace Game.Application;

public sealed class SessionEvents
{
    private readonly object _syncRoot = new();
    private readonly Dictionary<Type, List<HandlerRegistration>> _handlersByType = [];
    private readonly List<Action<ISessionEvent>> _allHandlers = [];

    public IDisposable Subscribe<TEvent>(Action<TEvent> handler)
        where TEvent : class, ISessionEvent
    {
        ArgumentNullException.ThrowIfNull(handler);

        var registration = new HandlerRegistration(handler, sessionEvent => handler((TEvent)sessionEvent));
        lock (_syncRoot)
        {
            var eventType = typeof(TEvent);
            if (!_handlersByType.TryGetValue(eventType, out var handlers))
            {
                handlers = [];
                _handlersByType.Add(eventType, handlers);
            }

            handlers.Add(registration);
        }

        return new Subscription(() => Unsubscribe<TEvent>(registration));
    }

    public IDisposable SubscribeAll(Action<ISessionEvent> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        lock (_syncRoot)
        {
            _allHandlers.Add(handler);
        }

        return new Subscription(() => UnsubscribeAll(handler));
    }

    public void Publish<TEvent>(TEvent sessionEvent)
        where TEvent : class, ISessionEvent
    {
        ArgumentNullException.ThrowIfNull(sessionEvent);

        Action<ISessionEvent>[] typedHandlers;
        Action<ISessionEvent>[] allHandlers;
        lock (_syncRoot)
        {
            typedHandlers = _handlersByType.TryGetValue(sessionEvent.GetType(), out var handlers)
                ? handlers.Select(static handler => handler.Invoke).ToArray()
                : [];
            allHandlers = _allHandlers.ToArray();
        }

        foreach (var handler in typedHandlers)
        {
            handler(sessionEvent);
        }

        foreach (var handler in allHandlers)
        {
            handler(sessionEvent);
        }
    }

    private void Unsubscribe<TEvent>(HandlerRegistration registration)
        where TEvent : class, ISessionEvent
    {
        lock (_syncRoot)
        {
            var eventType = typeof(TEvent);
            if (!_handlersByType.TryGetValue(eventType, out var handlers))
            {
                return;
            }

            handlers.Remove(registration);
            if (handlers.Count == 0)
            {
                _handlersByType.Remove(eventType);
            }
        }
    }

    private void UnsubscribeAll(Action<ISessionEvent> handler)
    {
        lock (_syncRoot)
        {
            _allHandlers.Remove(handler);
        }
    }

    private sealed record HandlerRegistration(Delegate Original, Action<ISessionEvent> Invoke);

    private sealed class Subscription(Action unsubscribe) : IDisposable
    {
        private Action? _unsubscribe = unsubscribe;

        public void Dispose()
        {
            Interlocked.Exchange(ref _unsubscribe, null)?.Invoke();
        }
    }
}

public interface ISessionEvent;

public sealed record MapChangedEvent(string MapId) : ISessionEvent;

public sealed record ClockChangedEvent : ISessionEvent;

public sealed record CurrencyChangedEvent : ISessionEvent;

public sealed record AdventureStateChangedEvent : ISessionEvent;

public sealed record InventoryChangedEvent : ISessionEvent;

public sealed record ChestChangedEvent : ISessionEvent;

public sealed record ItemAcquiredEvent(
    string ItemId,
    string ItemName,
    int Quantity) : ISessionEvent;

public sealed record ToastRequestedEvent(string Message) : ISessionEvent;

public sealed record PartyChangedEvent : ISessionEvent;

public sealed record CharacterChangedEvent(string CharacterId) : ISessionEvent;

public sealed record CharacterLeveledUpEvent(string CharacterId, int OldLevel, int NewLevel) : ISessionEvent;

public sealed record JournalChangedEvent : ISessionEvent;

public sealed record StoryStateChangedEvent : ISessionEvent;

public sealed record SaveLoadedEvent : ISessionEvent;

public sealed record ProfileChangedEvent : ISessionEvent;

public sealed record ProfileLoadedEvent : ISessionEvent;

public sealed record AchievementUnlockedEvent(string AchievementId) : ISessionEvent;
