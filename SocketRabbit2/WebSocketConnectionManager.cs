using System.Net.WebSockets;
using System.Collections.Concurrent;
using System.Text;

public class WebSocketConnectionManager
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, WebSocket>> _rooms = new ConcurrentDictionary<string, ConcurrentDictionary<string, WebSocket>>();

    public void AddSocket(string room, string id, WebSocket socket)
    {
        var roomSockets = _rooms.GetOrAdd(room, _ => new ConcurrentDictionary<string, WebSocket>());
        roomSockets.TryAdd(id, socket);
    }

    public async Task RemoveSocket(string room, string id)
    {
        if (_rooms.TryGetValue(room, out var roomSockets) && roomSockets.TryRemove(id, out var socket))
        {
            if (socket != null && socket.State == WebSocketState.Open)
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by the WebSocketManager", CancellationToken.None);
            }

            if (roomSockets.IsEmpty)
            {
                _rooms.TryRemove(room, out _); 
            }
        }
    }

    public async Task BroadcastMessage(string room, string message)
    {
        if (_rooms.TryGetValue(room, out var roomSockets))
        {
            var buffer = Encoding.UTF8.GetBytes(message);
            var tasks = roomSockets.Values.Where(x => x.State == WebSocketState.Open).Select(socket =>
                {
                    return socket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
                }
            );
            await Task.WhenAll(tasks);
        }
    }
}
