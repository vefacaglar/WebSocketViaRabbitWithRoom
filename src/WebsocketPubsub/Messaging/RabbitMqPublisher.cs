using System.Text;

namespace WebsocketPubsub.Messaging;

public sealed class RabbitMqPublisher : IMessagePublisher
{
    private readonly RabbitMqConnection _connection;

    public RabbitMqPublisher(RabbitMqConnection connection)
    {
        _connection = connection;
    }

    public void Publish(string room, string message)
    {
        using var channel = _connection.CreateChannel();
        var body = Encoding.UTF8.GetBytes(message);
        channel.BasicPublish(exchange: _connection.ExchangeName, routingKey: room, mandatory: false, basicProperties: null, body: body);
    }
}
