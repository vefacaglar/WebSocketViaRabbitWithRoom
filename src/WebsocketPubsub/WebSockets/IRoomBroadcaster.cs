namespace WebsocketPubsub.WebSockets;

public interface IRoomBroadcaster
{
    void Subscribe(string room);
    void Unsubscribe(string room);
}
