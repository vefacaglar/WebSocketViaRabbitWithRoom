using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

public interface IWebSocketBackgroundService
{
    void StartConsumingForRoom(string room);
    void StopConsumingForRoom(string room);
    void NotifyClientDisconnected(string room);
}

public class WebSocketBackgroundService : IWebSocketBackgroundService
{
    private readonly RabbitMqService _rabbitMqService;
    private readonly WebSocketConnectionManager _connectionManager;
    private readonly Dictionary<string, RoomConsumer> _rooms = new Dictionary<string, RoomConsumer>();
    private readonly object _roomsLock = new();

    public WebSocketBackgroundService(RabbitMqService rabbitMqService, WebSocketConnectionManager connectionManager)
    {
        _rabbitMqService = rabbitMqService;
        _connectionManager = connectionManager;
    }

    public void StartConsumingForRoom(string room)
    {
        lock (_roomsLock)
        {
            if (_rooms.TryGetValue(room, out var consumer))
            {
                consumer.IncrementConnectionCount();
                return;
            }

            _rooms[room] = CreateRoomConsumer(room);
        }
    }

    public void NotifyClientDisconnected(string room)
    {
        RoomConsumer? consumerToDispose = null;

        lock (_roomsLock)
        {
            if (_rooms.TryGetValue(room, out var consumer) && consumer.DecrementConnectionCount() == 0)
            {
                _rooms.Remove(room);
                consumerToDispose = consumer;
            }
        }

        consumerToDispose?.Dispose();
    }

    public void StopConsumingForRoom(string room)
    {
        RoomConsumer? consumerToDispose = null;

        lock (_roomsLock)
        {
            if (_rooms.Remove(room, out var consumer))
            {
                consumerToDispose = consumer;
            }
        }

        consumerToDispose?.Dispose();
    }

    private RoomConsumer CreateRoomConsumer(string room)
    {
        var cancellationTokenSource = new CancellationTokenSource();
        var token = cancellationTokenSource.Token;

        var subscription = _rabbitMqService.ConsumeMessages(room, message =>
        {
            if (!token.IsCancellationRequested)
            {
                _ = BroadcastMessage(room, message, token);
            }
        }, token);

        return new RoomConsumer(cancellationTokenSource, subscription);
    }

    private async Task BroadcastMessage(string room, string message, CancellationToken token)
    {
        try
        {
            if (!token.IsCancellationRequested)
            {
                await _connectionManager.BroadcastMessage(room, message);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Broadcast failed for room '{room}': {ex.Message}");
        }
    }

    private sealed class RoomConsumer : IDisposable
    {
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly IDisposable _subscription;
        private int _connectionCount = 1;
        private bool _disposed;

        public RoomConsumer(CancellationTokenSource cancellationTokenSource, IDisposable subscription)
        {
            _cancellationTokenSource = cancellationTokenSource;
            _subscription = subscription;
        }

        public void IncrementConnectionCount()
        {
            Interlocked.Increment(ref _connectionCount);
        }

        public int DecrementConnectionCount()
        {
            var count = Interlocked.Decrement(ref _connectionCount);
            return Math.Max(count, 0);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _cancellationTokenSource.Cancel();
            _subscription.Dispose();
            _cancellationTokenSource.Dispose();
        }
    }
}
