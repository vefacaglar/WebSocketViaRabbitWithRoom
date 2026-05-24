using Microsoft.AspNetCore.Mvc;

namespace WebsocketPubsub.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => View();
}
