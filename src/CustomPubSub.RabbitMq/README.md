# CustomPubSub.RabbitMq

RabbitMQ implementation for the CustomPubSub pub/sub messaging library.

## Installation

```bash
dotnet add package CustomPubSub.RabbitMq
```

## Quick Start

```csharp
using CustomPubSub.RabbitMq;

builder.Services.AddRabbitMqPubSub(builder.Configuration);
```

`appsettings.json`:

```json
{
  "RabbitMq": {
    "HostName": "localhost",
    "Port": 5672,
    "UserName": "guest",
    "Password": "guest",
    "ExchangeName": "rooms"
  }
}
```

Or use the delegate overload:

```csharp
builder.Services.AddRabbitMqPubSub(options =>
{
    options.HostName = "localhost";
    options.ExchangeName = "rooms";
});
```

## Options

| Property | Default | Description |
|----------|---------|-------------|
| `HostName` | `localhost` | RabbitMQ host |
| `Port` | `5672` | AMQP port |
| `UserName` | `guest` | Username |
| `Password` | `guest` | Password |
| `ExchangeName` | `rooms` | Direct exchange name |

## How It Works

- Uses a **direct exchange** where each room maps to a routing key.
- Subscribers get exclusive auto-delete queues bound to the exchange.
- Messages are published with the room name as the routing key.

## Links

- [GitHub Repository](https://github.com/vefacaglar/websocket-pubsub-rabbitmq)
- [CustomPubSub](https://www.nuget.org/packages/CustomPubSub) (core abstractions)
- [CustomPubSub.WebSocket](https://www.nuget.org/packages/CustomPubSub.WebSocket) (WebSocket middleware)
