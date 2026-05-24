using Microsoft.AspNetCore.Mvc;

namespace SocketRabbit2.Controllers
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

        [HttpGet]
        public IActionResult Send(string room, string message)
        {
            _queueService.PublishMessage(room, message);
            return Ok();
        }
    }
}
