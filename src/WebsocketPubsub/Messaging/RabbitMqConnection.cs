using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace WebsocketPubsub.Messaging;

public sealed class RabbitMqConnection : IDisposable
{
    private readonly ConnectionFactory _factory;
    private readonly object _lock = new();
    private IConnection? _connection;

    public RabbitMqConnection(IOptions<RabbitMqOptions> options)
    {
        var value = options.Value;
        _factory = new ConnectionFactory
        {
            HostName = value.HostName,
            Port = value.Port,
            UserName = value.UserName,
            Password = value.Password,
        };
    }

    public IModel CreateChannel()
    {
        lock (_lock)
        {
            if (_connection?.IsOpen != true)
            {
                _connection?.Dispose();
                _connection = _factory.CreateConnection();
            }

            return _connection.CreateModel();
        }
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}
