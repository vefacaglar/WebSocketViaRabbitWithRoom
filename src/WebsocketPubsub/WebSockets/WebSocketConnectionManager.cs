using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

namespace WebsocketPubsub.WebSockets;

public sealed class WebSocketConnectionManager
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, WebSocket>> _rooms = new();

    public void AddSocket(string room, string id, WebSocket socket)
    {
        var roomSockets = _rooms.GetOrAdd(room, _ => new ConcurrentDictionary<string, WebSocket>());
        roomSockets.TryAdd(id, socket);
    }

    public async Task RemoveSocket(string room, string id)
    {
        if (!_rooms.TryGetValue(room, out var roomSockets) || !roomSockets.TryRemove(id, out var socket))
        {
            return;
        }

        if (socket.State == WebSocketState.Open)
        {
            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by the WebSocketManager", CancellationToken.None);
        }

        if (roomSockets.IsEmpty)
        {
            _rooms.TryRemove(room, out _);
        }
    }

    public async Task BroadcastMessage(string room, string message)
    {
        if (!_rooms.TryGetValue(room, out var roomSockets))
        {
            return;
        }

        var buffer = Encoding.UTF8.GetBytes(message);
        var tasks = roomSockets.Select(async pair =>
        {
            var socket = pair.Value;

            if (socket.State != WebSocketState.Open)
            {
                await RemoveSocket(room, pair.Key);
                return;
            }

            try
            {
                await socket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
            }
            catch (WebSocketException)
            {
                await RemoveSocket(room, pair.Key);
            }
        });

        await Task.WhenAll(tasks);
    }
}
