using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Html;
using SharpHash.Base;
using Microsoft.AspNetCore.Mvc;

namespace KekUploadServer.Controllers;

public class UploadController : Controller
{

    [HttpPost]
    [Route("u/{stream}/{hash}")]
    public IActionResult Upload(string stream, string hash)
    {
        var item = Data.UploadStreams.GetByStreamId(stream);
        if (item == null)
        {
            return new JsonResult(new
            {
                generic = "NOT_FOUND",
                field = "STREAM",
                error = "Stream not found"
            }) {ContentType = "application/json", SerializerSettings = null, StatusCode = 404};
        }

        var uploadStream = new MemoryStream();
        HttpContext.Request.Body.CopyToAsync(uploadStream).Wait();
        var data = uploadStream.ToArray();
        if (data.Length > Data.MaxChunkSize)
        {
            return new JsonResult(new {generic = "OVERFLOW", field = "CHUNK", error = "Chunk size exceeded"})
                {ContentType = "application/json", SerializerSettings = null, StatusCode = 400};
        }
        var chunkHash = HashFactory.Crypto.CreateSHA1().ComputeBytes(data).ToString().ToLower().Replace("-", "");
        if (chunkHash != hash)
        {
            return new JsonResult(new {generic = "HASH_MISMATCH", field = "HASH", error = "Hash doesn't match"})
                {ContentType = "application/json", SerializerSettings = null, StatusCode = 400};
        }
        //uploadStream.CopyTo(item.FileStream);
        item.FileStream.Write(data, 0, data.Length);
        item.Hash.TransformBytes(data);
        return new JsonResult(new { success = true }) {ContentType = "application/json", SerializerSettings = null, StatusCode = 200};
    }

    [HttpPost]
    [Route("c/{ext}")]
    public IActionResult Create(string ext)
    {
        var streamId = Data.RandomString(64);
        FileStream fileStream;
        try
        {
            fileStream = System.IO.File.Create("uploads/" + streamId + ".upload");
        }catch(Exception e)
        {
            return new JsonResult(new { generic = "FS_CREATE", field = "FILE", error = "Error while creating file: " + e.Message })
            {
                ContentType = "application/json",
                SerializerSettings = null,
                StatusCode = 500
            };
        }
        Data.UploadStreams.Add(new UploadItem(streamId, ext, fileStream));
        Console.WriteLine("Created upload stream with Id: " + streamId);
        return new JsonResult(new { stream = streamId });
    }
    
    [HttpPost]
    [Route("c/{ext}/{name}")]
    public IActionResult Create(string ext, string name)
    {
        var streamId = Data.RandomString(64);
        FileStream fileStream;
        try
        {
            fileStream = System.IO.File.Create("uploads/" + streamId + ".upload");
        }catch(Exception e)
        {
            return new JsonResult(new { generic = "FS_CREATE", field = "FILE", error = "Error while creating file: " + e.Message })
            {
                ContentType = "application/json",
                SerializerSettings = null,
                StatusCode = 500
            };
        }
        Data.UploadStreams.Add(new UploadItem(streamId, ext, name, fileStream));
        return new JsonResult(new { stream = streamId });
    }

    [HttpPost]
    [Route("r/{stream}")]
    public IActionResult Remove(string stream)
    {
        try
        {
            System.IO.File.Delete("uploads/" + stream + ".upload");
        }
        catch (IOException e)
        {
            return new JsonResult(new { generic = "FS_REMOVE", field = "FILE", error = "Error while deleting file: " + e.Message })
            {
                ContentType = "application/json",
                SerializerSettings = null,
                StatusCode = 500
            };
        }
        return new JsonResult(new { success = true });
    }
    
    [HttpPost]
    [Route("f/{stream}/{hash}")]
    public IActionResult Finish(string stream, string hash)
    {
        var item = Data.UploadStreams.GetByStreamId(stream);
        if (item == null)
        {
            return new JsonResult(new { generic = "NOT_FOUND", field = "STREAM", error = "Stream not found" })
            {
                ContentType = "application/json",
                SerializerSettings = null,
                StatusCode = 404
            };
        }

        var fileHash = item.Hash.TransformFinal().ToString().ToLower();
        if (fileHash != hash)
        {
            return new JsonResult(new { generic = "HASH_MISMATCH", field = "HASH", error = "Hash doesn't match" })
            {
                ContentType = "application/json",
                SerializerSettings = null,
                StatusCode = 400
            };
        }
        item.FileStream.Close();
        Data.UploadStreams.Remove(item);
        var id = Data.RandomString(7);
        Data.InsertUploadedItemIntoDatabase(item, id);
        return new JsonResult(new { id = id }) {ContentType = "application/json", SerializerSettings = null, StatusCode = 200};
    }

    [HttpGet]
    [Route("d/{id}")]
    public IActionResult Download(string id)
    {
        var notFound = new JsonResult(new { generic = "NOT_FOUND", field = "ID", error = "File with id not found" })
        {
            ContentType = "application/json",
            SerializerSettings = null,
            StatusCode = 404
        };
        var streamId = Data.GetStreamIdByUploadId(id);
        if (streamId == null)
        {
            return notFound;
        }
        var extension = Data.GetExtensionFromId(id);
        var name = Data.GetNameFromId(id);
        extension ??= "";
        if (name == null)
        {
            var hash = Data.GetHashFromId(id);
            name = hash ?? "HASH_NOT_FOUND";
        }

        if (System.IO.File.Exists("uploads/" + streamId + ".upload"))
        {
            return File(new FileStream("uploads/" + streamId + ".upload", FileMode.Open), "application/octet-stream", name + (extension.Equals("none") ? "" : "." + extension));
        }
        return notFound;
    }

    [HttpGet]
    [Route("vs/{id}")]
    public Task StreamVideo(string id)
    {
        var notFound = new JsonResult(new { generic = "NOT_FOUND", field = "ID", error = "File with id not found" })
        {
            ContentType = "application/json",
            SerializerSettings = null,
            StatusCode = 404
        };
        var streamId = Data.GetStreamIdByUploadId(id);
        if (streamId == null)
        {
            return notFound.ExecuteResultAsync(ControllerContext);
        }
        var extension = Data.GetExtensionFromId(id);
        var name = Data.GetNameFromId(id);
        extension ??= "";
        if (name == null)
        {
            var hash = Data.GetHashFromId(id);
            name = hash ?? "HASH_NOT_FOUND";
        }

        if (!System.IO.File.Exists("uploads/" + streamId + ".upload"))
            return notFound.ExecuteResultAsync(ControllerContext);
        
        HttpContext.Response.ContentType = "video/" + extension;
        var buffer = new byte[4096];
        var stream = new FileStream("uploads/" + streamId + ".upload", FileMode.Open);
        /*return new Task(() =>
        {
            while (true)
            {
                var read = stream.Read(buffer, 0, buffer.Length);
                if (read == 0)
                    break;
                HttpContext.Response.Body.Write(buffer, 0, read);
            }
        });*/
        //return new FileStreamResult(new FileStream("uploads/" + streamId + ".upload", FileMode.Open), "video/" + extension).ExecuteResultAsync(ControllerContext);
        //var video = new VideoStream(name);
        
        HttpContext.Response.Headers.Add("Content-Disposition", "attachment; filename=" + name + (extension.Equals("none") ? "" : "." + extension));
        return new Task(() =>
        {
            while (true)
            {
                var read = stream.Read(buffer, 0, buffer.Length);
                if (read == 0)
                    break;
                HttpContext.Response.Body.Write(buffer, 0, read);
            }
        });
        //return new PushStreamContent(video.WriteToStream, new MediaTypeHeaderValue("video/" + extension)).CopyToAsync(HttpContext.Response.Body);
    }

    [HttpGet]
    [Route("l/{id}")]
    public IActionResult Lenght(string id)
    {
        var notFound = new JsonResult(new { generic = "NOT_FOUND", field = "ID", error = "File with id not found" })
        {
            ContentType = "application/json",
            SerializerSettings = null,
            StatusCode = 404
        };
        var streamId = Data.GetStreamIdByUploadId(id);
        if (streamId == null)
        {
            return notFound;
        }
        var extension = Data.GetExtensionFromId(id);
        var name = Data.GetNameFromId(id);
        extension ??= "";
        if (name == null)
        {
            var hash = Data.GetHashFromId(id);
            name = hash ?? "HASH_NOT_FOUND";
        }

        if (!System.IO.File.Exists("uploads/" + streamId + ".upload")) return notFound;
        
        return base.Content(new FileInfo("uploads/" + streamId + ".upload").Length.ToString());
    }

    [HttpGet]
    [Route("d/{id}/{startByte}/{endByte}")]
    public IActionResult DownloadRange(string id, long startByte, long endByte)
    {
        var notFound = new JsonResult(new { generic = "NOT_FOUND", field = "ID", error = "File with id not found" })
        {
            ContentType = "application/json",
            SerializerSettings = null,
            StatusCode = 404
        };
        var streamId = Data.GetStreamIdByUploadId(id);
        if (streamId == null)
        {
            return notFound;
        }
        var extension = Data.GetExtensionFromId(id);
        var name = Data.GetNameFromId(id);
        extension ??= "";
        if (name == null)
        {
            var hash = Data.GetHashFromId(id);
            name = hash ?? "HASH_NOT_FOUND";
        }

        if (!System.IO.File.Exists("uploads/" + streamId + ".upload")) return notFound;
        
        var stream = new FileStream("uploads/" + streamId + ".upload", FileMode.Open);
        var range = new byte[endByte - startByte];
        try
        {
            stream.Seek(startByte, SeekOrigin.Begin);
            var read = stream.Read(range, 0, (int) (endByte - startByte));
            stream.Close();
        }catch(Exception e)
        {
            return notFound;
        }
        return File(range, "application/octet-stream", name + (extension.Equals("none") ? "" : "." + extension));
    }
    

    [HttpGet]
    [Route("v/{id}")]
    public IActionResult Video(string id)
    {
        var notFound = new JsonResult(new { generic = "NOT_FOUND", field = "ID", error = "File with id not found" })
        {
            ContentType = "application/json",
            SerializerSettings = null,
            StatusCode = 404
        };
        var streamId = Data.GetStreamIdByUploadId(id);
        if (streamId == null)
        {
            return notFound;
        }
        var extension = Data.GetExtensionFromId(id);
        var name = Data.GetNameFromId(id);
        extension ??= "";
        if (name == null)
        {
            var hash = Data.GetHashFromId(id);
            name = hash ?? "HASH_NOT_FOUND";
        }

        if (!System.IO.File.Exists("uploads/" + streamId + ".upload")) return notFound;
        
        return base.Content($"<!DOCTYPE html><html><head><title>{name}</title><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1, shrink-to-fit=no\"><link rel=\"stylesheet\" href=\"https://stackpath.bootstrapcdn.com/bootstrap/4.3.1/css/bootstrap.min.css\" integrity=\"sha384-ggOyR0iXCbMQv3Xipma34MD+dH/1fQ784/j6cY/iJTQUOhcWr7x9JvoRxT2MZw1T\" crossorigin=\"anonymous\"><script src=\"https://code.jquery.com/jquery-3.3.1.slim.min.js\" integrity=\"sha384-q8i/X+965DzO0rT7abK41JStQIAqVgRVzpbzo5smXKp4YfRvH+8abtTE1Pi6jizo\" crossorigin=\"anonymous\"></script><script src=\"https://cdnjs.cloudflare.com/ajax/libs/popper.js/1.14.7/umd/popper.min.js\" integrity=\"sha384-UO2eT0CpHqdSJQ6hJty5KVphtPhzWj9WO1clHTMGa3JDZwrnQq4sF86dIHNDz0W1\" crossorigin=\"anonymous\"></script><script src=\"https://stackpath.bootstrapcdn.com/bootstrap/4.3.1/js/bootstrap.min.js\" integrity=\"sha384-JjSmVgyd0p3pXB1rRibZUAYoIIy6OrQ6VrjIEaFf/nJGzIxFDsf4x0xIM+B07jRM\" crossorigin=\"anonymous\"></script><style>body{{background-color:black;color:white;}}</style></head><body><video controls autoplay><source src=\"/vs/{id}\" type=\"video/{extension}\"></video></body></html>", "text/html");
        
        /* return base.Content($"<!DOCTYPE html><html><head>\n <title>{name}</title> <link href=\"https://vjs.zencdn.net/7.19.2/video-js.css\" rel=\"stylesheet\" />\n" +
                            $"</head>\n\n<body>\n" +
                            $"  <video\n" +
                            $"    id=\"my-video\"\n" +
                            $"    class=\"video-js\"\n" +
                            $"    controls\n" +
                            $"    preload=\"auto\"\n" +
                            //$"    poster=\"MY_VIDEO_POSTER.jpg\"\n" +
                            $"    data-setup=\"{{}}\"\n" +
                            $"  >\n" +
                            $"    <source src=\"{"/vs/" + id}\" type=\"video/{extension}\" />\n" +
                            $"    <p class=\"vjs-no-js\">\n" +
                            $"      To view this video please enable JavaScript, and consider upgrading to a\n" +
                            $"      web browser that\n" +
                            $"      <a href=\"https://videojs.com/html5-video-support/\" target=\"_blank\"\n" +
                            $"        >supports HTML5 video</a\n" +
                            $"      >\n" +
                            $"    </p>\n" +
                            $"  </video>\n\n" +
                            $"  <script src=\"https://vjs.zencdn.net/7.19.2/video.min.js\"></script>\n" +
                            $"<br>" +
                            $"<a href=\"{Data.EnsureNotNullConfig().DownloadUrl + id}\">Download this video!</a>" +
                            $"</body></html>", "text/html");*/
    }
    
    
    [HttpGet]
    [Route("{id}")]
    public IActionResult Get(string id)
    {
        var notFound = new JsonResult(new { generic = "NOT_FOUND", field = "ID", error = "File with id not found" })
        {
            ContentType = "application/json",
            SerializerSettings = null,
            StatusCode = 404
        };
        var streamId = Data.GetStreamIdByUploadId(id);
        if (streamId == null)
        {
            return notFound;
        }
        var extension = Data.GetExtensionFromId(id);
        var name = Data.GetNameFromId(id);
        extension ??= "";
        if (name == null)
        {
            var hash = Data.GetHashFromId(id);
            name = hash ?? "HASH_NOT_FOUND";
        }

        if (!System.IO.File.Exists("uploads/" + streamId + ".upload")) return notFound;
        var contentBuilder = new StringBuilder();
        var description = Data.EnsureNotNullConfig().EmbedDescription;
        contentBuilder.Append("<!DOCTYPE html>" +
                              $"<meta http-equiv=\"Refresh\" content=\"0; url='{Data.EnsureNotNullConfig().DownloadUrl + id}'\" />" +
                              "<meta charset='UTF-8'>" +
                              "<meta property='og:type' content='website'>" +
                              "<meta property='twitter:card' content='" + ( "summary_large_image") + "'>" +
                              $"<meta name='title' content='{name + "." + extension}'>" +
                              $"<meta property='og:title' content='{name + "." + extension}'>" +
                              $"<meta name='theme-color' content='{Data.EnsureNotNullConfig().EmbedColor}'>" +
                              $"<meta name='description' content='{description}'>");
        var imageExtensions = new List<string> { ".JPG", ".JPEG", ".JPE", ".BMP", ".GIF", ".PNG" };
        var videoExtensions = new List<string> { ".MP4", ".MOV", ".M4V", ".AVI", ".WMV", ".MPG", ".MPEG", ".OGG", ".WEBM" };
        if (imageExtensions.Contains("." + extension.ToUpper()))
        {
            contentBuilder.Append($"<meta property='og:image' content='{Data.EnsureNotNullConfig().DownloadUrl + id}'>" + 
                                  $"<meta property='twitter:image' content='{Data.EnsureNotNullConfig().DownloadUrl + id}'>" + 
                                  $"<meta property='og:description' content='{description}'>" +
                                  $"<meta property='twitter:description' content='{description}'>");
        }
        if(videoExtensions.Contains("." + extension.ToUpper()))
        {
            contentBuilder.Append($"<meta property='og:description' content='{description + "\n" + "Watch video at: " + Data.EnsureNotNullConfig().VideoUrl + id}'>" +
                                  $"<meta property='twitter:description' content='{description + "\n" + "Watch video at: " + Data.EnsureNotNullConfig().VideoUrl + id}'>");
        }

        return base.Content(contentBuilder.ToString(), "text/html");
    }

}