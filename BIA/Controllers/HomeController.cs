using Microsoft.AspNetCore.Mvc;

namespace BIA.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
