using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace WebsocketPubsub.Messaging;

public sealed class RabbitMqConnection : IAsyncDisposable
{
    private readonly ConnectionFactory _factory;
    private readonly SemaphoreSlim _gate = new(1, 1);
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

    public async Task<IChannel> CreateChannelAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_connection is null || !_connection.IsOpen)
            {
                if (_connection is not null)
                {
                    await _connection.DisposeAsync();
                }

                _connection = await _factory.CreateConnectionAsync(cancellationToken);
                _exchangeDeclared = false;
            }

            var channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

            if (!_exchangeDeclared)
            {
                await channel.ExchangeDeclareAsync(
                    exchange: ExchangeName,
                    type: ExchangeType.Direct,
                    durable: false,
                    autoDelete: false,
                    cancellationToken: cancellationToken);
                _exchangeDeclared = true;
            }

            return channel;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        _gate.Dispose();
    }
}
