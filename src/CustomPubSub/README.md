# CustomPubSub

Core abstractions for room-based pub/sub messaging in .NET.

## Interfaces

```csharp
public interface IMessagePublisher
{
    ValueTask PublishAsync(string room, string message, CancellationToken cancellationToken = default);
}

public interface IMessageSubscriber
{
    Task<IAsyncDisposable> SubscribeAsync(string room, Func<string, Task> handleMessageAsync, CancellationToken cancellationToken);
}
```

## Usage

This package contains only abstractions. Install an implementation package:

- `CustomPubSub.RabbitMq` — RabbitMQ implementation
- `CustomPubSub.WebSocket` — ASP.NET Core WebSocket middleware

## Links

- [GitHub Repository](https://github.com/vefacaglar/websocket-pubsub-rabbitmq)
- [Samples](https://github.com/vefacaglar/websocket-pubsub-rabbitmq/tree/main/samples)
