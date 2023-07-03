using Microsoft.AspNetCore.Mvc;

namespace KekUploadServer.Controllers
{
    public class FrontendController : Controller
    {
        [HttpGet("/")]
        public IActionResult Index()
        {
            var filePath = Path.GetFullPath(Path.Combine(Data.EnsureNotNullConfig().WebRoot, "index.html"));
            return System.IO.File.Exists(filePath) ? PhysicalFile(filePath, "text/html") : NotFound();
        }

        [HttpGet("theme.js")]
        public IActionResult Theme()
        {
            var filePath = Path.GetFullPath(Path.Combine(Data.EnsureNotNullConfig().WebRoot, "theme.js"));
            return System.IO.File.Exists(filePath) ? PhysicalFile(filePath, "text/javascript") : NotFound();
        }

        [HttpGet("themes/{theme}")]
        public IActionResult Themes(string theme)
        {
            var filePath = Path.GetFullPath(Path.Combine(Data.EnsureNotNullConfig().WebRoot, "themes", theme));
            return System.IO.File.Exists(filePath) ? PhysicalFile(filePath, "text/css") : NotFound();
        }

        [HttpGet("assets/{asset}")]
        public IActionResult Assets(string asset)
        {
            var filePath = Path.GetFullPath(Path.Combine(Data.EnsureNotNullConfig().WebRoot, "assets", asset));
            var contentType = GetContentType(filePath);
            return System.IO.File.Exists(filePath) ? PhysicalFile(filePath, contentType) : NotFound();
        }

        [HttpGet("favicon.{ext}")]
        public IActionResult Favicon(string ext)
        {
            var filePath = Path.GetFullPath(Path.Combine(Data.EnsureNotNullConfig().WebRoot, $"favicon.{ext}"));
            var contentType = GetContentType(filePath);
            return System.IO.File.Exists(filePath) ? PhysicalFile(filePath, contentType) : NotFound();
        }

        private static string GetContentType(string filePath)
        {
            if(!System.IO.File.Exists(filePath))
                return "application/octet-stream";
            var mimeTypeEnumerable = MimeTypeMap.List.MimeTypeMap.GetMimeType(Path.GetExtension(filePath));
            if (mimeTypeEnumerable != null)
                return mimeTypeEnumerable.First();
            
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