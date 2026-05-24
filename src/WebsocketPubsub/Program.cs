using Microsoft.AspNetCore.WebSockets;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddWebSockets(options =>
{
    options.KeepAliveInterval = TimeSpan.FromSeconds(120);
});
builder.Services.AddSingleton<WebSocketConnectionManager>();
builder.Services.AddSingleton<RabbitMqService>();
builder.Services.AddSingleton<IWebSocketBackgroundService, WebSocketBackgroundService>();

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
app.UseMiddleware<WebSocketMiddleware>();

app.Run();
