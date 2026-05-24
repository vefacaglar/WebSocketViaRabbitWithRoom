using Microsoft.AspNetCore.Mvc;
using WebsocketPubsub.Messaging;
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
    public IActionResult Send([FromBody] SendMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Room) || string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest("Room and message are required.");
        }

        _publisher.Publish(request.Room.Trim(), request.Message);
        return Ok();
    }
}
