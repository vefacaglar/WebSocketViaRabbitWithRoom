using Microsoft.AspNetCore.WebSockets;
using WebsocketPubsub.Messaging;
using WebsocketPubsub.Middleware;
using WebsocketPubsub.WebSockets;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services
    .AddOptions<RabbitMqOptions>()
    .Bind(builder.Configuration.GetSection(RabbitMqOptions.SectionName));

builder.Services.AddWebSockets(options =>
{
    options.KeepAliveInterval = TimeSpan.FromSeconds(120);
});

builder.Services.AddSingleton<RabbitMqConnection>();
builder.Services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();
builder.Services.AddSingleton<IMessageSubscriber, RabbitMqSubscriber>();
builder.Services.AddSingleton<WebSocketConnectionManager>();
builder.Services.AddSingleton<WebSocketSessionHandler>();
builder.Services.AddSingleton<IRoomBroadcaster, RoomBroadcaster>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsync("An unexpected error occurred.");
        });
    });
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.UseWebSockets();
app.UseMiddleware<WebsocketPubsub.Middleware.WebSocketMiddleware>();

app.Run();
