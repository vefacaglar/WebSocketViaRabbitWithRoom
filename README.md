# CustomPubSub

Room-based pub/sub messaging library for .NET with RabbitMQ and WebSocket support.

## Packages

| Package | Description |
|---------|-------------|
| `CustomPubSub` | Core abstractions (`IMessagePublisher`, `IMessageSubscriber`) |
| `CustomPubSub.RabbitMq` | RabbitMQ implementation using direct exchanges |
| `CustomPubSub.WebSocket` | ASP.NET Core WebSocket middleware for room-based broadcasting |

## Installation

```bash
dotnet add package CustomPubSub --version 1.0.0
dotnet add package CustomPubSub.RabbitMq --version 1.0.0
dotnet add package CustomPubSub.WebSocket --version 1.0.0
```

## Quick Start

### 1. Register services

```csharp
using CustomPubSub.RabbitMq;
using CustomPubSub.WebSocket;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRabbitMqPubSub(builder.Configuration);
builder.Services.AddCustomPubSubWebSocket();

var app = builder.Build();

app.UseCustomPubSubWebSocket();

app.Run();
```

### 2. Configure RabbitMQ

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
    options.Port = 5672;
    options.UserName = "guest";
    options.Password = "guest";
    options.ExchangeName = "rooms";
});
```

### 3. Publish messages

```csharp
public class MyController : ControllerBase
{
    private readonly IMessagePublisher _publisher;

    public MyController(IMessagePublisher publisher)
    {
        _publisher = publisher;
    }

    [HttpPost("publish")]
    public async Task Publish(string room, string message)
    {
        await _publisher.PublishAsync(room, message);
    }
}
```

### 4. Connect via WebSocket

```javascript
const socket = new WebSocket('ws://localhost:5000/ws/myroom');
socket.onmessage = (event) => console.log(event.data);
```

## API Reference

### CustomPubSub

Core abstractions that define the pub/sub contract.

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

### CustomPubSub.RabbitMq

RabbitMQ implementation. Registers `IMessagePublisher` and `IMessageSubscriber` as singletons.

**Registration:**

```csharp
services.AddRabbitMqPubSub(configuration);
// or
services.AddRabbitMqPubSub(options => { ... });
```

**Options (`RabbitMqOptions`):**

| Property | Default | Description |
|----------|---------|-------------|
| `HostName` | `localhost` | RabbitMQ host |
| `Port` | `5672` | AMQP port |
| `UserName` | `guest` | Username |
| `Password` | `guest` | Password |
| `ExchangeName` | `rooms` | Direct exchange name |

### CustomPubSub.WebSocket

ASP.NET Core WebSocket middleware. Manages room-based WebSocket connections and bridges them to RabbitMQ subscriptions.

**Registration:**

```csharp
services.AddCustomPubSubWebSocket();
// optional keep-alive interval
services.AddCustomPubSubWebSocket(keepAliveInterval: TimeSpan.FromSeconds(60));
```

**Middleware:**

```csharp
app.UseCustomPubSubWebSocket();
```

**WebSocket endpoint:**

```
ws://localhost:<port>/ws/{room}
```

## Architecture

```
Client A ──ws──┐                    ┌──ws── Client C
               │                    │
          Instance 1 ──┐      ┌── Instance 2
               │       │      │       │
               │       ▼      ▼       │
               │     ┌────────────┐   │
               │     │  RabbitMQ  │   │
               │     │  (direct   │   │
               │     │  exchange) │   │
               │     └────────────┘   │
               │                      │
Client B ──ws──┘                      └──ws── Client D
```

- Each application instance tracks only its own WebSocket clients in memory.
- RabbitMQ direct exchanges route messages by room name (routing key).
- When a client connects to a room, the instance starts a RabbitMQ consumer for that room.
- Published messages are distributed by RabbitMQ to all consuming instances.
- Each instance broadcasts the message only to its own connected WebSocket clients.

This enables horizontal scaling across multiple application instances.

## Sample

See [samples/WebsocketPubsub](samples/WebsocketPubsub/) for a working example.

## Development

### Requirements

- .NET SDK 10
- Docker

### Build

```bash
dotnet build
```

### Pack

```bash
dotnet pack src/CustomPubSub/CustomPubSub.csproj -c Release -o packages
dotnet pack src/CustomPubSub.RabbitMq/CustomPubSub.RabbitMq.csproj -c Release -o packages
dotnet pack src/CustomPubSub.WebSocket/CustomPubSub.WebSocket.csproj -c Release -o packages
```

### Start RabbitMQ

```bash
docker compose up -d
```

Management UI: `http://localhost:15672` (guest / guest)

### Run sample

```bash
dotnet run --project samples/WebsocketPubsub/WebsocketPubsub.csproj --launch-profile http
```

### Multi-instance test

Simulate a load-balanced environment with 3 instances:

```bash
make local-lb
```

Opens ports `5232`, `5233`, `5234`. Open each in a browser, connect to the same room, and publish from any tab.

```bash
make local-lb-stop
```
