using Microsoft.AspNetCore.Mvc;

namespace KekUploadServer.Controllers;

public class LegalController : Controller
{
    [HttpGet]
    [Route("legal")]
    public IActionResult Index()
    {
        var legal = System.IO.File.ReadAllText("Legal.html");
        legal = legal.Replace("%email%", Data.EnsureNotNullConfig().ContactEmail);
        return Content(legal, "text/html");
    }
}