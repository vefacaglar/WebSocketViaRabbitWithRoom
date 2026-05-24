using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

public class RabbitMqService : IDisposable
{
    private readonly ConnectionFactory _factory;
    private readonly object _connectionLock = new();
    private IConnection? _connection;

    public RabbitMqService(IConfiguration configuration)
    {
        _factory = new ConnectionFactory()
        {
            HostName = configuration["RabbitMq:HostName"] ?? "localhost",
            Port = configuration.GetValue("RabbitMq:Port", AmqpTcpEndpoint.UseDefaultPort),
            UserName = configuration["RabbitMq:UserName"] ?? ConnectionFactory.DefaultUser,
            Password = configuration["RabbitMq:Password"] ?? ConnectionFactory.DefaultPass
        };
    }

    public void PublishMessage(string room, string message)
    {
        using var channel = CreateChannel();
        channel.ExchangeDeclare(exchange: room, type: ExchangeType.Fanout);
        var body = Encoding.UTF8.GetBytes(message);
        channel.BasicPublish(exchange: room, routingKey: "", basicProperties: null, body: body);
    }

    public IDisposable ConsumeMessages(string room, Action<string> handleMessage, CancellationToken token)
    {
        var channel = CreateChannel();
        channel.ExchangeDeclare(exchange: room, type: ExchangeType.Fanout);
        var queueName = channel.QueueDeclare().QueueName;
        channel.QueueBind(queue: queueName, exchange: room, routingKey: "");

        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += (model, ea) =>
        {
            if (token.IsCancellationRequested)
            {
                return;
            }

            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            handleMessage(message);
        };

        var consumerTag = channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);

        var cancellationRegistration = token.Register(() =>
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

        return new RabbitMqConsumer(channel, consumerTag, cancellationRegistration);
    }

    private IModel CreateChannel()
    {
        lock (_connectionLock)
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

    private sealed class RabbitMqConsumer : IDisposable
    {
        private readonly IModel _channel;
        private readonly string _consumerTag;
        private readonly CancellationTokenRegistration _cancellationRegistration;
        private bool _disposed;

        public RabbitMqConsumer(IModel channel, string consumerTag, CancellationTokenRegistration cancellationRegistration)
        {
            _channel = channel;
            _consumerTag = consumerTag;
            _cancellationRegistration = cancellationRegistration;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _cancellationRegistration.Dispose();

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
