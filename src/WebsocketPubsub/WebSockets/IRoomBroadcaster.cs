namespace WebsocketPubsub.WebSockets;

public interface IRoomBroadcaster
{
    Task SubscribeAsync(string room, CancellationToken cancellationToken);
    Task UnsubscribeAsync(string room);
}
