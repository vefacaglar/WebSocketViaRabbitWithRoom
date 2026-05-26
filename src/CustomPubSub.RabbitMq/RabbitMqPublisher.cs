using System.Text;
using RabbitMQ.Client;

namespace CustomPubSub.RabbitMq;

public sealed class RabbitMqPublisher : IMessagePublisher
{
    private readonly RabbitMqConnection _connection;

    public RabbitMqPublisher(RabbitMqConnection connection)
    {
        _connection = connection;
    }

    public async ValueTask PublishAsync(string room, string message, CancellationToken cancellationToken = default)
    {
        await using var channel = await _connection.CreateChannelAsync(cancellationToken);
        var body = Encoding.UTF8.GetBytes(message);
        await channel.BasicPublishAsync(_connection.ExchangeName, room, body, cancellationToken);
    }
}
