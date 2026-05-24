using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace WebsocketPubsub.Messaging;

public sealed class RabbitMqConnection : IDisposable
{
    private readonly ConnectionFactory _factory;
    private readonly object _lock = new();
    private IConnection? _connection;
    private bool _exchangeDeclared;

    public RabbitMqConnection(IOptions<RabbitMqOptions> options)
    {
        var value = options.Value;
        ExchangeName = value.ExchangeName;
        _factory = new ConnectionFactory
        {
            HostName = value.HostName,
            Port = value.Port,
            UserName = value.UserName,
            Password = value.Password,
        };
    }

    public string ExchangeName { get; }

    public IModel CreateChannel()
    {
        lock (_lock)
        {
            if (_connection?.IsOpen != true)
            {
                _connection?.Dispose();
                _connection = _factory.CreateConnection();
                _exchangeDeclared = false;
            }

            var channel = _connection.CreateModel();
            if (!_exchangeDeclared)
            {
                channel.ExchangeDeclare(ExchangeName, ExchangeType.Direct, durable: false, autoDelete: false);
                _exchangeDeclared = true;
            }

            return channel;
        }
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}
