using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CustomPubSub.RabbitMq;

public sealed class RabbitMqSubscriber : IMessageSubscriber
{
    private readonly RabbitMqConnection _connection;

    public RabbitMqSubscriber(RabbitMqConnection connection)
    {
        _connection = connection;
    }

    public async Task<IAsyncDisposable> SubscribeAsync(string room, Func<string, Task> handleMessageAsync, CancellationToken cancellationToken)
    {
        var channel = await _connection.CreateChannelAsync(cancellationToken);
        var queue = await channel.QueueDeclareAsync(cancellationToken: cancellationToken);
        await channel.QueueBindAsync(
            queue: queue.QueueName,
            exchange: _connection.ExchangeName,
            routingKey: room,
            cancellationToken: cancellationToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            var message = Encoding.UTF8.GetString(ea.Body.ToArray());
            await handleMessageAsync(message);
        };

        await channel.BasicConsumeAsync(
            queue: queue.QueueName,
            autoAck: true,
            consumer: consumer,
            cancellationToken: cancellationToken);

        return new Subscription(channel, cancellationToken);
    }

    private sealed class Subscription : IAsyncDisposable
    {
        private readonly IChannel _channel;
        private readonly CancellationTokenRegistration _registration;
        private int _disposed;

        public Subscription(IChannel channel, CancellationToken cancellationToken)
        {
            _channel = channel;
            _registration = cancellationToken.Register(() => _ = DisposeAsync().AsTask());
        }

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1)
            {
                return;
            }

            await _registration.DisposeAsync();
            await _channel.DisposeAsync();
        }
    }
}
