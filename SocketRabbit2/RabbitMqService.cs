using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Threading;

public class RabbitMqService : IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;

    public RabbitMqService()
    {
        //var factory = new ConnectionFactory() { HostName = "localhost" };
        var factory = new ConnectionFactory()
        {
            HostName = "localhost"
        };
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
    }

    public void PublishMessage(string room, string message)
    {
        _channel.ExchangeDeclare(exchange: room, type: ExchangeType.Fanout);
        var body = Encoding.UTF8.GetBytes(message);
        _channel.BasicPublish(exchange: room, routingKey: "", basicProperties: null, body: body);
    }

    public void ConsumeMessages(string room, Action<string> handleMessage, CancellationToken token)
    {
        _channel.ExchangeDeclare(exchange: room, type: ExchangeType.Fanout);
        var queueName = _channel.QueueDeclare().QueueName;
        _channel.QueueBind(queue: queueName, exchange: room, routingKey: "");

        var consumer = new EventingBasicConsumer(_channel);
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

        var consumerTag = _channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);

        // Listen for the cancellation token being triggered
        token.Register(() =>
        {
            // Cancel the consumer when the token is triggered
            _channel.BasicCancel(consumerTag);
        });
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
