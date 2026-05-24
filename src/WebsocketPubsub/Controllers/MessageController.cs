using Microsoft.AspNetCore.Mvc;

namespace WebsocketPubsub.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MessageController : ControllerBase
    {
        private readonly RabbitMqService _queueService;

        public MessageController(
            RabbitMqService queueService
            )
        {
            _queueService = queueService;
        }

        [HttpPost]
        public IActionResult Send([FromBody] SendMessageRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Room) || string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest("Room and message are required.");
            }

            _queueService.PublishMessage(request.Room.Trim(), request.Message);
            return Ok();
        }
    }

    public record SendMessageRequest(string Room, string Message);
}
