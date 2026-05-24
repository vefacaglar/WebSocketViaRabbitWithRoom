namespace WebsocketPubsub.Messaging;

public interface IMessageSubscriber
{
    Task<IAsyncDisposable> SubscribeAsync(string room, Func<string, Task> handleMessageAsync, CancellationToken cancellationToken);
}
