using Microsoft.AspNetCore.Mvc;

namespace KekUploadServer.Controllers
{
    public class FrontendController : Controller
    {
        [HttpGet("/")]
        public IActionResult Index()
        {
            var filePath = Path.Combine(Data.EnsureNotNullConfig().WebRoot, "index.html");
            return PhysicalFile(filePath, "text/html");
        }

        [HttpGet("theme.js")]
        public IActionResult Theme()
        {
            var filePath = Path.Combine(Data.EnsureNotNullConfig().WebRoot, "theme.js");
            return PhysicalFile(filePath, "text/javascript");
        }

        [HttpGet("themes/{theme}")]
        public IActionResult Themes(string theme)
        {
            var filePath = Path.Combine(Data.EnsureNotNullConfig().WebRoot, "themes", theme);
            return PhysicalFile(filePath, "text/css");
        }

        [HttpGet("assets/{asset}")]
        public IActionResult Assets(string asset)
        {
            var filePath = Path.Combine(Data.EnsureNotNullConfig().WebRoot, "assets", asset);
            var contentType = GetContentType(filePath);
            return PhysicalFile(filePath, contentType);
        }

        [HttpGet("favicon.{ext}")]
        public IActionResult Favicon(string ext)
        {
            var filePath = Path.Combine(Data.EnsureNotNullConfig().WebRoot, $"favicon.{ext}");
            var contentType = GetContentType(filePath);
            return PhysicalFile(filePath, contentType);
        }

        private static string GetContentType(string filePath)
        {
            var extension = Path.GetExtension(filePath);
            return extension switch
            {
                ".css" => "text/css",
                ".js" => "text/javascript",
                ".png" => "image/png",
                ".ico" => "image/x-icon",
                _ => "application/octet-stream",
            };
        }
    }
}