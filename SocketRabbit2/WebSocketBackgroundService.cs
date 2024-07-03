using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

public interface IWebSocketBackgroundService
{
    void StartConsumingForRoom(string room);
}

public class WebSocketBackgroundService : BackgroundService, IWebSocketBackgroundService
{
    private readonly RabbitMqService _rabbitMqService;
    private readonly WebSocketConnectionManager _connectionManager;
    private readonly ConcurrentDictionary<string, Task> _consumers = new ConcurrentDictionary<string, Task>();

    public WebSocketBackgroundService(RabbitMqService rabbitMqService, WebSocketConnectionManager connectionManager)
    {
        _rabbitMqService = rabbitMqService;
        _connectionManager = connectionManager;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.CompletedTask;
    }

    public void StartConsumingForRoom(string room)
    {
        if (!_consumers.ContainsKey(room))
        {
            var consumerTask = Task.Run(() =>
            {
                _rabbitMqService.ConsumeMessages(room, async message =>
                {
                    await _connectionManager.BroadcastMessage(room, message);
                });
            });
            _consumers.TryAdd(room, consumerTask);
        }
    }

    public override void Dispose()
    {
        _rabbitMqService.Dispose();
        base.Dispose();
    }
}