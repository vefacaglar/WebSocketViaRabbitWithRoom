using Microsoft.Extensions.Logging;

namespace CustomPubSub.WebSocket;

public sealed class RoomBroadcaster : IRoomBroadcaster
{
    private readonly IMessageSubscriber _subscriber;
    private readonly WebSocketConnectionManager _connections;
    private readonly ILogger<RoomBroadcaster> _logger;
    private readonly Dictionary<string, RoomSubscription> _rooms = new();
    private readonly SemaphoreSlim _gate = new(1, 1);

    public RoomBroadcaster(
        IMessageSubscriber subscriber,
        WebSocketConnectionManager connections,
        ILogger<RoomBroadcaster> logger)
    {
        _subscriber = subscriber;
        _connections = connections;
        _logger = logger;
    }

    public async Task SubscribeAsync(string room, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_rooms.TryGetValue(room, out var existing))
            {
                existing.AddListener();
                return;
            }

            _rooms[room] = await CreateSubscriptionAsync(room);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task UnsubscribeAsync(string room)
    {
        RoomSubscription? toDispose = null;

        await _gate.WaitAsync();
        try
        {
            if (_rooms.TryGetValue(room, out var subscription) && subscription.RemoveListener() == 0)
            {
                _rooms.Remove(room);
                toDispose = subscription;
            }
        }
        finally
        {
            _gate.Release();
        }

        if (toDispose is not null)
        {
            await toDispose.DisposeAsync();
        }
    }

    private async Task<RoomSubscription> CreateSubscriptionAsync(string room)
    {
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        var subscription = await _subscriber.SubscribeAsync(room, async message =>
        {
            if (token.IsCancellationRequested)
            {
                return;
            }

            try
            {
                await _connections.BroadcastMessage(room, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Broadcast failed for room {Room}", room);
            }
        }, token);

        return new RoomSubscription(cts, subscription);
    }

    private sealed class RoomSubscription : IAsyncDisposable
    {
        private readonly CancellationTokenSource _cts;
        private readonly IAsyncDisposable _subscription;
        private int _listeners = 1;
        private bool _disposed;

        public RoomSubscription(CancellationTokenSource cts, IAsyncDisposable subscription)
        {
            _cts = cts;
            _subscription = subscription;
        }

        public void AddListener() => Interlocked.Increment(ref _listeners);

        public int RemoveListener() => Math.Max(Interlocked.Decrement(ref _listeners), 0);

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _cts.Cancel();
            await _subscription.DisposeAsync();
            _cts.Dispose();
        }
    }
}
