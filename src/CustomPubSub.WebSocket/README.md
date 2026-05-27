# CustomPubSub.WebSocket

ASP.NET Core WebSocket middleware for room-based broadcasting, built on top of CustomPubSub.

## Installation

```bash
dotnet add package CustomPubSub.WebSocket
```

## Quick Start

```csharp
using CustomPubSub.WebSocket;

builder.Services.AddCustomPubSubWebSocket();

var app = builder.Build();

app.UseCustomPubSubWebSocket();
```

## WebSocket Endpoint

```
ws://localhost:<port>/ws/{room}
```

```javascript
const socket = new WebSocket('ws://localhost:5000/ws/myroom');
socket.onmessage = (event) => console.log(event.data);
```

## Options

```csharp
services.AddCustomPubSubWebSocket(keepAliveInterval: TimeSpan.FromSeconds(60));
```

## How It Works

- Accepts WebSocket connections at `/ws/{room}`.
- Tracks connected sockets per room in memory.
- Subscribes to RabbitMQ for each active room via `IMessageSubscriber`.
- Broadcasts incoming messages to all WebSocket clients in that room.
- Reference-counts room subscriptions — shared across clients, cleaned up when the last client disconnects.

## Requirements

Requires a `CustomPubSub` implementation (e.g. `CustomPubSub.RabbitMq`) to be registered.

## Links

- [GitHub Repository](https://github.com/vefacaglar/websocket-pubsub-rabbitmq)
- [CustomPubSub](https://www.nuget.org/packages/CustomPubSub) (core abstractions)
- [CustomPubSub.RabbitMq](https://www.nuget.org/packages/CustomPubSub.RabbitMq) (RabbitMQ implementation)
