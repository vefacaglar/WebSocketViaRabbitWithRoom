using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

namespace CustomPubSub.WebSocket;

public sealed class WebSocketMiddleware
{
    private static readonly Regex PathRegex = new(@"^/ws/(?<room>[\w-]+)$", RegexOptions.Compiled);

    private readonly RequestDelegate _next;
    private readonly WebSocketConnectionManager _connections;
    private readonly IRoomBroadcaster _broadcaster;
    private readonly WebSocketSessionHandler _sessionHandler;

    public WebSocketMiddleware(
        RequestDelegate next,
        WebSocketConnectionManager connections,
        IRoomBroadcaster broadcaster,
        WebSocketSessionHandler sessionHandler)
    {
        _next = next;
        _connections = connections;
        _broadcaster = broadcaster;
        _sessionHandler = sessionHandler;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var match = PathRegex.Match(context.Request.Path);
        if (!match.Success)
        {
            await _next(context);
            return;
        }

        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        var room = match.Groups["room"].Value;
        var socket = await context.WebSockets.AcceptWebSocketAsync();
        var socketId = Guid.NewGuid().ToString();

        _connections.AddSocket(room, socketId, socket);
        await _broadcaster.SubscribeAsync(room, context.RequestAborted);

        try
        {
            await _sessionHandler.HandleAsync(socket, room, socketId, context.RequestAborted);
        }
        finally
        {
            await _broadcaster.UnsubscribeAsync(room);
        }
    }
}
