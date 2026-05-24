using System.Text;
using RabbitMQ.Client;

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
        channel.ExchangeDeclare(exchange: room, type: ExchangeType.Fanout);
        var body = Encoding.UTF8.GetBytes(message);
        channel.BasicPublish(exchange: room, routingKey: string.Empty, basicProperties: null, body: body);
    }
}
