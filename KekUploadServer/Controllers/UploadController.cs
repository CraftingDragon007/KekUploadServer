using Microsoft.AspNetCore.Mvc;

namespace KekUploadServer.Controllers;

public class Upload : Controller
{
    // GET
    public IActionResult Index()
    {
        return View();
    }
}