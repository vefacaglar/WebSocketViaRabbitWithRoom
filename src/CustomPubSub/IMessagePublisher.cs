namespace CustomPubSub;

public interface IMessagePublisher
{
    ValueTask PublishAsync(string room, string message, CancellationToken cancellationToken = default);
}
