﻿using System.Net.WebSockets;
using System.Text.RegularExpressions;

public class WebSocketMiddleware
{
    private readonly RequestDelegate _next;
    private readonly WebSocketConnectionManager _manager;
    private readonly IWebSocketBackgroundService _backgroundService;
    private static readonly Regex PathRegex = new Regex(@"^/ws/(?<room>[\w-]+)$", RegexOptions.Compiled);

    public WebSocketMiddleware(
        RequestDelegate next, 
        WebSocketConnectionManager manager, 
        IWebSocketBackgroundService backgroundService
        )
    {
        _next = next;
        _manager = manager;
        _backgroundService = backgroundService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var match = PathRegex.Match(context.Request.Path);

        if (match.Success)
        {
            var room = match.Groups["room"].Value;

            if (context.WebSockets.IsWebSocketRequest)
            {
                var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                var socketId = Guid.NewGuid().ToString();
                _manager.AddSocket(room, socketId, webSocket);

                _backgroundService.StartConsumingForRoom(room);

                await HandleWebSocketAsync(webSocket, room, socketId);

                _backgroundService.NotifyClientDisconnected(room);
            }
            else
            {
                context.Response.StatusCode = 400;
            }
        }
        else
        {
            await _next(context);
        }
    }

    private async Task HandleWebSocketAsync(WebSocket socket, string room, string socketId)
    {
        var buffer = new byte[1024 * 4];
        WebSocketReceiveResult result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

        while (!result.CloseStatus.HasValue)
        {
            result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        }

        await _manager.RemoveSocket(room, socketId);
        await socket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
    }
}
