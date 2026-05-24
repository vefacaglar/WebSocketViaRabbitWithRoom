using WebsocketPubsub.Messaging;

namespace WebsocketPubsub.WebSockets;

public sealed class RoomBroadcaster : IRoomBroadcaster
{
    private readonly IMessageSubscriber _subscriber;
    private readonly WebSocketConnectionManager _connections;
    private readonly ILogger<RoomBroadcaster> _logger;
    private readonly Dictionary<string, RoomSubscription> _rooms = new();
    private readonly object _lock = new();

    public RoomBroadcaster(
        IMessageSubscriber subscriber,
        WebSocketConnectionManager connections,
        ILogger<RoomBroadcaster> logger)
    {
        _subscriber = subscriber;
        _connections = connections;
        _logger = logger;
    }

    public void Subscribe(string room)
    {
        lock (_lock)
        {
            if (_rooms.TryGetValue(room, out var existing))
            {
                existing.AddListener();
                return;
            }

            _rooms[room] = CreateSubscription(room);
        }
    }

    public void Unsubscribe(string room)
    {
        RoomSubscription? toDispose = null;

        lock (_lock)
        {
            if (_rooms.TryGetValue(room, out var subscription) && subscription.RemoveListener() == 0)
            {
                _rooms.Remove(room);
                toDispose = subscription;
            }
        }

        toDispose?.Dispose();
    }

    private RoomSubscription CreateSubscription(string room)
    {
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        var subscription = _subscriber.Subscribe(room, message =>
        {
            if (token.IsCancellationRequested)
            {
                return;
            }

            _ = BroadcastAsync(room, message, token);
        }, token);

        return new RoomSubscription(cts, subscription);
    }

    private async Task BroadcastAsync(string room, string message, CancellationToken token)
    {
        try
        {
            if (token.IsCancellationRequested)
            {
                return;
            }

            await _connections.BroadcastMessage(room, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Broadcast failed for room {Room}", room);
        }
    }

    private sealed class RoomSubscription : IDisposable
    {
        private readonly CancellationTokenSource _cts;
        private readonly IDisposable _subscription;
        private int _listeners = 1;
        private bool _disposed;

        public RoomSubscription(CancellationTokenSource cts, IDisposable subscription)
        {
            _cts = cts;
            _subscription = subscription;
        }

        public void AddListener() => Interlocked.Increment(ref _listeners);

        public int RemoveListener() => Math.Max(Interlocked.Decrement(ref _listeners), 0);

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _cts.Cancel();
            _subscription.Dispose();
            _cts.Dispose();
        }
    }
}
