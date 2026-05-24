using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace WebsocketPubsub.Messaging;

public sealed class RabbitMqSubscriber : IMessageSubscriber
{
    private readonly RabbitMqConnection _connection;

    public RabbitMqSubscriber(RabbitMqConnection connection)
    {
        _connection = connection;
    }

    public IDisposable Subscribe(string room, Action<string> handleMessage, CancellationToken cancellationToken)
    {
        var channel = _connection.CreateChannel();
        var queueName = channel.QueueDeclare().QueueName;
        channel.QueueBind(queue: queueName, exchange: _connection.ExchangeName, routingKey: room);

        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += (_, ea) =>
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            var message = Encoding.UTF8.GetString(ea.Body.ToArray());
            handleMessage(message);
        };

        channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);

        return new Subscription(channel, cancellationToken);
    }

    private sealed class Subscription : IDisposable
    {
        private readonly IModel _channel;
        private readonly CancellationTokenRegistration _registration;
        private bool _disposed;

        public Subscription(IModel channel, CancellationToken cancellationToken)
        {
            _channel = channel;
            _registration = cancellationToken.Register(Dispose);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _registration.Dispose();
            _channel.Dispose();
        }
    }
}
