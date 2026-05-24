using System.Net.WebSockets;

namespace WebsocketPubsub.WebSockets;

public sealed class WebSocketSessionHandler
{
    private const int ReceiveBufferSize = 4 * 1024;

    private readonly WebSocketConnectionManager _connections;
    private readonly ILogger<WebSocketSessionHandler> _logger;

    public WebSocketSessionHandler(WebSocketConnectionManager connections, ILogger<WebSocketSessionHandler> logger)
    {
        _connections = connections;
        _logger = logger;
    }

    public async Task HandleAsync(WebSocket socket, string room, string socketId, CancellationToken cancellationToken)
    {
        var buffer = new byte[ReceiveBufferSize];

        try
        {
            while (socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                if (result.CloseStatus.HasValue)
                {
                    break;
                }
            }
        }
        catch (WebSocketException ex) when (ex.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely || socket.State == WebSocketState.Aborted)
        {
            _logger.LogInformation("WebSocket aborted. Room={Room} SocketId={SocketId}", room, socketId);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            await _connections.RemoveSocket(room, socketId);
        }
    }
}
