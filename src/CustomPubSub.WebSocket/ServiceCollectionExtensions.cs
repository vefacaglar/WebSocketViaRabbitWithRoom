using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.WebSockets;
using Microsoft.Extensions.DependencyInjection;

namespace CustomPubSub.WebSocket;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCustomPubSubWebSocket(this IServiceCollection services, TimeSpan? keepAliveInterval = null)
    {
        services.AddWebSockets(options =>
        {
            options.KeepAliveInterval = keepAliveInterval ?? TimeSpan.FromSeconds(120);
        });

        services.AddSingleton<WebSocketConnectionManager>();
        services.AddSingleton<WebSocketSessionHandler>();
        services.AddSingleton<IRoomBroadcaster, RoomBroadcaster>();

        return services;
    }

    public static IApplicationBuilder UseCustomPubSubWebSocket(this IApplicationBuilder app)
    {
        app.UseWebSockets();
        app.UseMiddleware<WebSocketMiddleware>();
        return app;
    }
}
