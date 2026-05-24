namespace WebsocketPubsub.Messaging;

public interface IMessagePublisher
{
    void Publish(string room, string message);
}
