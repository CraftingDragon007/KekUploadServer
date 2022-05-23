using Microsoft.AspNetCore.Mvc;

namespace KekUploadServer.Controllers;

public class FrontendController : Controller
{
    [Route("/")]
    [HttpGet]
    public IActionResult Index()
    {
        return File(new FileStream(Data.EnsureNotNullConfig().WebRoot + "index.html", FileMode.Open), "text/html");
    }
    
    [Route("theme.js")]
    [HttpGet]
    public IActionResult Theme()
    {
        return File(new FileStream(Data.EnsureNotNullConfig().WebRoot + "theme.js", FileMode.Open), "text/javascript");
    }

    [Route("themes/{theme}")]
    [HttpGet]
    public IActionResult Themes(string theme)
    {
        return File(new FileStream(Data.EnsureNotNullConfig().WebRoot + "themes/" + theme, FileMode.Open), "text/css");
    }
    
    /*[Route("{*path}")]
    [HttpGet]
    public IActionResult Static(string path)
    {
        var file = Data.EnsureNotNullConfig().WebRoot + path;
        var fileInfo = new FileInfo(file);
        return File(new FileStream(file, FileMode.Open), "text/" + fileInfo.Extension.Replace(".", "").Replace("js", "javascript"));
    }*/
    
    [Route("assets/{asset}")]
    [HttpGet]
    public IActionResult Assets(string asset)
    {
        var file = Data.EnsureNotNullConfig().WebRoot + "assets/" + asset;
        var fileInfo = new FileInfo(file);
        return File(new FileStream(file, FileMode.Open), "text/" + fileInfo.Extension.Replace(".", "").Replace("js", "javascript"));
    }
}