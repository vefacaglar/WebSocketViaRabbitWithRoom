# WebsocketPubsub

ASP.NET Core WebSocket + RabbitMQ pub/sub POC.

This project makes it easy to test room-based WebSocket broadcasting through RabbitMQ, including a local multi-instance setup that simulates a load-balanced environment.

## Requirements

- .NET SDK 10
- Docker
- Make

## RabbitMQ

Start RabbitMQ:

```bash
docker compose up -d
```

RabbitMQ Management UI:

```text
http://localhost:15672
```

Login:

```text
guest / guest
```

Stop RabbitMQ:

```bash
docker compose down
```

Stop RabbitMQ and remove its volume:

```bash
docker compose down -v
```

## Run One Instance

From the repository root:

```bash
dotnet run --project src/WebsocketPubsub/WebsocketPubsub.csproj --launch-profile http
```

Application URL:

```text
http://localhost:5232
```

On the page:

1. Choose a room.
2. Click `Connect WebSocket`.
3. Enter a message.
4. Click `Publish` to publish the message to the same room through RabbitMQ.

## VS Code Debug

Open the Run and Debug panel and select:

```text
Debug WebsocketPubsub
```

This configuration:

1. Starts RabbitMQ with `docker compose up -d`.
2. Restores and builds the project.
3. Starts the application in debug mode on `http://localhost:5232`.

## Local Multi-Instance Test

To simulate a load-balanced environment locally, start three application instances with one command:

```bash
make local-lb
```

This command:

- Starts RabbitMQ.
- Restores and builds the project.
- Starts the application on three different ports:

```text
http://localhost:5232
http://localhost:5233
http://localhost:5234
```

To test the flow:

1. Open the three URLs in separate browser tabs.
2. Use the same room in each tab, for example `vefa`.
3. Click `Connect WebSocket` in each tab.
4. Click `Publish` from any tab.
5. The message should appear in the other tabs as well.

Logs are written to:

```text
.local/logs/
```

Stop all local instances:

```bash
make local-lb-stop
```

## API

Publish a message:

```http
POST /api/message
Content-Type: application/json
```

Body:

```json
{
  "room": "vefa",
  "message": "hello local websocket"
}
```

Curl example:

```bash
curl -X POST http://localhost:5232/api/message \
  -H "Content-Type: application/json" \
  -d '{"room":"vefa","message":"hello local websocket"}'
```

## WebSocket Endpoint

Room-based WebSocket endpoint format:

```text
ws://localhost:5232/ws/{room}
```

Example:

```text
ws://localhost:5232/ws/vefa
```

## Architecture Notes

- Each application instance keeps track of only its own connected WebSocket clients in memory.
- RabbitMQ fanout exchanges are created by room name.
- When a client connects to a room, that application instance starts a RabbitMQ consumer for that room.
- When a message is published through the API, RabbitMQ distributes it to all application instances that consume the same room.
- Each application instance broadcasts the message only to the WebSocket clients connected to that instance.

This allows local testing of pub/sub behavior across multiple application instances.
