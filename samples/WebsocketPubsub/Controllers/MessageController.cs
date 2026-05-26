using Microsoft.AspNetCore.Mvc;
using CustomPubSub;
using WebsocketPubsub.Models;

namespace WebsocketPubsub.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MessageController : ControllerBase
{
    private readonly IMessagePublisher _publisher;

    public MessageController(IMessagePublisher publisher)
    {
        _publisher = publisher;
    }

    [HttpPost]
    public async Task<IActionResult> Send([FromBody] SendMessageRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Room) || string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest("Room and message are required.");
        }

        await _publisher.PublishAsync(request.Room.Trim(), request.Message, cancellationToken);
        return Ok();
    }
}
