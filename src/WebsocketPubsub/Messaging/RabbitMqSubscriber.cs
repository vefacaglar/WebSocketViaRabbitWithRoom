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
        channel.ExchangeDeclare(exchange: room, type: ExchangeType.Fanout);
        var queueName = channel.QueueDeclare().QueueName;
        channel.QueueBind(queue: queueName, exchange: room, routingKey: string.Empty);

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

        var consumerTag = channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);

        var registration = cancellationToken.Register(() =>
        {
            try
            {
                if (channel.IsOpen)
                {
                    channel.BasicCancel(consumerTag);
                }
            }
            catch
            {
            }
        });

        return new Subscription(channel, consumerTag, registration);
    }

    private sealed class Subscription : IDisposable
    {
        private readonly IModel _channel;
        private readonly string _consumerTag;
        private readonly CancellationTokenRegistration _registration;
        private bool _disposed;

        public Subscription(IModel channel, string consumerTag, CancellationTokenRegistration registration)
        {
            _channel = channel;
            _consumerTag = consumerTag;
            _registration = registration;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _registration.Dispose();

            if (_channel.IsOpen)
            {
                try
                {
                    _channel.BasicCancel(_consumerTag);
                }
                catch
                {
                }
            }

            _channel.Dispose();
        }
    }
}
