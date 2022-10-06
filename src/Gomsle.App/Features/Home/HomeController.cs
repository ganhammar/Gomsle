using Microsoft.AspNetCore.Mvc;

namespace Gomsle.App.Features.Home;

public class HomeController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }
}