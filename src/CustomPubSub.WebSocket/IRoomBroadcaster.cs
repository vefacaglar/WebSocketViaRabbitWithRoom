namespace CustomPubSub.WebSocket;

public interface IRoomBroadcaster
{
    Task SubscribeAsync(string room, CancellationToken cancellationToken);
    Task UnsubscribeAsync(string room);
}
