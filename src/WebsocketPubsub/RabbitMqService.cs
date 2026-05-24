using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Threading;

public class RabbitMqService : IDisposable
{
    private readonly ConnectionFactory _factory;
    private readonly object _connectionLock = new();
    private IConnection? _connection;
    private IModel? _channel;

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
        var channel = GetChannel();
        channel.ExchangeDeclare(exchange: room, type: ExchangeType.Fanout);
        var body = Encoding.UTF8.GetBytes(message);
        channel.BasicPublish(exchange: room, routingKey: "", basicProperties: null, body: body);
    }

    public void ConsumeMessages(string room, Action<string> handleMessage, CancellationToken token)
    {
        var channel = GetChannel();
        channel.ExchangeDeclare(exchange: room, type: ExchangeType.Fanout);
        var queueName = channel.QueueDeclare().QueueName;
        channel.QueueBind(queue: queueName, exchange: room, routingKey: "");

        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += (model, ea) =>
        {
            if (token.IsCancellationRequested)
            {
                // If cancellation is requested, stop processing messages
                return;
            }

            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            handleMessage(message);
        };

        var consumerTag = channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);

        // Listen for the cancellation token being triggered
        token.Register(() =>
        {
            // Cancel the consumer when the token is triggered
            channel.BasicCancel(consumerTag);
        });
    }

    private IModel GetChannel()
    {
        if (_channel?.IsOpen == true)
        {
            return _channel;
        }

        lock (_connectionLock)
        {
            if (_channel?.IsOpen == true)
            {
                return _channel;
            }

            if (_connection?.IsOpen != true)
            {
                _connection?.Dispose();
                _connection = _factory.CreateConnection();
            }

            _channel?.Dispose();
            _channel = _connection.CreateModel();
            return _channel;
        }
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
