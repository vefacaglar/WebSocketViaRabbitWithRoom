namespace WebsocketPubsub.Messaging;

public interface IMessageSubscriber
{
    IDisposable Subscribe(string room, Action<string> handleMessage, CancellationToken cancellationToken);
}
