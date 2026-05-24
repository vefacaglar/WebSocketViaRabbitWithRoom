using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;
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
    private readonly ConcurrentDictionary<string, Task> _consumers = new ConcurrentDictionary<string, Task>();
    private readonly ConcurrentDictionary<string, int> _roomConnectionCounts = new ConcurrentDictionary<string, int>();
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _cancellationTokens = new ConcurrentDictionary<string, CancellationTokenSource>();

    public WebSocketBackgroundService(RabbitMqService rabbitMqService, WebSocketConnectionManager connectionManager)
    {
        _rabbitMqService = rabbitMqService;
        _connectionManager = connectionManager;
    }

    public void StartConsumingForRoom(string room)
    {
        if (!_consumers.ContainsKey(room))
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;

            var consumerTask = Task.Run(() =>
            {
                _rabbitMqService.ConsumeMessages(room, async message =>
                {
                    if (!token.IsCancellationRequested)
                    {
                        await _connectionManager.BroadcastMessage(room, message);
                    }
                }, token); // Pass the cancellation token to stop consuming when requested
            }, token);

            _consumers.TryAdd(room, consumerTask);
            _cancellationTokens.TryAdd(room, cancellationTokenSource);
        }

        // Increment connection count for this room
        _roomConnectionCounts.AddOrUpdate(room, 1, (key, currentCount) => currentCount + 1);
    }

    public void NotifyClientDisconnected(string room)
    {
        // Decrement connection count when a client disconnects
        if (_roomConnectionCounts.TryGetValue(room, out var connectionCount) && connectionCount > 0)
        {
            var newCount = connectionCount - 1;

            if (newCount == 0)
            {
                // If no clients remain, stop consuming for the room
                StopConsumingForRoom(room);
            }
            else
            {
                _roomConnectionCounts[room] = newCount;
            }
        }
    }

    public void StopConsumingForRoom(string room)
    {
        if (_consumers.TryRemove(room, out var consumerTask) && _cancellationTokens.TryRemove(room, out var cancellationTokenSource))
        {
            // Cancel the consumer task
            cancellationTokenSource.Cancel();

            // Optionally wait for the consumer task to complete
            consumerTask.Wait();

            // Remove the room connection count
            _roomConnectionCounts.TryRemove(room, out _);
        }
    }
}
